using System.Collections.Concurrent;
using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Services;

/// <summary>
/// Keeps each leaderboard in memory for <see cref="TimeToLive"/>, so a burst of requests for the
/// same leaderboard runs one database query instead of one per request.
/// <para>
/// Registered as a singleton, so every request thread uses the same instance at the same time:
/// </para>
/// <list type="bullet">
/// <item>The entries live in a <see cref="ConcurrentDictionary{TKey, TValue}"/>. A plain
/// <see cref="Dictionary{TKey, TValue}"/> written by two requests at once can lose entries,
/// throw, or corrupt its internal buckets.</item>
/// <item>Loading a missing entry is guarded by a <see cref="SemaphoreSlim"/>, so when 50 requests
/// miss the same leaderboard at once, one loads it and the other 49 wait and reuse the result.
/// <c>lock</c> cannot be used here because the load is awaited, and <c>await</c> is not allowed
/// inside a <c>lock</c>.</item>
/// <item>Storing a loaded entry is guarded by a <c>lock</c>: the size check, the removal of expired
/// entries and the insert must happen as one step, or two requests for different leaderboards
/// (which use different semaphores) could both see room for one more and go over
/// <see cref="MaxEntries"/>. <c>lock</c> works here because nothing inside it is awaited.</item>
/// </list>
/// The cache never holds a DbContext: the scoped caller passes the load function in.
/// </summary>
public sealed class LeaderboardCache(TimeProvider timeProvider) : ILeaderboardCache
{
    /// <summary>How long a loaded leaderboard is served before it is read from the database again.</summary>
    public static readonly TimeSpan TimeToLive = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Upper bound on cached leaderboards. The topic is free text from the query string, so without a
    /// bound every distinct topic someone sends would stay in memory.
    /// </summary>
    public const int MaxEntries = 500;

    // A fixed set of semaphores shared by hash ("lock striping"): one per leaderboard would have to be
    // created and cleaned up per topic, and one for everything would make unrelated leaderboards wait.
    private const int GateCount = 32;

    private readonly ConcurrentDictionary<LeaderboardQuery, CacheEntry> _entries = new();
    private readonly SemaphoreSlim[] _gates = Enumerable.Range(0, GateCount)
        .Select(_ => new SemaphoreSlim(1, 1))
        .ToArray();
    private readonly object _storeLock = new();
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <summary>Number of leaderboards currently held, fresh or expired.</summary>
    public int Count => _entries.Count;

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetOrLoadAsync(
        LeaderboardQuery query,
        Func<CancellationToken, Task<IReadOnlyList<LeaderboardEntryDto>>> load,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(load);

        if (TryGetFresh(query, out var cached))
        {
            return cached;
        }

        var gate = _gates[(query.GetHashCode() & int.MaxValue) % GateCount];
        await gate.WaitAsync(cancellationToken);
        try
        {
            // Another request may have loaded it while this one was waiting at the gate.
            if (TryGetFresh(query, out cached))
            {
                return cached;
            }

            var loaded = await load(cancellationToken);
            Store(query, loaded);
            return loaded;
        }
        finally
        {
            gate.Release();
        }
    }

    private bool TryGetFresh(LeaderboardQuery query, out IReadOnlyList<LeaderboardEntryDto> entries)
    {
        if (_entries.TryGetValue(query, out var entry) && entry.ExpiresAt > _timeProvider.GetUtcNow())
        {
            entries = entry.Entries;
            return true;
        }

        entries = [];
        return false;
    }

    private void Store(LeaderboardQuery query, IReadOnlyList<LeaderboardEntryDto> entries)
    {
        // Check-then-insert on shared data: without the lock, two threads could both pass the check.
        lock (_storeLock)
        {
            if (_entries.Count >= MaxEntries && !_entries.ContainsKey(query))
            {
                RemoveExpired();
                if (_entries.Count >= MaxEntries)
                {
                    // Full of fresh entries: serve this result without caching it.
                    return;
                }
            }

            _entries[query] = new CacheEntry(entries, _timeProvider.GetUtcNow() + TimeToLive);
        }
    }

    private void RemoveExpired()
    {
        var now = _timeProvider.GetUtcNow();
        foreach (var pair in _entries)
        {
            if (pair.Value.ExpiresAt <= now)
            {
                // Removes the pair only if it was not replaced by a fresh entry in the meantime.
                _entries.TryRemove(pair);
            }
        }
    }

    private sealed record CacheEntry(IReadOnlyList<LeaderboardEntryDto> Entries, DateTimeOffset ExpiresAt);
}

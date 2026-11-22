using CodeRacer.Server.Dtos;
using CodeRacer.Server.Models;
using CodeRacer.Server.Services;

namespace CodeRacer.Server.Tests.Unit;

public class LeaderboardCacheTests
{
    private readonly ManualTimeProvider _clock = new();
    private readonly LeaderboardCache _cache;

    public LeaderboardCacheTests()
    {
        _cache = new LeaderboardCache(_clock);
    }

    [Fact]
    public async Task GetOrLoadAsync_FreshEntry_IsServedWithoutLoadingAgain()
    {
        var loads = 0;
        var query = CreateQuery();

        var first = await _cache.GetOrLoadAsync(query, _ => LoadAsync(ref loads), CancellationToken.None);
        _clock.Advance(LeaderboardCache.TimeToLive - TimeSpan.FromSeconds(1));
        var second = await _cache.GetOrLoadAsync(query, _ => LoadAsync(ref loads), CancellationToken.None);

        Assert.Equal(1, loads);
        Assert.Same(first, second);
    }

    [Fact]
    public async Task GetOrLoadAsync_ExpiredEntry_IsLoadedAgain()
    {
        var loads = 0;
        var query = CreateQuery();

        await _cache.GetOrLoadAsync(query, _ => LoadAsync(ref loads), CancellationToken.None);
        _clock.Advance(LeaderboardCache.TimeToLive);
        await _cache.GetOrLoadAsync(query, _ => LoadAsync(ref loads), CancellationToken.None);

        Assert.Equal(2, loads);
    }

    [Fact]
    public async Task GetOrLoadAsync_DifferentQueries_AreCachedSeparately()
    {
        var loads = 0;

        await _cache.GetOrLoadAsync(CreateQuery(Difficulty.Easy), _ => LoadAsync(ref loads), CancellationToken.None);
        await _cache.GetOrLoadAsync(CreateQuery(Difficulty.Hard), _ => LoadAsync(ref loads), CancellationToken.None);
        await _cache.GetOrLoadAsync(CreateQuery(Difficulty.Easy), _ => LoadAsync(ref loads), CancellationToken.None);

        Assert.Equal(2, loads);
    }

    [Fact]
    public async Task GetOrLoadAsync_ManyConcurrentRequestsForTheSameQuery_LoadOnce()
    {
        var loads = 0;
        var release = new TaskCompletionSource();
        var query = CreateQuery();

        async Task<IReadOnlyList<LeaderboardEntryDto>> SlowLoad(CancellationToken token)
        {
            Interlocked.Increment(ref loads);
            await release.Task; // keep the first load in progress until every request has arrived
            return [CreateEntry()];
        }

        var requests = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() => _cache.GetOrLoadAsync(query, SlowLoad, CancellationToken.None)))
            .ToArray();
        await Task.Delay(100);
        release.SetResult();
        var results = await Task.WhenAll(requests);

        Assert.Equal(1, loads);
        Assert.All(results, result => Assert.Same(results[0], result));
    }

    [Fact]
    public async Task GetOrLoadAsync_FailedLoad_IsNotCached()
    {
        var query = CreateQuery();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _cache.GetOrLoadAsync(
            query,
            _ => throw new InvalidOperationException("database unavailable"),
            CancellationToken.None));

        var loads = 0;
        await _cache.GetOrLoadAsync(query, _ => LoadAsync(ref loads), CancellationToken.None);
        Assert.Equal(1, loads);
    }

    [Fact]
    public async Task GetOrLoadAsync_CancelledWhileWaiting_ThrowsAndDoesNotBlockOthers()
    {
        var release = new TaskCompletionSource();
        var query = CreateQuery();
        var firstRequest = _cache.GetOrLoadAsync(
            query,
            async _ =>
            {
                await release.Task;
                IReadOnlyList<LeaderboardEntryDto> entries = [CreateEntry()];
                return entries;
            },
            CancellationToken.None);

        using var cancellation = new CancellationTokenSource();
        var waitingRequest = _cache.GetOrLoadAsync(query, _ => throw new InvalidOperationException("must not load"), cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitingRequest);
        release.SetResult();
        Assert.Single(await firstRequest);
    }

    [Fact]
    public async Task GetOrLoadAsync_WhenFull_DropsExpiredEntriesAndNeverGrowsPastTheLimit()
    {
        var loads = 0;
        for (var i = 0; i < LeaderboardCache.MaxEntries; i++)
        {
            await _cache.GetOrLoadAsync(CreateQuery(topic: $"topic {i}"), _ => LoadAsync(ref loads), CancellationToken.None);
        }

        // Full of fresh entries: a new leaderboard is still served, just not cached.
        await _cache.GetOrLoadAsync(CreateQuery(topic: "one more"), _ => LoadAsync(ref loads), CancellationToken.None);
        Assert.Equal(LeaderboardCache.MaxEntries, _cache.Count);

        // Once the old entries expire, they are cleared to make room.
        _clock.Advance(LeaderboardCache.TimeToLive);
        await _cache.GetOrLoadAsync(CreateQuery(topic: "after expiry"), _ => LoadAsync(ref loads), CancellationToken.None);
        Assert.Equal(1, _cache.Count);
    }

    [Fact]
    public async Task GetOrLoadAsync_ManyDistinctQueriesConcurrently_NeverExceedsTheLimit()
    {
        // Every load waits for the same signal, so loads that use different semaphores finish
        // together and their inserts collide, the case where an unsynchronized size check overshoots.
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<IReadOnlyList<LeaderboardEntryDto>> LoadWhenReleased(CancellationToken token)
        {
            await release.Task;
            return [CreateEntry()];
        }

        var requests = Enumerable.Range(0, LeaderboardCache.MaxEntries * 2)
            .Select(i => Task.Run(() => _cache.GetOrLoadAsync(
                CreateQuery(topic: $"topic {i}"),
                LoadWhenReleased,
                CancellationToken.None)))
            .ToArray();
        await Task.Delay(100);
        release.SetResult();
        var results = await Task.WhenAll(requests);

        Assert.All(results, result => Assert.Single(result)); // every request is still answered
        Assert.Equal(LeaderboardCache.MaxEntries, _cache.Count);
    }

    private static Task<IReadOnlyList<LeaderboardEntryDto>> LoadAsync(ref int loads)
    {
        loads++;
        return Task.FromResult<IReadOnlyList<LeaderboardEntryDto>>([CreateEntry()]);
    }

    private static LeaderboardQuery CreateQuery(Difficulty difficulty = Difficulty.Easy, string? topic = null) =>
        new(LeaderboardSortBy.TypingSpeed, ProgrammingLanguage.CSharp, difficulty, topic);

    private static LeaderboardEntryDto CreateEntry() =>
        new(
            Rank: 1,
            UserId: Guid.NewGuid(),
            UserName: "player",
            RaceResultId: Guid.NewGuid(),
            TypingSpeed: 100,
            Accuracy: 95,
            CompletionTime: TimeSpan.FromSeconds(15),
            Mistakes: 0,
            CreatedAt: DateTime.UnixEpoch);

    /// <summary>A clock the test moves forward by hand, to check expiry without waiting 30 seconds.</summary>
    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

        public void Advance(TimeSpan by) => _now += by;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}

using CodeRacer.Server.Dtos;

namespace CodeRacer.Server.Models.Multiplayer;

public enum LobbyStatus
{
    Waiting,
    Racing,
    Finished
}

/// <summary>
/// An active multiplayer lobby kept in memory.
/// <para>
/// Several SignalR connections change the same lobby at the same time (joins, leaves, progress
/// updates), so every read and write of its state happens while <see cref="SyncRoot"/> is locked.
/// Most changes are several steps (check the status, then add a player; remove a player, then pick
/// a new host), and without the lock two connections could interleave and, for example, both pass
/// the <see cref="MaxPlayers"/> check. <c>lock</c> fits here because nothing inside it is awaited.
/// </para>
/// </summary>
public sealed class LobbyRoom
{
    /// <summary>Free-for-all races allow up to 5 players (see ROADMAP.md).</summary>
    public const int MaxPlayers = 5;

    public required string Id { get; init; }

    public required DateTime CreatedAt { get; init; }

    /// <summary>Lock that guards all mutable state of this lobby and its players.</summary>
    public object SyncRoot { get; } = new();

    public string HostConnectionId { get; set; } = string.Empty;

    public LobbyStatus Status { get; set; } = LobbyStatus.Waiting;

    public CodeSnippet? Snippet { get; set; }

    /// <summary>
    /// Set once the lobby is removed from the manager, so a connection that still holds a
    /// reference to it cannot join an orphaned lobby.
    /// </summary>
    public bool IsClosed { get; set; }

    public long NextJoinOrder { get; set; }

    /// <summary>Players by SignalR connection ID. Guarded by <see cref="SyncRoot"/>.</summary>
    public Dictionary<string, LobbyPlayer> Players { get; } = new();

    /// <summary>Takes a consistent snapshot of the lobby for sending to clients.</summary>
    public LobbyRoomDto ToDto()
    {
        lock (SyncRoot)
        {
            var players = Players.Values
                .OrderBy(player => player.JoinOrder)
                .Select(player => new LobbyPlayerDto(
                    player.ConnectionId,
                    player.UserName,
                    player.IsHost,
                    player.ProgressPercentage,
                    player.Wpm,
                    player.IsFinished,
                    player.FinishedAt))
                .ToList();

            var hostUserName = Players.TryGetValue(HostConnectionId, out var host) ? host.UserName : string.Empty;

            return new LobbyRoomDto(Id, HostConnectionId, hostUserName, Status.ToString(), players, CreatedAt);
        }
    }
}

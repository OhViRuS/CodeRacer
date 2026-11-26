namespace CodeRacer.Server.Models.Multiplayer;

/// <summary>
/// A player in a multiplayer lobby. Its mutable state is only changed while the owning
/// <see cref="LobbyRoom.SyncRoot"/> lock is held.
/// </summary>
public sealed class LobbyPlayer
{
    public required string ConnectionId { get; init; }

    public required string UserName { get; init; }

    /// <summary>Order in which the player joined; the earliest remaining player becomes the next host.</summary>
    public required long JoinOrder { get; init; }

    public bool IsHost { get; set; }

    public double ProgressPercentage { get; set; }

    public double Wpm { get; set; }

    public DateTime? FinishedAt { get; set; }

    public bool IsFinished => FinishedAt is not null;
}

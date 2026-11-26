namespace CodeRacer.Server.Models.Multiplayer;

public enum JoinLobbyResult
{
    Joined,
    NotFound,
    AlreadyStarted,
    Full
}

public enum StartRaceResult
{
    Started,
    NotFound,
    NotHost,
    AlreadyRacing
}

/// <param name="RoomClosed">True when the player was the last one and the lobby was removed.</param>
/// <param name="RaceFinished">True when the player's departure left only finished players in a running race.</param>
public sealed record LeaveLobbyResult(LobbyRoom Room, LobbyPlayer Player, bool RoomClosed, bool RaceFinished);

/// <param name="RaceFinished">True only for the update that made the last player finish.</param>
public sealed record ProgressUpdateResult(LobbyRoom Room, LobbyPlayer Player, bool RaceFinished);

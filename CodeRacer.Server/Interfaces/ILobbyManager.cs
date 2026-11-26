using CodeRacer.Server.Models;
using CodeRacer.Server.Models.Multiplayer;

namespace CodeRacer.Server.Interfaces;

/// <summary>
/// Active multiplayer lobbies kept in memory and shared by all SignalR connections.
/// </summary>
public interface ILobbyManager
{
    /// <summary>Creates a lobby with the caller as host, leaving any lobby the caller was in.</summary>
    LobbyRoom CreateLobby(string hostConnectionId, string hostUserName);

    /// <summary>Finds a lobby by its code (case-insensitive).</summary>
    LobbyRoom? GetLobby(string lobbyId);

    /// <summary>Finds the lobby the given connection is in.</summary>
    LobbyRoom? GetLobbyByConnectionId(string connectionId);

    /// <summary>
    /// Adds a player to a lobby that is not currently racing and not full.
    /// The caller leaves their previous lobby only when joining the new one succeeds.
    /// </summary>
    JoinLobbyResult JoinLobby(string lobbyId, string connectionId, string userName, out LobbyRoom? room);

    /// <summary>
    /// Adds a player to a lobby that is not currently racing and not full.
    /// The caller leaves their previous lobby only when joining the new one succeeds, returning
    /// the departure details in <paramref name="leftPreviousLobby"/>.
    /// </summary>
    JoinLobbyResult JoinLobby(
        string lobbyId,
        string connectionId,
        string userName,
        out LobbyRoom? room,
        out LeaveLobbyResult? leftPreviousLobby);

    /// <summary>Starts a race with <paramref name="snippet"/> for every player. Only the host can start it.</summary>
    StartRaceResult StartRace(string lobbyId, string hostConnectionId, CodeSnippet snippet, out LobbyRoom? room);

    /// <summary>
    /// Records a player's typing progress during a race. Returns null when the update is ignored
    /// (not in a lobby, no race running, player already finished or invalid values).
    /// </summary>
    ProgressUpdateResult? UpdateProgress(string connectionId, double progressPercentage, double wpm);

    /// <summary>
    /// Removes a connection from its lobby, handing the host role to the earliest remaining player
    /// and removing the lobby when it becomes empty. Returns null when the connection was in no lobby.
    /// </summary>
    LeaveLobbyResult? LeaveLobby(string connectionId);

    /// <summary>
    /// Removes a lobby. Only the host can close it. Returns the connection IDs that were in the lobby,
    /// or null when the lobby does not exist or the caller is not the host.
    /// </summary>
    IReadOnlyList<string>? CloseLobby(string lobbyId, string hostConnectionId);
}

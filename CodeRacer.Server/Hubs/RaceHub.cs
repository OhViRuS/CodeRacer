using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using CodeRacer.Server.Models.Multiplayer;
using Microsoft.AspNetCore.SignalR;

namespace CodeRacer.Server.Hubs;

/// <summary>
/// Real-time endpoint for multiplayer races (mapped to /hubs/race).
/// Lobby state lives in <see cref="ILobbyManager"/>; this hub validates input, calls the manager
/// and tells the players in the lobby's SignalR group what changed.
/// </summary>
public class RaceHub(ILobbyManager lobbyManager, ICodeSnippetProvider snippetProvider) : Hub
{
    /// <summary>Same limit as a registered user name (see User).</summary>
    public const int MaxUserNameLength = 50;

    private const string DefaultUserName = "Guest";

    private readonly ILobbyManager _lobbyManager = lobbyManager;
    private readonly ICodeSnippetProvider _snippetProvider = snippetProvider;

    /// <summary>Creates a lobby with the caller as host and returns it.</summary>
    public async Task<LobbyRoomDto> CreateLobby(string? userName)
    {
        var normalizedUserName = NormalizeUserName(userName);

        // Leave any previous lobby first so its remaining players and SignalR group are updated.
        await HandleLeaveAsync(Context.ConnectionAborted);

        var room = _lobbyManager.CreateLobby(Context.ConnectionId, normalizedUserName);
        await Groups.AddToGroupAsync(Context.ConnectionId, room.Id, Context.ConnectionAborted);

        return room.ToDto();
    }

    /// <summary>Joins a lobby by its code, tells the other players and returns the lobby.</summary>
    public async Task<LobbyRoomDto> JoinLobby(string? lobbyId, string? userName)
    {
        var normalizedLobbyId = NormalizeLobbyId(lobbyId);
        var normalizedUserName = NormalizeUserName(userName);
        var result = _lobbyManager.JoinLobby(
            normalizedLobbyId,
            Context.ConnectionId,
            normalizedUserName,
            out var room,
            out var leftPreviousLobby);

        if (result != JoinLobbyResult.Joined || room is null)
        {
            throw new HubException(result switch
            {
                JoinLobbyResult.AlreadyStarted => "This race has already started.",
                JoinLobbyResult.Full => $"This lobby is full (maximum {LobbyRoom.MaxPlayers} players).",
                _ => $"Lobby '{normalizedLobbyId}' was not found."
            });
        }

        if (leftPreviousLobby is not null)
        {
            await NotifyLeaveAsync(leftPreviousLobby, Context.ConnectionAborted);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, room.Id, Context.ConnectionAborted);

        var lobby = room.ToDto();
        await Clients.OthersInGroup(room.Id).SendAsync(RaceHubEvents.LobbyUpdated, lobby, Context.ConnectionAborted);

        return lobby;
    }

    /// <summary>Starts a race for everyone in the lobby with a snippet matching the filters. Host only.</summary>
    public async Task StartGame(string? lobbyId, ProgrammingLanguage? language, Difficulty? difficulty, string? topic)
    {
        var normalizedLobbyId = NormalizeLobbyId(lobbyId);

        // Checked before the database call so non-hosts cannot trigger queries; checked again under the lock below.
        var existing = _lobbyManager.GetLobby(normalizedLobbyId) ?? throw new HubException("Lobby not found.");
        if (existing.ToDto().HostConnectionId != Context.ConnectionId)
        {
            throw new HubException("Only the host can start the race.");
        }

        var snippet = await _snippetProvider.GetRandomAsync(language, difficulty, topic, Context.ConnectionAborted)
            ?? throw new HubException("No code snippets match the selected filters.");

        var result = _lobbyManager.StartRace(normalizedLobbyId, Context.ConnectionId, snippet, out var room);
        if (result != StartRaceResult.Started || room is null)
        {
            throw new HubException(result switch
            {
                StartRaceResult.NotHost => "Only the host can start the race.",
                StartRaceResult.AlreadyRacing => "The race is already running.",
                _ => "Lobby not found."
            });
        }

        await Clients.Group(room.Id).SendAsync(
            RaceHubEvents.GameStarted,
            new GameStartedDto(snippet, room.ToDto()),
            Context.ConnectionAborted);
    }

    /// <summary>Shares the caller's typing progress with the lobby and ends the race when everyone has finished.</summary>
    public async Task UpdateProgress(double progressPercentage, double wpm)
    {
        var result = _lobbyManager.UpdateProgress(Context.ConnectionId, progressPercentage, wpm);
        if (result is null)
        {
            return;
        }

        var progress = new PlayerProgressDto(
            Context.ConnectionId,
            result.Player.ProgressPercentage,
            result.Player.Wpm,
            result.Player.IsFinished);

        await Clients.Group(result.Room.Id).SendAsync(RaceHubEvents.PlayerProgressUpdated, progress, Context.ConnectionAborted);

        if (result.RaceFinished)
        {
            await Clients.Group(result.Room.Id).SendAsync(RaceHubEvents.GameFinished, result.Room.ToDto(), Context.ConnectionAborted);
        }
    }

    /// <summary>Leaves the caller's current lobby.</summary>
    public Task LeaveLobby() => HandleLeaveAsync(Context.ConnectionAborted);

    /// <summary>Closes the lobby for everyone. Host only.</summary>
    public async Task CloseLobby(string? lobbyId)
    {
        var normalizedLobbyId = NormalizeLobbyId(lobbyId);
        var connectionIds = _lobbyManager.CloseLobby(normalizedLobbyId, Context.ConnectionId)
            ?? throw new HubException("Only the host can close the lobby.");

        await Clients.Group(normalizedLobbyId).SendAsync(RaceHubEvents.LobbyClosed, normalizedLobbyId, Context.ConnectionAborted);

        foreach (var connectionId in connectionIds)
        {
            await Groups.RemoveFromGroupAsync(connectionId, normalizedLobbyId, Context.ConnectionAborted);
        }
    }

    /// <summary>Removes a player whose browser tab closed or whose connection dropped.</summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // The connection is already gone, so its ConnectionAborted token is cancelled; notify the others anyway.
        await HandleLeaveAsync(CancellationToken.None);
        await base.OnDisconnectedAsync(exception);
    }

    private async Task HandleLeaveAsync(CancellationToken cancellationToken)
    {
        var result = _lobbyManager.LeaveLobby(Context.ConnectionId);
        if (result is not null)
        {
            await NotifyLeaveAsync(result, cancellationToken);
        }
    }

    private async Task NotifyLeaveAsync(LeaveLobbyResult result, CancellationToken cancellationToken)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, result.Room.Id, cancellationToken);

        if (result.RoomClosed || result.Room.IsClosed)
        {
            return;
        }

        var lobby = result.Room.ToDto();
        await Clients.Group(result.Room.Id).SendAsync(
            RaceHubEvents.PlayerLeft,
            new PlayerLeftDto(Context.ConnectionId, result.Player.UserName, lobby),
            cancellationToken);

        if (result.RaceFinished)
        {
            await Clients.Group(result.Room.Id).SendAsync(RaceHubEvents.GameFinished, lobby, cancellationToken);
        }
    }

    private static string NormalizeLobbyId(string? lobbyId) =>
        lobbyId?.Trim().ToUpperInvariant() ?? string.Empty;

    private static string NormalizeUserName(string? userName)
    {
        var trimmed = userName?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return DefaultUserName;
        }

        if (trimmed.Length > MaxUserNameLength)
        {
            throw new HubException($"User name must be at most {MaxUserNameLength} characters.");
        }

        return trimmed;
    }
}

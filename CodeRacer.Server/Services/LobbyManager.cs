using System.Collections.Concurrent;
using System.Security.Cryptography;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using CodeRacer.Server.Models.Multiplayer;

namespace CodeRacer.Server.Services;

/// <summary>
/// Keeps active multiplayer lobbies in memory.
/// <para>
/// Registered as a singleton, so every SignalR connection uses the same instance at the same time:
/// </para>
/// <list type="bullet">
/// <item>The lobby index and the connection-to-lobby index are
/// <see cref="ConcurrentDictionary{TKey, TValue}"/>s, so lookups, inserts and removals from many
/// connections at once cannot corrupt them.</item>
/// <item>Changes inside one lobby are guarded by that lobby's <see cref="LobbyRoom.SyncRoot"/> lock,
/// because they are check-then-act steps (is it full? is the race running? who is the next host?)
/// that must not interleave with another connection's changes.</item>
/// </list>
/// </summary>
public sealed class LobbyManager(TimeProvider timeProvider) : ILobbyManager
{
    public const int LobbyIdLength = 6;

    // Leaves out characters that are easy to confuse: O and 0, I and 1.
    private const string IdCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ConcurrentDictionary<string, LobbyRoom> _lobbies = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _connectionToLobbyId = new();

    public LobbyRoom CreateLobby(string hostConnectionId, string hostUserName)
    {
        LeaveLobby(hostConnectionId);

        while (true)
        {
            var room = new LobbyRoom
            {
                Id = GenerateId(),
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                HostConnectionId = hostConnectionId
            };

            room.Players[hostConnectionId] = CreatePlayer(room, hostConnectionId, hostUserName, isHost: true);

            // TryAdd instead of a separate "contains" check, so two lobbies can never get the same code.
            if (_lobbies.TryAdd(room.Id, room))
            {
                _connectionToLobbyId[hostConnectionId] = room.Id;
                return room;
            }
        }
    }

    public LobbyRoom? GetLobby(string lobbyId) =>
        _lobbies.TryGetValue(lobbyId, out var room) ? room : null;

    public LobbyRoom? GetLobbyByConnectionId(string connectionId) =>
        _connectionToLobbyId.TryGetValue(connectionId, out var lobbyId) ? GetLobby(lobbyId) : null;

    public JoinLobbyResult JoinLobby(string lobbyId, string connectionId, string userName, out LobbyRoom? room)
    {
        LeaveLobby(connectionId);

        room = GetLobby(lobbyId);
        if (room is null)
        {
            return JoinLobbyResult.NotFound;
        }

        lock (room.SyncRoot)
        {
            if (room.IsClosed)
            {
                room = null;
                return JoinLobbyResult.NotFound;
            }

            if (room.Status != LobbyStatus.Waiting)
            {
                room = null;
                return JoinLobbyResult.AlreadyStarted;
            }

            if (room.Players.Count >= LobbyRoom.MaxPlayers)
            {
                room = null;
                return JoinLobbyResult.Full;
            }

            room.Players[connectionId] = CreatePlayer(room, connectionId, userName, isHost: false);
            _connectionToLobbyId[connectionId] = room.Id;
            return JoinLobbyResult.Joined;
        }
    }

    public StartRaceResult StartRace(string lobbyId, string hostConnectionId, CodeSnippet snippet, out LobbyRoom? room)
    {
        room = GetLobby(lobbyId);
        if (room is null)
        {
            return StartRaceResult.NotFound;
        }

        lock (room.SyncRoot)
        {
            if (room.IsClosed)
            {
                return StartRaceResult.NotFound;
            }

            if (room.HostConnectionId != hostConnectionId)
            {
                return StartRaceResult.NotHost;
            }

            if (room.Status == LobbyStatus.Racing)
            {
                return StartRaceResult.AlreadyRacing;
            }

            room.Snippet = snippet;
            room.Status = LobbyStatus.Racing;

            foreach (var player in room.Players.Values)
            {
                player.ProgressPercentage = 0;
                player.Wpm = 0;
                player.FinishedAt = null;
            }

            return StartRaceResult.Started;
        }
    }

    public ProgressUpdateResult? UpdateProgress(string connectionId, double progressPercentage, double wpm)
    {
        if (!double.IsFinite(progressPercentage) || !double.IsFinite(wpm))
        {
            return null;
        }

        var room = GetLobbyByConnectionId(connectionId);
        if (room is null)
        {
            return null;
        }

        lock (room.SyncRoot)
        {
            if (room.Status != LobbyStatus.Racing
                || !room.Players.TryGetValue(connectionId, out var player)
                || player.IsFinished)
            {
                return null;
            }

            player.ProgressPercentage = Math.Clamp(progressPercentage, 0, 100);
            player.Wpm = Math.Max(0, wpm);

            if (player.ProgressPercentage >= 100)
            {
                player.FinishedAt = _timeProvider.GetUtcNow().UtcDateTime;
            }

            return new ProgressUpdateResult(room, player, FinishRaceIfEveryoneFinished(room));
        }
    }

    public LeaveLobbyResult? LeaveLobby(string connectionId)
    {
        if (!_connectionToLobbyId.TryRemove(connectionId, out var lobbyId)
            || !_lobbies.TryGetValue(lobbyId, out var room))
        {
            return null;
        }

        lock (room.SyncRoot)
        {
            if (!room.Players.Remove(connectionId, out var player))
            {
                return null;
            }

            if (room.Players.Count == 0)
            {
                room.IsClosed = true;
                _lobbies.TryRemove(room.Id, out _);
                return new LeaveLobbyResult(room, player, RoomClosed: true, RaceFinished: false);
            }

            if (room.HostConnectionId == connectionId)
            {
                var nextHost = room.Players.Values.MinBy(remaining => remaining.JoinOrder)!;
                nextHost.IsHost = true;
                room.HostConnectionId = nextHost.ConnectionId;
            }

            return new LeaveLobbyResult(room, player, RoomClosed: false, FinishRaceIfEveryoneFinished(room));
        }
    }

    public IReadOnlyList<string>? CloseLobby(string lobbyId, string hostConnectionId)
    {
        var room = GetLobby(lobbyId);
        if (room is null)
        {
            return null;
        }

        lock (room.SyncRoot)
        {
            if (room.IsClosed || room.HostConnectionId != hostConnectionId)
            {
                return null;
            }

            room.IsClosed = true;
            _lobbies.TryRemove(room.Id, out _);

            var connectionIds = room.Players.Keys.ToList();
            foreach (var connectionId in connectionIds)
            {
                _connectionToLobbyId.TryRemove(connectionId, out _);
            }

            return connectionIds;
        }
    }

    // Must be called while room.SyncRoot is held.
    private static bool FinishRaceIfEveryoneFinished(LobbyRoom room)
    {
        if (room.Status != LobbyStatus.Racing || !room.Players.Values.All(player => player.IsFinished))
        {
            return false;
        }

        room.Status = LobbyStatus.Finished;
        return true;
    }

    // Must be called while room.SyncRoot is held (or before the room is shared).
    private static LobbyPlayer CreatePlayer(LobbyRoom room, string connectionId, string userName, bool isHost) =>
        new()
        {
            ConnectionId = connectionId,
            UserName = userName,
            JoinOrder = room.NextJoinOrder++,
            IsHost = isHost
        };

    private static string GenerateId()
    {
        Span<char> id = stackalloc char[LobbyIdLength];
        for (var i = 0; i < id.Length; i++)
        {
            id[i] = IdCharacters[RandomNumberGenerator.GetInt32(IdCharacters.Length)];
        }

        return new string(id);
    }
}

using CodeRacer.Server.Models;

namespace CodeRacer.Server.Dtos;

public record LobbyPlayerDto(
    string ConnectionId,
    string UserName,
    bool IsHost,
    double ProgressPercentage,
    double Wpm,
    bool IsFinished,
    DateTime? FinishedAt);

public record LobbyRoomDto(
    string Id,
    string HostConnectionId,
    string HostUserName,
    string Status,
    IReadOnlyList<LobbyPlayerDto> Players,
    DateTime CreatedAt);

public record PlayerProgressDto(
    string ConnectionId,
    double ProgressPercentage,
    double Wpm,
    bool IsFinished);

public record PlayerLeftDto(
    string ConnectionId,
    string UserName,
    LobbyRoomDto Lobby);

public record GameStartedDto(
    CodeSnippet Snippet,
    LobbyRoomDto Lobby);

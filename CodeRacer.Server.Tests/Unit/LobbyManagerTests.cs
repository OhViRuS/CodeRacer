using CodeRacer.Server.Models;
using CodeRacer.Server.Models.Multiplayer;
using CodeRacer.Server.Services;
using CodeRacer.Server.Tests.TestDoubles;

namespace CodeRacer.Server.Tests.Unit;

public class LobbyManagerTests
{
    private readonly FixedTimeProvider _clock = new();
    private readonly LobbyManager _lobbyManager;

    public LobbyManagerTests()
    {
        _lobbyManager = new LobbyManager(_clock);
    }

    [Fact]
    public void CreateLobby_CreatesWaitingLobbyWithCallerAsHost()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");
        var lobby = room.ToDto();

        Assert.Equal(LobbyManager.LobbyIdLength, lobby.Id.Length);
        Assert.Equal(FixedTimeProvider.DefaultNow.UtcDateTime, lobby.CreatedAt);
        Assert.Equal(nameof(LobbyStatus.Waiting), lobby.Status);
        Assert.Equal("host", lobby.HostConnectionId);
        Assert.Equal("Alice", lobby.HostUserName);
        var player = Assert.Single(lobby.Players);
        Assert.True(player.IsHost);
    }

    [Fact]
    public void GetLobby_IgnoresCaseOfTheCode()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");

        Assert.Same(room, _lobbyManager.GetLobby(room.Id.ToLowerInvariant()));
        Assert.Null(_lobbyManager.GetLobby("NOPE00"));
    }

    [Fact]
    public void GetLobbyByConnectionId_FindsOnlyTheConnectionsLobby()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");

        Assert.Same(room, _lobbyManager.GetLobbyByConnectionId("host"));
        Assert.Null(_lobbyManager.GetLobbyByConnectionId("stranger"));
    }

    [Fact]
    public void JoinLobby_WaitingLobby_AddsPlayerAsNonHost()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");

        var result = _lobbyManager.JoinLobby(room.Id, "guest", "Bob", out var joined);

        Assert.Equal(JoinLobbyResult.Joined, result);
        Assert.Same(room, joined);
        Assert.Equal(["Alice", "Bob"], room.ToDto().Players.Select(player => player.UserName));
        Assert.False(room.Players["guest"].IsHost);
    }

    [Fact]
    public void JoinLobby_UnknownCode_ReturnsNotFound()
    {
        var result = _lobbyManager.JoinLobby("NOPE00", "guest", "Bob", out var room);

        Assert.Equal(JoinLobbyResult.NotFound, result);
        Assert.Null(room);
    }

    [Fact]
    public void JoinLobby_RaceAlreadyStarted_ReturnsAlreadyStarted()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");
        _lobbyManager.StartRace(room.Id, "host", CreateSnippet(), out _);

        var result = _lobbyManager.JoinLobby(room.Id, "guest", "Bob", out var joined);

        Assert.Equal(JoinLobbyResult.AlreadyStarted, result);
        Assert.Null(joined);
    }

    [Fact]
    public void JoinLobby_FullLobby_ReturnsFull()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");
        for (var i = 1; i < LobbyRoom.MaxPlayers; i++)
        {
            _lobbyManager.JoinLobby(room.Id, $"guest{i}", $"Player{i}", out _);
        }

        var result = _lobbyManager.JoinLobby(room.Id, "late", "Late", out var joined);

        Assert.Equal(JoinLobbyResult.Full, result);
        Assert.Null(joined);
        Assert.Equal(LobbyRoom.MaxPlayers, room.Players.Count);
    }

    [Fact]
    public async Task JoinLobby_ManyConnectionsAtOnce_NeverExceedsMaxPlayers()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");

        var results = await Task.WhenAll(Enumerable.Range(0, 50).Select(i =>
            Task.Run(() => _lobbyManager.JoinLobby(room.Id, $"guest{i}", $"Player{i}", out _))));

        Assert.Equal(LobbyRoom.MaxPlayers - 1, results.Count(result => result == JoinLobbyResult.Joined));
        Assert.Equal(LobbyRoom.MaxPlayers, room.Players.Count);
    }

    [Fact]
    public void LeaveLobby_NonHostLeaves_KeepsHostAndRemovesPlayer()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");
        _lobbyManager.JoinLobby(room.Id, "guest", "Bob", out _);

        var result = _lobbyManager.LeaveLobby("guest");

        Assert.NotNull(result);
        Assert.False(result.RoomClosed);
        Assert.Equal("Bob", result.Player.UserName);
        Assert.Equal("host", room.HostConnectionId);
        Assert.Null(_lobbyManager.GetLobbyByConnectionId("guest"));
    }

    [Fact]
    public void LeaveLobby_HostLeaves_EarliestRemainingPlayerBecomesHost()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");
        _lobbyManager.JoinLobby(room.Id, "second", "Bob", out _);
        _lobbyManager.JoinLobby(room.Id, "third", "Carol", out _);

        _lobbyManager.LeaveLobby("host");

        var lobby = room.ToDto();
        Assert.Equal("second", lobby.HostConnectionId);
        Assert.Equal("Bob", lobby.HostUserName);
        Assert.True(room.Players["second"].IsHost);
        Assert.False(room.Players["third"].IsHost);
    }

    [Fact]
    public void LeaveLobby_LastPlayerLeaves_ClosesAndRemovesLobby()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");

        var result = _lobbyManager.LeaveLobby("host");

        Assert.NotNull(result);
        Assert.True(result.RoomClosed);
        Assert.True(room.IsClosed);
        Assert.Null(_lobbyManager.GetLobby(room.Id));
    }

    [Fact]
    public void LeaveLobby_ConnectionInNoLobby_ReturnsNull()
    {
        Assert.Null(_lobbyManager.LeaveLobby("stranger"));
    }

    [Fact]
    public void CreateLobby_CallerAlreadyInLobby_LeavesThePreviousLobby()
    {
        var first = _lobbyManager.CreateLobby("host", "Alice");
        _lobbyManager.JoinLobby(first.Id, "guest", "Bob", out _);

        var second = _lobbyManager.CreateLobby("host", "Alice");

        Assert.NotEqual(first.Id, second.Id);
        Assert.False(first.Players.ContainsKey("host"));
        Assert.Equal("guest", first.HostConnectionId);
        Assert.Same(second, _lobbyManager.GetLobbyByConnectionId("host"));
    }

    [Fact]
    public void StartRace_ByHost_SetsSnippetAndResetsProgress()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");
        var snippet = CreateSnippet();

        var result = _lobbyManager.StartRace(room.Id, "host", snippet, out var started);

        Assert.Equal(StartRaceResult.Started, result);
        Assert.Same(room, started);
        Assert.Equal(LobbyStatus.Racing, room.Status);
        Assert.Same(snippet, room.Snippet);
    }

    [Fact]
    public void StartRace_FinishedRace_CanBeRestartedWithProgressReset()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");
        _lobbyManager.StartRace(room.Id, "host", CreateSnippet(), out _);
        _lobbyManager.UpdateProgress("host", 100, 80);

        var result = _lobbyManager.StartRace(room.Id, "host", CreateSnippet(), out _);

        Assert.Equal(StartRaceResult.Started, result);
        var player = room.Players["host"];
        Assert.Equal(0, player.ProgressPercentage);
        Assert.Equal(0, player.Wpm);
        Assert.False(player.IsFinished);
    }

    [Fact]
    public void StartRace_ByNonHost_ReturnsNotHost()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");
        _lobbyManager.JoinLobby(room.Id, "guest", "Bob", out _);

        var result = _lobbyManager.StartRace(room.Id, "guest", CreateSnippet(), out _);

        Assert.Equal(StartRaceResult.NotHost, result);
        Assert.Equal(LobbyStatus.Waiting, room.Status);
    }

    [Fact]
    public void StartRace_WhileRacing_ReturnsAlreadyRacing()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");
        _lobbyManager.StartRace(room.Id, "host", CreateSnippet(), out _);

        Assert.Equal(StartRaceResult.AlreadyRacing, _lobbyManager.StartRace(room.Id, "host", CreateSnippet(), out _));
    }

    [Fact]
    public void StartRace_UnknownLobby_ReturnsNotFound()
    {
        Assert.Equal(StartRaceResult.NotFound, _lobbyManager.StartRace("NOPE00", "host", CreateSnippet(), out var room));
        Assert.Null(room);
    }

    [Fact]
    public void UpdateProgress_ClampsValues()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");
        _lobbyManager.JoinLobby(room.Id, "guest", "Bob", out _);
        _lobbyManager.StartRace(room.Id, "host", CreateSnippet(), out _);

        var result = _lobbyManager.UpdateProgress("host", -10, -5);

        Assert.NotNull(result);
        Assert.Equal(0, result.Player.ProgressPercentage);
        Assert.Equal(0, result.Player.Wpm);
        Assert.False(result.RaceFinished);
    }

    [Fact]
    public void UpdateProgress_LastPlayerFinishes_FinishesRaceOnce()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");
        _lobbyManager.JoinLobby(room.Id, "guest", "Bob", out _);
        _lobbyManager.StartRace(room.Id, "host", CreateSnippet(), out _);

        var first = _lobbyManager.UpdateProgress("host", 100, 90);
        var last = _lobbyManager.UpdateProgress("guest", 150, 70);
        var afterFinish = _lobbyManager.UpdateProgress("guest", 100, 70);

        Assert.False(first!.RaceFinished);
        Assert.Equal(FixedTimeProvider.DefaultNow.UtcDateTime, first.Player.FinishedAt);
        Assert.True(last!.RaceFinished);
        Assert.Equal(100, last.Player.ProgressPercentage);
        Assert.Null(afterFinish);
        Assert.Equal(LobbyStatus.Finished, room.Status);
    }

    [Theory]
    [InlineData(double.NaN, 50)]
    [InlineData(50, double.PositiveInfinity)]
    public void UpdateProgress_NonFiniteValues_AreIgnored(double progress, double wpm)
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");
        _lobbyManager.StartRace(room.Id, "host", CreateSnippet(), out _);

        Assert.Null(_lobbyManager.UpdateProgress("host", progress, wpm));
    }

    [Fact]
    public void UpdateProgress_NoRaceRunning_IsIgnored()
    {
        _lobbyManager.CreateLobby("host", "Alice");

        Assert.Null(_lobbyManager.UpdateProgress("host", 50, 60));
        Assert.Null(_lobbyManager.UpdateProgress("stranger", 50, 60));
    }

    [Fact]
    public void LeaveLobby_OnlyUnfinishedPlayerLeaves_FinishesRace()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");
        _lobbyManager.JoinLobby(room.Id, "guest", "Bob", out _);
        _lobbyManager.StartRace(room.Id, "host", CreateSnippet(), out _);
        _lobbyManager.UpdateProgress("host", 100, 90);

        var result = _lobbyManager.LeaveLobby("guest");

        Assert.True(result!.RaceFinished);
        Assert.Equal(LobbyStatus.Finished, room.Status);
    }

    [Fact]
    public void CloseLobby_ByHost_RemovesLobbyAndReturnsItsConnections()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");
        _lobbyManager.JoinLobby(room.Id, "guest", "Bob", out _);

        var connectionIds = _lobbyManager.CloseLobby(room.Id, "host");

        Assert.NotNull(connectionIds);
        Assert.Equal(["guest", "host"], connectionIds.Order());
        Assert.Null(_lobbyManager.GetLobby(room.Id));
        Assert.Null(_lobbyManager.GetLobbyByConnectionId("guest"));
        Assert.Equal(JoinLobbyResult.NotFound, _lobbyManager.JoinLobby(room.Id, "late", "Late", out _));
    }

    [Fact]
    public void CloseLobby_ByNonHost_ReturnsNullAndKeepsLobby()
    {
        var room = _lobbyManager.CreateLobby("host", "Alice");
        _lobbyManager.JoinLobby(room.Id, "guest", "Bob", out _);

        Assert.Null(_lobbyManager.CloseLobby(room.Id, "guest"));
        Assert.Same(room, _lobbyManager.GetLobby(room.Id));
    }

    private static CodeSnippet CreateSnippet() => new()
    {
        Id = Guid.NewGuid(),
        CodeText = "print('hello')",
        Language = ProgrammingLanguage.Python,
        Difficulty = Difficulty.Easy,
        ProgrammingConcept = "Output"
    };
}

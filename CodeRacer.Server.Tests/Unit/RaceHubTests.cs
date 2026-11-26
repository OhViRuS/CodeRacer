using CodeRacer.Server.Dtos;
using CodeRacer.Server.Hubs;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using CodeRacer.Server.Models.Multiplayer;
using CodeRacer.Server.Services;
using CodeRacer.Server.Tests.TestDoubles;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace CodeRacer.Server.Tests.Unit;

public class RaceHubTests
{
    private readonly FixedTimeProvider _clock = new();
    private readonly ILobbyManager _lobbyManager;
    private readonly ICodeSnippetProvider _snippetProvider = Substitute.For<ICodeSnippetProvider>();
    private readonly IHubCallerClients _clients = Substitute.For<IHubCallerClients>();
    private readonly IClientProxy _groupProxy = Substitute.For<IClientProxy>();
    private readonly IClientProxy _othersInGroupProxy = Substitute.For<IClientProxy>();
    private readonly IGroupManager _groups = Substitute.For<IGroupManager>();

    public RaceHubTests()
    {
        _lobbyManager = new LobbyManager(_clock);
        _clients.Group(Arg.Any<string>()).Returns(_groupProxy);
        _clients.OthersInGroup(Arg.Any<string>()).Returns(_othersInGroupProxy);
    }

    [Fact]
    public async Task CreateLobby_ValidUserName_CreatesLobbyAndAddsCallerToGroup()
    {
        using var hub = CreateHub("host-1");

        var lobby = await hub.CreateLobby("  Alice  ");

        Assert.Equal("Alice", lobby.HostUserName);
        Assert.Equal("host-1", lobby.HostConnectionId);
        await _groups.Received(1).AddToGroupAsync("host-1", lobby.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateLobby_BlankUserName_DefaultsToGuest()
    {
        using var hub = CreateHub("host-1");

        var lobby = await hub.CreateLobby("   ");

        Assert.Equal("Guest", lobby.HostUserName);
    }

    [Fact]
    public async Task CreateLobby_TooLongUserName_ThrowsHubException()
    {
        using var hub = CreateHub("host-1");
        var oversizedName = new string('a', RaceHub.MaxUserNameLength + 1);

        await Assert.ThrowsAsync<HubException>(() => hub.CreateLobby(oversizedName));
    }

    [Fact]
    public async Task JoinLobby_ExistingWaitingLobby_AddsToGroupAndNotifiesOthers()
    {
        using var hostHub = CreateHub("host-1");
        var created = await hostHub.CreateLobby("Alice");

        using var guestHub = CreateHub("guest-1");
        var joined = await guestHub.JoinLobby($" {created.Id.ToLowerInvariant()} ", "Bob");

        Assert.Equal(2, joined.Players.Count);
        await _groups.Received(1).AddToGroupAsync("guest-1", created.Id, Arg.Any<CancellationToken>());
        await _othersInGroupProxy.Received(1).SendCoreAsync(
            RaceHubEvents.LobbyUpdated,
            Arg.Is<object?[]>(args => args.Length == 1 && args[0] is LobbyRoomDto),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinLobby_NonExistentLobby_ThrowsHubException()
    {
        using var guestHub = CreateHub("guest-1");

        var ex = await Assert.ThrowsAsync<HubException>(() => guestHub.JoinLobby("NOPE00", "Bob"));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartGame_ByHost_BroadcastsGameStartedWithSnippet()
    {
        using var hostHub = CreateHub("host-1");
        var created = await hostHub.CreateLobby("Alice");
        var snippet = new CodeSnippet
        {
            Id = Guid.NewGuid(),
            CodeText = "print(1)",
            Language = ProgrammingLanguage.Python,
            Difficulty = Difficulty.Easy,
            ProgrammingConcept = "Basics"
        };
        _snippetProvider.GetRandomAsync(ProgrammingLanguage.Python, Difficulty.Easy, "Basics", Arg.Any<CancellationToken>())
            .Returns(snippet);

        await hostHub.StartGame(created.Id, ProgrammingLanguage.Python, Difficulty.Easy, "Basics");

        await _groupProxy.Received(1).SendCoreAsync(
            RaceHubEvents.GameStarted,
            Arg.Is<object?[]>(args => args.Length == 1 && args[0] is GameStartedDto),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartGame_ByNonHost_ThrowsHubExceptionWithoutQueryingSnippets()
    {
        using var hostHub = CreateHub("host-1");
        var created = await hostHub.CreateLobby("Alice");
        using var guestHub = CreateHub("guest-1");
        await guestHub.JoinLobby(created.Id, "Bob");

        await Assert.ThrowsAsync<HubException>(() =>
            guestHub.StartGame(created.Id, ProgrammingLanguage.Python, Difficulty.Easy, null));

        await _snippetProvider.DidNotReceiveWithAnyArgs().GetRandomAsync(default, default, default, default);
    }

    [Fact]
    public async Task StartGame_NoMatchingSnippet_ThrowsHubException()
    {
        using var hostHub = CreateHub("host-1");
        var created = await hostHub.CreateLobby("Alice");
        _snippetProvider.GetRandomAsync(Arg.Any<ProgrammingLanguage?>(), Arg.Any<Difficulty?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((CodeSnippet?)null);

        await Assert.ThrowsAsync<HubException>(() =>
            hostHub.StartGame(created.Id, ProgrammingLanguage.Cpp, Difficulty.Hard, "Pointers"));
    }

    [Fact]
    public async Task UpdateProgress_PlayerFinishes_BroadcastsProgressAndGameFinished()
    {
        using var hostHub = CreateHub("host-1");
        var created = await hostHub.CreateLobby("Alice");
        var snippet = new CodeSnippet
        {
            Id = Guid.NewGuid(),
            CodeText = "print(1)",
            Language = ProgrammingLanguage.Python,
            Difficulty = Difficulty.Easy,
            ProgrammingConcept = "Basics"
        };
        _snippetProvider.GetRandomAsync(Arg.Any<ProgrammingLanguage?>(), Arg.Any<Difficulty?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(snippet);
        await hostHub.StartGame(created.Id, ProgrammingLanguage.Python, Difficulty.Easy, null);

        await hostHub.UpdateProgress(100, 95.5);

        await _groupProxy.Received(1).SendCoreAsync(
            RaceHubEvents.PlayerProgressUpdated,
            Arg.Is<object?[]>(args => args.Length == 1 && args[0] is PlayerProgressDto),
            Arg.Any<CancellationToken>());
        await _groupProxy.Received(1).SendCoreAsync(
            RaceHubEvents.GameFinished,
            Arg.Is<object?[]>(args => args.Length == 1 && args[0] is LobbyRoomDto),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeaveLobby_WithRemainingPlayers_BroadcastsPlayerLeft()
    {
        using var hostHub = CreateHub("host-1");
        var created = await hostHub.CreateLobby("Alice");
        using var guestHub = CreateHub("guest-1");
        await guestHub.JoinLobby(created.Id, "Bob");

        await guestHub.LeaveLobby();

        await _groups.Received(1).RemoveFromGroupAsync("guest-1", created.Id, Arg.Any<CancellationToken>());
        await _groupProxy.Received(1).SendCoreAsync(
            RaceHubEvents.PlayerLeft,
            Arg.Is<object?[]>(args => args.Length == 1 && args[0] is PlayerLeftDto),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CloseLobby_ByHost_BroadcastsLobbyClosedAndRemovesAllFromGroup()
    {
        using var hostHub = CreateHub("host-1");
        var created = await hostHub.CreateLobby("Alice");
        using var guestHub = CreateHub("guest-1");
        await guestHub.JoinLobby(created.Id, "Bob");

        await hostHub.CloseLobby(created.Id);

        await _groupProxy.Received(1).SendCoreAsync(
            RaceHubEvents.LobbyClosed,
            Arg.Is<object?[]>(args => args.Length == 1 && Equals(args[0], created.Id)),
            Arg.Any<CancellationToken>());
        await _groups.Received(1).RemoveFromGroupAsync("host-1", created.Id, Arg.Any<CancellationToken>());
        await _groups.Received(1).RemoveFromGroupAsync("guest-1", created.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateLobby_WhileInExistingLobby_RemovesFromOldGroupAndNotifiesOldLobby()
    {
        using var hostHub = CreateHub("host-1");
        var first = await hostHub.CreateLobby("Alice");
        using var guestHub = CreateHub("guest-1");
        await guestHub.JoinLobby(first.Id, "Bob");

        var second = await guestHub.CreateLobby("Bob");

        Assert.NotEqual(first.Id, second.Id);
        await _groups.Received(1).RemoveFromGroupAsync("guest-1", first.Id, Arg.Any<CancellationToken>());
        await _groupProxy.Received(1).SendCoreAsync(
            RaceHubEvents.PlayerLeft,
            Arg.Is<object?[]>(args => args.Length == 1 && args[0] is PlayerLeftDto && ((PlayerLeftDto)args[0]!).ConnectionId == "guest-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinLobby_WhileInExistingLobby_RemovesFromOldGroupAndNotifiesOldLobby()
    {
        using var host1Hub = CreateHub("host-1");
        var first = await host1Hub.CreateLobby("Alice");
        using var host2Hub = CreateHub("host-2");
        var second = await host2Hub.CreateLobby("Carol");
        using var guestHub = CreateHub("guest-1");
        await guestHub.JoinLobby(first.Id, "Bob");

        await guestHub.JoinLobby(second.Id, "Bob");

        await _groups.Received(1).RemoveFromGroupAsync("guest-1", first.Id, Arg.Any<CancellationToken>());
        await _groupProxy.Received(1).SendCoreAsync(
            RaceHubEvents.PlayerLeft,
            Arg.Is<object?[]>(args => args.Length == 1 && args[0] is PlayerLeftDto && ((PlayerLeftDto)args[0]!).Lobby.Id == first.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinLobby_WhenTargetFails_DoesNotLeaveCurrentLobby()
    {
        using var hostHub = CreateHub("host-1");
        var first = await hostHub.CreateLobby("Alice");
        using var guestHub = CreateHub("guest-1");
        await guestHub.JoinLobby(first.Id, "Bob");

        await Assert.ThrowsAsync<HubException>(() => guestHub.JoinLobby("NOPE00", "Bob"));

        await _groups.DidNotReceive().RemoveFromGroupAsync("guest-1", first.Id, Arg.Any<CancellationToken>());
        Assert.Equal(2, _lobbyManager.GetLobby(first.Id)!.Players.Count);
    }

    private RaceHub CreateHub(string connectionId)
    {
        var context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns(connectionId);
        context.ConnectionAborted.Returns(CancellationToken.None);

        return new RaceHub(_lobbyManager, _snippetProvider)
        {
            Context = context,
            Clients = _clients,
            Groups = _groups
        };
    }
}

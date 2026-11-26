namespace CodeRacer.Server.Hubs;

/// <summary>Names of the events <see cref="RaceHub"/> sends to clients.</summary>
public static class RaceHubEvents
{
    public const string LobbyUpdated = "LobbyUpdated";
    public const string PlayerLeft = "PlayerLeft";
    public const string LobbyClosed = "LobbyClosed";
    public const string GameStarted = "GameStarted";
    public const string PlayerProgressUpdated = "PlayerProgressUpdated";
    public const string GameFinished = "GameFinished";
}

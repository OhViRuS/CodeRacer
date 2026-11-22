namespace CodeRacer.Server.Interfaces;

/// <summary>Brings the database up to date at startup: applies migrations, then seeds the starter data.</summary>
public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

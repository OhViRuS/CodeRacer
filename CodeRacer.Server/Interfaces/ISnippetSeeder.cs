namespace CodeRacer.Server.Interfaces;

/// <summary>Fills an empty CodeSnippets table with the starter snippets that ship with the app.</summary>
public interface ISnippetSeeder
{
    /// <returns>The number of snippets inserted (0 when the table already had data).</returns>
    Task<int> SeedIfEmptyAsync(CancellationToken cancellationToken);
}

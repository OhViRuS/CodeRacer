namespace CodeRacer.Server.Interfaces;

/// <summary>Makes sure the starter snippets that ship with the app exist in the CodeSnippets table.</summary>
public interface ISnippetSeeder
{
    /// <returns>The number of snippets inserted (0 when every starter snippet was already there).</returns>
    Task<int> SeedAsync(CancellationToken cancellationToken);
}

using System.Text.Json;
using CodeRacer.Server.Data;
using CodeRacer.Server.Extensions;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeRacer.Server.Services;

/// <summary>
/// One-time seeding: when the CodeSnippets table is empty, the starter snippets in
/// <c>Data/snippets.json</c> are copied into it. After that the database is the only source of
/// snippets; the file is never read again or written to.
/// The path comes from <c>SnippetStorage:FilePath</c> (relative paths start at the content root).
/// </summary>
public class JsonSnippetSeeder(
    CodeRacerDbContext context,
    IConfiguration configuration,
    IHostEnvironment environment) : ISnippetSeeder
{
    private const string DefaultPath = "Data/snippets.json";

    public async Task<int> SeedIfEmptyAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await context.CodeSnippets.AnyAsync(cancellationToken))
        {
            return 0;
        }

        List<CodeSnippet> snippets = await ReadSnippetsAsync(ResolvePath(), cancellationToken);

        foreach (CodeSnippet snippet in snippets)
        {
            snippet.CodeText = snippet.CodeText.NormalizeSnippet();
        }

        context.CodeSnippets.AddRange(snippets);
        await context.SaveChangesAsync(cancellationToken);
        return snippets.Count;
    }

    private string ResolvePath()
    {
        string configured = configuration["SnippetStorage:FilePath"] ?? DefaultPath;
        return Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured);
    }

    private static async Task<List<CodeSnippet>> ReadSnippetsAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"The starter snippets file was not found at '{path}'. Make sure snippets.json is copied to the output directory.");
        }

        await using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

        List<CodeSnippet> snippets =
            await JsonSerializer.DeserializeAsync<List<CodeSnippet>>(stream, cancellationToken: cancellationToken)
            ?? [];

        if (snippets.Any(snippet => snippet.Id == Guid.Empty))
        {
            throw new InvalidOperationException($"'{path}' contains snippets without an Id.");
        }

        return snippets;
    }
}

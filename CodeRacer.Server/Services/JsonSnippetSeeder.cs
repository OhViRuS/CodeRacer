using System.Text.Json;
using CodeRacer.Server.Data;
using CodeRacer.Server.Extensions;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeRacer.Server.Services;

/// <summary>
/// Startup seeding: every starter snippet in <c>Data/snippets.json</c> that is not in the CodeSnippets
/// table yet is inserted. A snippet counts as already there when its Id or its (normalized) code is in the
/// table, so rows that exist for other reasons never block the rest from being seeded and nothing is
/// inserted twice. The database stays the only source of snippets; the file is never written to.
/// The path comes from <c>SnippetStorage:FilePath</c> (relative paths start at the content root).
/// </summary>
public class JsonSnippetSeeder(
    CodeRacerDbContext context,
    IConfiguration configuration,
    IHostEnvironment environment) : ISnippetSeeder
{
    private const string DefaultPath = "Data/snippets.json";

    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<CodeSnippet> starterSnippets = await ReadSnippetsAsync(ResolvePath(), cancellationToken);

        var existing = await context.CodeSnippets
            .AsNoTracking()
            .Select(snippet => new { snippet.Id, snippet.CodeText })
            .ToListAsync(cancellationToken);

        HashSet<Guid> knownIds = existing.Select(snippet => snippet.Id).ToHashSet();
        HashSet<string> knownCode = existing.Select(snippet => snippet.CodeText).ToHashSet();

        var missing = new List<CodeSnippet>();
        foreach (CodeSnippet snippet in starterSnippets)
        {
            snippet.CodeText = snippet.CodeText.NormalizeSnippet();

            if (knownIds.Contains(snippet.Id) || knownCode.Contains(snippet.CodeText))
            {
                continue;
            }

            knownIds.Add(snippet.Id);
            knownCode.Add(snippet.CodeText);
            missing.Add(snippet);
        }

        if (missing.Count == 0)
        {
            return 0;
        }

        context.CodeSnippets.AddRange(missing);
        await context.SaveChangesAsync(cancellationToken);
        return missing.Count;
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

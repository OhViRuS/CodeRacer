using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using CodeRacer.Server.Models;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Extensions;

namespace CodeRacer.Server.Services;

public class FileSnippetProvider : ICodeSnippetProvider
{
    private readonly object _loadLock = new();
    private List<CodeSnippet>? _cachedSnippets;

    public List<CodeSnippet> GetSnippets(string filePath = "Data/snippets.json")
    {
        if (_cachedSnippets != null)
        {
            return new List<CodeSnippet>(_cachedSnippets);
        }

        lock (_loadLock)
        {
            if (_cachedSnippets != null)
            {
                return new List<CodeSnippet>(_cachedSnippets);
            }

            if (!File.Exists(filePath))
            {
                _cachedSnippets = new List<CodeSnippet>();
                return new List<CodeSnippet>(_cachedSnippets);
            }

            string jsonText;

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(fileStream))
            {
                jsonText = reader.ReadToEnd();
            }

            var snippets = JsonSerializer.Deserialize<List<CodeSnippet>>(jsonText) ?? new List<CodeSnippet>();

            var missing = snippets.Where(s => s.Id == Guid.Empty).ToList();
            if (missing.Any())
            {
                throw new InvalidOperationException($"Data/snippets.json contains {missing.Count} snippet(s) without an Id. Add explicit Ids to the JSON or enable a migration.");
            }

            foreach (var snippet in snippets)
            {
                snippet.CodeText = snippet.CodeText.NormalizeSnippet();
            }

            _cachedSnippets = snippets;

            return new List<CodeSnippet>(_cachedSnippets);
        }
    }
}
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

            foreach (var snippet in snippets)
            {
                if (snippet.Id == Guid.Empty)
                {
                    snippet.Id = Guid.NewGuid();
                }

                snippet.CodeText = snippet.CodeText.NormalizeSnippet();
            }

            _cachedSnippets = snippets;

            return new List<CodeSnippet>(_cachedSnippets);
        }
    }
}
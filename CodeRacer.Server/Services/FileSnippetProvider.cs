using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using CodeRacer.Server.Models;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Extensions;
using CodeRacer.Server.Exceptions;
using System.Linq;

namespace CodeRacer.Server.Services;

public class FileSnippetProvider : ICodeSnippetProvider
{
    private readonly object _loadLock = new();
    private List<CodeSnippet>? _cachedSnippets;
    private readonly object _modifyLock = new();

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
                throw new FileNotFoundException($"The snippets data file was not found at '{filePath}'. Please ensure snippets.json exists and is copied to the output directory.");
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

    public void AddSnippet(CodeSnippet snippet, bool validateDuplicate = true)
    {
        if (snippet == null) throw new ArgumentNullException(nameof(snippet));

        lock (_modifyLock)
        {
            var current = GetSnippets();

            snippet.CodeText = snippet.CodeText.NormalizeSnippet();

            if (validateDuplicate && current.Any(s => s.CodeText == snippet.CodeText))
            {
                throw new DuplicateSnippetException();
            }

            if (snippet.Id == Guid.Empty)
            {
                snippet.Id = Guid.NewGuid();
            }

            _cachedSnippets!.Add(snippet);
        }
    }

    public bool UpdateSnippet(Guid id, CodeSnippet updatedSnippet)
    {
        if (updatedSnippet == null) throw new ArgumentNullException(nameof(updatedSnippet));

        lock (_modifyLock)
        {
            GetSnippets();
            var existing = _cachedSnippets!.FirstOrDefault(s => s.Id == id);
            if (existing == null) return false;

            existing.CodeText = updatedSnippet.CodeText.NormalizeSnippet();
            existing.Language = updatedSnippet.Language;
            existing.Difficulty = updatedSnippet.Difficulty;
            existing.ProgrammingConcept = updatedSnippet.ProgrammingConcept;

            return true;
        }
    }

    public bool DeleteSnippet(Guid id)
    {
        lock (_modifyLock)
        {
            GetSnippets();
            var existing = _cachedSnippets!.FirstOrDefault(s => s.Id == id);
            if (existing == null) return false;

            return _cachedSnippets!.Remove(existing);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CodeRacer.Server.Exceptions;
using CodeRacer.Server.Extensions;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using Microsoft.Extensions.Configuration;

namespace CodeRacer.Server.Services;

public class FileSnippetProvider : ICodeSnippetRepository
{
    private readonly string _filePath;
    private readonly object _loadLock = new();
    private readonly object _modifyLock = new();
    private List<CodeSnippet>? _cachedSnippets;

    public FileSnippetProvider(IConfiguration? configuration = null)
    {
        _filePath = configuration?["SnippetStorage:FilePath"] ?? "Data/snippets.json";
    }

    public FileSnippetProvider(string filePath)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath) ? "Data/snippets.json" : filePath;
    }

    public IReadOnlyList<CodeSnippet> GetSnippets(ProgrammingLanguage? language = null)
    {
        List<CodeSnippet> allSnippets = EnsureLoaded();

        if (language is not null)
        {
            return allSnippets.Where(s => s.Language == language).ToList();
        }

        return allSnippets;
    }

    public CodeSnippet? GetById(Guid id)
    {
        List<CodeSnippet> allSnippets = EnsureLoaded();
        return allSnippets.FirstOrDefault(s => s.Id == id);
    }

    public CodeSnippet? GetRandom(ProgrammingLanguage? language = null, Difficulty? difficulty = null)
    {
        IEnumerable<CodeSnippet> filtered = EnsureLoaded();

        if (language is not null)
        {
            filtered = filtered.Where(s => s.Language == language);
        }

        if (difficulty is not null)
        {
            filtered = filtered.Where(s => s.Difficulty == difficulty);
        }

        var snippetList = filtered.ToList();
        if (snippetList.Count == 0)
        {
            return null;
        }

        int index = Random.Shared.Next(0, snippetList.Count);
        return snippetList[index];
    }

    public void AddSnippet(CodeSnippet snippet, bool validateDuplicate = true)
    {
        if (snippet == null)
        {
            throw new ArgumentNullException(nameof(snippet));
        }

        lock (_modifyLock)
        {
            EnsureLoaded();

            snippet.CodeText = snippet.CodeText.NormalizeSnippet();

            if (validateDuplicate && _cachedSnippets!.Any(s => s.CodeText == snippet.CodeText))
            {
                throw new DuplicateSnippetException();
            }

            if (snippet.Id == Guid.Empty)
            {
                snippet.Id = Guid.NewGuid();
            }

            _cachedSnippets!.Add(snippet);
            SaveSnippets();
        }
    }

    public bool UpdateSnippet(Guid id, CodeSnippet updatedSnippet)
    {
        if (updatedSnippet == null)
        {
            throw new ArgumentNullException(nameof(updatedSnippet));
        }

        lock (_modifyLock)
        {
            EnsureLoaded();
            var existing = _cachedSnippets!.FirstOrDefault(s => s.Id == id);
            if (existing == null)
            {
                return false;
            }

            existing.CodeText = updatedSnippet.CodeText.NormalizeSnippet();
            existing.Language = updatedSnippet.Language;
            existing.Difficulty = updatedSnippet.Difficulty;
            existing.ProgrammingConcept = updatedSnippet.ProgrammingConcept;

            SaveSnippets();
            return true;
        }
    }

    public bool DeleteSnippet(Guid id)
    {
        lock (_modifyLock)
        {
            EnsureLoaded();
            var existing = _cachedSnippets!.FirstOrDefault(s => s.Id == id);
            if (existing == null)
            {
                return false;
            }

            bool removed = _cachedSnippets!.Remove(existing);
            if (removed)
            {
                SaveSnippets();
            }

            return removed;
        }
    }

    private List<CodeSnippet> EnsureLoaded()
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

            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException($"The snippets data file was not found at '{_filePath}'. Please ensure snippets.json exists and is copied to the output directory.");
            }

            string jsonText;

            using (FileStream fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(fileStream))
            {
                jsonText = reader.ReadToEnd();
            }

            var snippets = JsonSerializer.Deserialize<List<CodeSnippet>>(jsonText) ?? new List<CodeSnippet>();

            var missing = snippets.Where(s => s.Id == Guid.Empty).ToList();
            if (missing.Any())
            {
                throw new InvalidOperationException($"Data file '{_filePath}' contains {missing.Count} snippet(s) without an Id. Add explicit Ids to the JSON or enable a migration.");
            }

            foreach (var snippet in snippets)
            {
                snippet.CodeText = snippet.CodeText.NormalizeSnippet();
            }

            _cachedSnippets = snippets;

            return new List<CodeSnippet>(_cachedSnippets);
        }
    }

    private void SaveSnippets()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string jsonText = JsonSerializer.Serialize(_cachedSnippets, options);

        using (FileStream fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write))
        using (StreamWriter writer = new StreamWriter(fileStream))
        {
            writer.Write(jsonText);
        }
    }
}

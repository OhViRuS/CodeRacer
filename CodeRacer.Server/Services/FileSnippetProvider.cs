using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CodeRacer.Server.Exceptions;
using CodeRacer.Server.Extensions;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace CodeRacer.Server.Services;

/// <summary>
/// Provides code snippets loaded from and persisted to a JSON file.
/// Relative paths are resolved against the content root path so mutations persist to the expected location.
/// Writes use an atomic temporary-file swap, and disk saves occur before updating the memory cache.
/// </summary>
public class FileSnippetProvider : ICodeSnippetRepository
{
    private readonly string _filePath;
    private readonly object _syncLock = new();
    private List<CodeSnippet>? _cachedSnippets;

    public FileSnippetProvider(IConfiguration? configuration = null, IWebHostEnvironment? environment = null)
    {
        string configuredPath = configuration?["SnippetStorage:FilePath"] ?? "Data/snippets.json";

        if (Path.IsPathRooted(configuredPath))
        {
            _filePath = configuredPath;
        }
        else
        {
            string basePath = environment?.ContentRootPath ?? AppContext.BaseDirectory;
            _filePath = Path.Combine(basePath, configuredPath);
        }
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

        lock (_syncLock)
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

            var updatedList = new List<CodeSnippet>(_cachedSnippets!)
            {
                snippet
            };

            SaveSnippets(updatedList);
            _cachedSnippets = updatedList;
        }
    }

    public bool UpdateSnippet(Guid id, CodeSnippet updatedSnippet)
    {
        if (updatedSnippet == null)
        {
            throw new ArgumentNullException(nameof(updatedSnippet));
        }

        lock (_syncLock)
        {
            EnsureLoaded();
            var existingIndex = _cachedSnippets!.FindIndex(s => s.Id == id);
            if (existingIndex < 0)
            {
                return false;
            }

            var updated = new CodeSnippet
            {
                Id = id,
                CodeText = updatedSnippet.CodeText.NormalizeSnippet(),
                Language = updatedSnippet.Language,
                Difficulty = updatedSnippet.Difficulty,
                ProgrammingConcept = updatedSnippet.ProgrammingConcept
            };

            var updatedList = new List<CodeSnippet>(_cachedSnippets!);
            updatedList[existingIndex] = updated;

            SaveSnippets(updatedList);
            _cachedSnippets = updatedList;
            return true;
        }
    }

    public bool DeleteSnippet(Guid id)
    {
        lock (_syncLock)
        {
            EnsureLoaded();
            var existing = _cachedSnippets!.FirstOrDefault(s => s.Id == id);
            if (existing == null)
            {
                return false;
            }

            var updatedList = new List<CodeSnippet>(_cachedSnippets!);
            updatedList.Remove(existing);

            SaveSnippets(updatedList);
            _cachedSnippets = updatedList;
            return true;
        }
    }

    private List<CodeSnippet> EnsureLoaded()
    {
        lock (_syncLock)
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

    private void SaveSnippets(List<CodeSnippet> snippetsToSave)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string jsonText = JsonSerializer.Serialize(snippetsToSave, options);

        string directory = Path.GetDirectoryName(_filePath) ?? string.Empty;
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string tempFilePath = Path.Combine(
            string.IsNullOrEmpty(directory) ? "." : directory,
            $"{Path.GetFileName(_filePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (FileStream fileStream = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (StreamWriter writer = new StreamWriter(fileStream))
            {
                writer.Write(jsonText);
            }

            File.Move(tempFilePath, _filePath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch
                {
                    // Best-effort cleanup of temporary file on failure
                }
            }

            throw;
        }
    }
}

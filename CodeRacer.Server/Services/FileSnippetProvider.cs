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
    public List<CodeSnippet> GetSnippets(string filePath = "Data/snippets.json")
    {
        if (!File.Exists(filePath))
        {
            return new List<CodeSnippet>();
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
            snippet.Id = Guid.NewGuid();
            snippet.CodeText = snippet.CodeText.NormalizeSnippet();
        }

        return snippets;
    }
}
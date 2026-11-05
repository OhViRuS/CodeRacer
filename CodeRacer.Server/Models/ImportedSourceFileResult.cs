using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CodeRacer.Server.Models;

/// <summary>
/// Represents an imported source file together with extracted typing challenge snippets.
/// </summary>
public class ImportedSourceFileResult
{
    public string FileName { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProgrammingLanguage Language { get; set; }

    public string Content { get; set; } = string.Empty;

    public IReadOnlyList<ExtractedSourceSnippet> Snippets { get; set; }
        = Array.Empty<ExtractedSourceSnippet>();
}

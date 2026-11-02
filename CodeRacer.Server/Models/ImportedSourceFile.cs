using System.Text.Json.Serialization;

namespace CodeRacer.Server.Models;

/// <summary>
/// The validated result of importing a source code file. Immutable.
/// </summary>
public sealed record ImportedSourceFile(
    string FileName,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ProgrammingLanguage Language,
    string Content);

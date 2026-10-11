using System.ComponentModel.DataAnnotations;

namespace CodeRacer.Server.Models;

public class CodeSnippet
{
    public Guid Id { get; set; }

    [Required]
    public string CodeText { get; set; } = string.Empty;

    public ProgrammingLanguage Language { get; set; }

    public Difficulty Difficulty { get; set; }

    [Required]
    public string ProgrammingConcept { get; set; } = string.Empty;
}

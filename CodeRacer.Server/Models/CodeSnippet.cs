using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CodeRacer.Server.Models;

public class CodeSnippet : IValidatableObject
{
    /// <summary>
    /// Longest accepted code text. Keeps the unique index on CodeText below PostgreSQL's index entry
    /// size limit (about 2.7 KB). Checked here instead of with [StringLength] on purpose: that attribute
    /// would also change the column type and need a migration.
    /// </summary>
    public const int MaxCodeTextLength = 2000;

    /// <summary>Same limit as the database column (see CodeRacerDbContext).</summary>
    public const int MaxProgrammingConceptLength = 100;

    public Guid Id { get; set; }

    // [Required] rejects null, empty and whitespace-only text (AllowEmptyStrings is false by default),
    // so a blank snippet can never normalize to "" and be stored.
    [Required]
    public string CodeText { get; set; } = string.Empty;

    public ProgrammingLanguage Language { get; set; }

    public Difficulty Difficulty { get; set; }

    [Required]
    [StringLength(MaxProgrammingConceptLength)]
    public string ProgrammingConcept { get; set; } = string.Empty;

    [JsonIgnore]
    public ICollection<RaceResult> RaceResults { get; set; } = new List<RaceResult>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CodeText.Length > MaxCodeTextLength)
        {
            yield return new ValidationResult(
                $"The code text must be at most {MaxCodeTextLength} characters.",
                [nameof(CodeText)]);
        }
    }
}

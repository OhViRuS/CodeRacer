using Microsoft.AspNetCore.Mvc;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CodeSnippetsController : ControllerBase
{
    // Temporary in-memory storage until database integration is added.
    private static readonly List<CodeSnippet> Snippets = new()
    {
        new CodeSnippet
        {
            Id = Guid.NewGuid(),
            CodeText = "Console.WriteLine(\"Hello World\");",
            Language = ProgrammingLanguage.CSharp,
            Difficulty = Difficulty.Easy,
            ProgrammingConcept = "Output"
        },

        new CodeSnippet
        {
            Id = Guid.NewGuid(),
            CodeText = "console.log('Hello World');",
            Language = ProgrammingLanguage.JavaScript,
            Difficulty = Difficulty.Hard,
            ProgrammingConcept = "Output"
        },

        new CodeSnippet
        {
            Id = Guid.NewGuid(),
            CodeText = "print(\"Hello World\")",
            Language = ProgrammingLanguage.Python,
            Difficulty = Difficulty.Easy,
            ProgrammingConcept = "Output"
        },

        new CodeSnippet
        {
            Id = Guid.NewGuid(),
            CodeText = "std::cout << \"Hello World\" << std::endl;",
            Language = ProgrammingLanguage.Cpp,
            Difficulty = Difficulty.Easy,
            ProgrammingConcept = "Output"
        }
    };

    // GET: /api/codesnippets
    // GET: /api/codesnippets?language=Python
    [HttpGet]
    public ActionResult<IEnumerable<CodeSnippet>> GetAll([FromQuery] ProgrammingLanguage? language)
    {
        IEnumerable<CodeSnippet> result = language is null
            ? Snippets
            : Snippets.Where(s => s.Language == language);

        return Ok(result);
    }

    // GET: /api/codesnippets/languages
    [HttpGet("languages")]
    public ActionResult<IEnumerable<LanguageOption>> GetLanguages()
    {
        var languages = Enum.GetValues<ProgrammingLanguage>()
            .Select(l => new LanguageOption(l.ToString(), l.ToDisplayName()));

        return Ok(languages);
    }

    // GET: /api/codesnippets/{id}
    [HttpGet("{id:guid}")]
    public ActionResult<CodeSnippet> GetById(Guid id)
    {
        var snippet = Snippets.FirstOrDefault(s => s.Id == id);

        if (snippet == null)
        {
            return NotFound();
        }

        return Ok(snippet);
    }

    // POST: /api/codesnippets
    [HttpPost]
    public ActionResult<CodeSnippet> Create(CodeSnippet snippet)
    {
        snippet.Id = Guid.NewGuid();

        Snippets.Add(snippet);

        return CreatedAtAction(
            nameof(GetById),
            new { id = snippet.Id },
            snippet
        );
    }

    // PUT: /api/codesnippets/{id}
    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, CodeSnippet updatedSnippet)
    {
        if (id != updatedSnippet.Id)
        {
            return BadRequest("Route id does not match snippet id.");
        }

        var snippet = Snippets.FirstOrDefault(s => s.Id == id);

        if (snippet == null)
        {
            return NotFound();
        }

        snippet.CodeText = updatedSnippet.CodeText;
        snippet.Language = updatedSnippet.Language;
        snippet.Difficulty = updatedSnippet.Difficulty;
        snippet.ProgrammingConcept = updatedSnippet.ProgrammingConcept;

        return NoContent();
    }

    // DELETE: /api/codesnippets/{id}
    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var snippet = Snippets.FirstOrDefault(s => s.Id == id);

        if (snippet == null)
        {
            return NotFound();
        }

        Snippets.Remove(snippet);

        return NoContent();
    }
}

public record LanguageOption(string Name, string DisplayName);

public static class ProgrammingLanguageExtensions
{
    public static string ToDisplayName(this ProgrammingLanguage language) => language switch
    {
        ProgrammingLanguage.CSharp => "C#",
        ProgrammingLanguage.Cpp => "C++",
        ProgrammingLanguage.Python => "Python",
        ProgrammingLanguage.JavaScript => "JavaScript",
        _ => language.ToString()
    };
}
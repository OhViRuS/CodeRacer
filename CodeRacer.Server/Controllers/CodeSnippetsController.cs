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
        }
    };

    // GET: /api/codesnippets
    [HttpGet]
    public ActionResult<IEnumerable<CodeSnippet>> GetAll()
    {
        return Ok(Snippets);
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
using CodeRacer.Server.Core;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace CodeRacer.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RaceSessionsController : ControllerBase
{
    [HttpPost("result")]
    public ActionResult GetResult(
        RaceResultRequest request,
        [FromServices] ICodeSnippetProvider snippetProvider)
    {
        var snippet = snippetProvider
            .GetSnippets()
            .FirstOrDefault(s => s.Id == request.SnippetId);

        if (snippet == null)
        {
            return NotFound("Code snippet not found.");
        }

        if (request.FinalText != snippet.CodeText)
        {
            return BadRequest("Race is not completed.");
        }

        if (request.ElapsedSeconds < 0)
        {
            return BadRequest("Elapsed time cannot be negative.");
        }

        var session = new RaceSession(snippet);

        foreach (var attempt in request.Keystrokes)
        {
            session.TypeCharacter(
                attempt.Character,
                attempt.Position);
        }

        if (!session.IsCompleted)
        {
            return BadRequest("Keystrokes do not match the completed code snippet.");
        }

        var result = session.GetResult(
            TimeSpan.FromSeconds(request.ElapsedSeconds));

        return Ok(new
        {
            time = result.CompletionTime.TotalSeconds,
            wpm = result.TypingSpeed,
            accuracy = result.Accuracy,
            mistakes = session.TotalErrorsCount
        });
    }
}

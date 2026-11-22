using System;
using CodeRacer.Server.Exceptions;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace CodeRacer.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CodeSnippetsController : ControllerBase
{
    private readonly ICodeSnippetRepository _snippetRepository;

    public CodeSnippetsController(ICodeSnippetRepository snippetRepository)
    {
        _snippetRepository = snippetRepository ?? throw new ArgumentNullException(nameof(snippetRepository));
    }

    // GET: /api/codesnippets
    // GET: /api/codesnippets?language=Python
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CodeSnippet>>> GetAll(
        [FromQuery] ProgrammingLanguage? language = null,
        CancellationToken cancellationToken = default)
    {
        var snippets = await _snippetRepository.GetSnippetsAsync(language, cancellationToken);
        return Ok(snippets);
    }

    // GET: /api/codesnippets/languages
    [HttpGet("languages")]
    public ActionResult<IEnumerable<string>> GetLanguages()
    {
        return Ok(Enum.GetNames<ProgrammingLanguage>());
    }

    // GET: /api/codesnippets/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CodeSnippet>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var snippet = await _snippetRepository.GetByIdAsync(id, cancellationToken);
        if (snippet == null)
        {
            return NotFound();
        }

        return Ok(snippet);
    }

    // GET: /api/codesnippets/random?language=Python&difficulty=Easy
    [HttpGet("random")]
    public async Task<ActionResult<CodeSnippet>> GetRandom(
        [FromQuery] ProgrammingLanguage? language = null,
        [FromQuery] Difficulty? difficulty = null,
        CancellationToken cancellationToken = default)
    {
        var randomSnippet = await _snippetRepository.GetRandomAsync(language, difficulty, cancellationToken);
        if (randomSnippet == null)
        {
            return NotFound();
        }

        return Ok(randomSnippet);
    }

    // POST: /api/codesnippets
    [HttpPost]
    public async Task<ActionResult<CodeSnippet>> Create(
        [FromBody] CodeSnippet snippet,
        CancellationToken cancellationToken)
    {
        if (snippet == null)
        {
            return BadRequest();
        }

        snippet.Id = snippet.Id == Guid.Empty ? Guid.NewGuid() : snippet.Id;
        try
        {
            await _snippetRepository.AddSnippetAsync(snippet, validateDuplicate: true, cancellationToken);
        }
        catch (DuplicateSnippetException ex)
        {
            return Conflict(new { message = ex.Message });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = snippet.Id },
            snippet
        );
    }

    // PUT: /api/codesnippets/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] CodeSnippet updatedSnippet,
        CancellationToken cancellationToken)
    {
        if (updatedSnippet == null)
        {
            return BadRequest();
        }

        if (id != updatedSnippet.Id)
        {
            return BadRequest("Route id does not match snippet id.");
        }

        try
        {
            var updated = await _snippetRepository.UpdateSnippetAsync(id, updatedSnippet, cancellationToken);
            return updated ? NoContent() : NotFound();
        }
        catch (DuplicateSnippetException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // DELETE: /api/codesnippets/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var removed = await _snippetRepository.DeleteSnippetAsync(id, cancellationToken);
            return removed ? NoContent() : NotFound();
        }
        catch (SnippetInUseException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

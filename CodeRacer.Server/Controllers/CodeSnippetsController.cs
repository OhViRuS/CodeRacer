using System;
using System.Collections.Generic;
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
    public ActionResult<IEnumerable<CodeSnippet>> GetAll([FromQuery] ProgrammingLanguage? language = null)
    {
        var snippets = _snippetRepository.GetSnippets(language: language);
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
    public ActionResult<CodeSnippet> GetById(Guid id)
    {
        var snippet = _snippetRepository.GetById(id);
        if (snippet == null)
        {
            return NotFound();
        }

        return Ok(snippet);
    }

    // GET: /api/codesnippets/random?language=Python&difficulty=Easy
    [HttpGet("random")]
    public ActionResult<CodeSnippet> GetRandom(
        [FromQuery] ProgrammingLanguage? language = null,
        [FromQuery] Difficulty? difficulty = null)
    {
        var randomSnippet = _snippetRepository.GetRandom(language: language, difficulty: difficulty);
        if (randomSnippet == null)
        {
            return NotFound();
        }

        return Ok(randomSnippet);
    }

    // POST: /api/codesnippets
    [HttpPost]
    public ActionResult<CodeSnippet> Create([FromBody] CodeSnippet snippet)
    {
        if (snippet == null)
        {
            return BadRequest();
        }

        snippet.Id = snippet.Id == Guid.Empty ? Guid.NewGuid() : snippet.Id;
        try
        {
            _snippetRepository.AddSnippet(validateDuplicate: true, snippet: snippet);
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
    public IActionResult Update(Guid id, [FromBody] CodeSnippet updatedSnippet)
    {
        if (updatedSnippet == null)
        {
            return BadRequest();
        }

        if (id != updatedSnippet.Id)
        {
            return BadRequest("Route id does not match snippet id.");
        }

        var updated = _snippetRepository.UpdateSnippet(id, updatedSnippet);
        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    // DELETE: /api/codesnippets/{id}
    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var removed = _snippetRepository.DeleteSnippet(id);
        if (!removed)
        {
            return NotFound();
        }

        return NoContent();
    }
}

using Microsoft.AspNetCore.Mvc;
using CodeRacer.Server.Models;
using CodeRacer.Server.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CodeRacer.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CodeSnippetsController : ControllerBase
{
    private readonly ICodeSnippetProvider _snippetProvider;
    private static List<CodeSnippet> _snippets = new();

    public CodeSnippetsController(ICodeSnippetProvider snippetProvider)
    {
        _snippetProvider = snippetProvider;

        _snippets = _snippetProvider.GetSnippets();
    }

    // GET: /api/codesnippets
    [HttpGet]
    public ActionResult<IEnumerable<CodeSnippet>> GetAll()
    {
        return Ok(_snippets);
    }

    // GET: /api/codesnippets/{id}
    [HttpGet("{id:guid}")]
    public ActionResult<CodeSnippet> GetById(Guid id)
    {
        var snippet = _snippets.FirstOrDefault(s => s.Id == id);

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

        _snippets.Add(snippet);

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

        var snippet = _snippets.FirstOrDefault(s => s.Id == id);

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
        var snippet = _snippets.FirstOrDefault(s => s.Id == id);

        if (snippet == null)
        {
            return NotFound();
        }

        _snippets.Remove(snippet);

        return NoContent();
    }
}
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

    // GET: /api/codesnippets
    [HttpGet]
    public ActionResult<IEnumerable<CodeSnippet>> GetAll([FromServices] ICodeSnippetProvider snippetProvider)
    {
        var snippets = snippetProvider.GetSnippets();
        return Ok(snippets);
    }

    // GET: /api/codesnippets/{id}
    [HttpGet("{id:guid}")]
    public ActionResult<CodeSnippet> GetById(Guid id, [FromServices] ICodeSnippetProvider snippetProvider)
    {
        var snippet = snippetProvider.GetSnippets().FirstOrDefault(s => s.Id == id);
        if (snippet == null) return NotFound();
        return Ok(snippet);
    }

    // POST: /api/codesnippets
    [HttpPost]
    public ActionResult<CodeSnippet> Create([FromBody] CodeSnippet snippet, [FromServices] ICodeSnippetProvider snippetProvider)
    {
        if (snippet == null) return BadRequest();

        snippet.Id = snippet.Id == Guid.Empty ? Guid.NewGuid() : snippet.Id;
        snippetProvider.AddSnippet(snippet);

        return CreatedAtAction(
            nameof(GetById),
            new { id = snippet.Id },
            snippet
        );
    }

    // PUT: /api/codesnippets/{id}
    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] CodeSnippet updatedSnippet, [FromServices] ICodeSnippetProvider snippetProvider)
    {
        if (updatedSnippet == null) return BadRequest();
        if (id != updatedSnippet.Id) return BadRequest("Route id does not match snippet id.");

        var updated = snippetProvider.UpdateSnippet(id, updatedSnippet);
        if (!updated) return NotFound();
        return NoContent();
    }

    // DELETE: /api/codesnippets/{id}
    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id, [FromServices] ICodeSnippetProvider snippetProvider)
    {
        var removed = snippetProvider.DeleteSnippet(id);
        if (!removed) return NotFound();
        return NoContent();
    }
}
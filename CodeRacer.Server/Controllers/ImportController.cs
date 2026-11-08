using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeRacer.Server.Exceptions;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using CodeRacer.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CodeRacer.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    // A little above the file limit to leave room for multipart boundaries and headers.
    private const long MaxRequestSizeBytes = SourceFileImporter.MaxFileSizeBytes + 64 * 1024;

    private readonly ISourceFileImporter _importer;
    private readonly ISourceSnippetExtractor _snippetExtractor;

    public ImportController(
        ISourceFileImporter importer,
        ISourceSnippetExtractor snippetExtractor)
    {
        _importer = importer ?? throw new ArgumentNullException(nameof(importer));
        _snippetExtractor = snippetExtractor ?? throw new ArgumentNullException(nameof(snippetExtractor));
    }

    // GET: /api/import/rules   (supported extensions and maximum file size)
    [HttpGet("rules")]
    public ActionResult<ImportRules> GetRules()
    {
        return Ok(_importer.Rules);
    }

    // POST: /api/import   (multipart/form-data with a single field named "file")
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestSizeBytes)]
    public async Task<ActionResult<ImportedSourceFileResult>> Import(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file == null)
        {
            return BadRequest(new { message = "No file was provided." });
        }

        try
        {
            await using Stream stream = file.OpenReadStream();
            ImportedSourceFile imported = await _importer.ImportAsync(file.FileName, stream, cancellationToken);
            IReadOnlyList<ExtractedSourceSnippet> snippets =
                _snippetExtractor.Extract(imported.Language, imported.Content);

            var result = new ImportedSourceFileResult
            {
                FileName = imported.FileName,
                Language = imported.Language,
                Content = imported.Content,
                Snippets = snippets
            };

            return Ok(result);
        }
        catch (ImportedFileException ex)
        {
            return StatusCode(GetStatusCode(ex.Reason), new { message = ex.Message });
        }
    }

    private static int GetStatusCode(ImportFailureReason reason)
    {
        return reason switch
        {
            ImportFailureReason.FileTooLarge => StatusCodes.Status413PayloadTooLarge,
            ImportFailureReason.UnsupportedExtension => StatusCodes.Status415UnsupportedMediaType,
            _ => StatusCodes.Status400BadRequest
        };
    }
}

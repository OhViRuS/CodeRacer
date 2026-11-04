using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Interfaces;

public interface ISourceFileImporter
{
    /// <summary>
    /// The supported extensions and the maximum file size. Single source of truth for all clients.
    /// </summary>
    ImportRules Rules { get; }

    /// <summary>
    /// Validates and reads an uploaded source code file.
    /// Throws <see cref="Exceptions.ImportedFileException"/> when the file is rejected.
    /// </summary>
    Task<ImportedSourceFile> ImportAsync(string? fileName, Stream content, CancellationToken cancellationToken = default);
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeRacer.Server.Exceptions;
using CodeRacer.Server.Extensions;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Services;

/// <summary>
/// Accepts source code files with a supported extension, up to the maximum size, both listed in <see cref="Rules"/>.
/// The size limit is enforced while reading, so it does not rely on a client-declared length.
/// Content must be valid UTF-8 text.
/// </summary>
public class SourceFileImporter : ISourceFileImporter
{
    public const long MaxFileSizeBytes = 5 * BytesPerMegabyte;

    private const long BytesPerMegabyte = 1024 * 1024;
    private const int BufferSize = 81920;

    // Both separators are handled explicitly: on Linux Path.GetFileName does not treat '\' as a separator.
    private static readonly char[] _pathSeparators = ['/', '\\'];

    private static readonly Dictionary<string, ProgrammingLanguage> _supportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".cs"] = ProgrammingLanguage.CSharp,
            [".cpp"] = ProgrammingLanguage.Cpp,
            [".py"] = ProgrammingLanguage.Python,
            [".js"] = ProgrammingLanguage.JavaScript
        };

    private static readonly UTF8Encoding _strictUtf8 =
        new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public ImportRules Rules { get; } = new(_supportedExtensions.Keys.ToArray(), MaxFileSizeBytes);

    public async Task<ImportedSourceFile> ImportAsync(
        string? fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ImportedFileException(ImportFailureReason.NoFile, "No file was provided.");
        }

        // Only the file name is kept; any client-supplied path (Windows or Unix style) is discarded.
        string safeName = GetSafeFileName(fileName);

        if (safeName.Length == 0)
        {
            throw new ImportedFileException(ImportFailureReason.NoFile, "No file was provided.");
        }

        string extension = Path.GetExtension(safeName);

        if (!_supportedExtensions.TryGetValue(extension, out ProgrammingLanguage language))
        {
            string shown = extension.Length == 0 ? "(none)" : extension;
            string supported = string.Join(", ", _supportedExtensions.Keys);
            throw new ImportedFileException(
                ImportFailureReason.UnsupportedExtension,
                $"Unsupported file type '{shown}'. Supported extensions: {supported}.");
        }

        byte[] bytes = await ReadWithLimitAsync(content, cancellationToken);
        string code = DecodeText(bytes).NormalizeSnippet();

        if (code.Length == 0)
        {
            throw new ImportedFileException(ImportFailureReason.EmptyFile, "The file is empty.");
        }

        return new ImportedSourceFile(safeName, language, code);
    }

    private static string GetSafeFileName(string fileName)
    {
        int lastSeparator = fileName.LastIndexOfAny(_pathSeparators);

        return lastSeparator >= 0 ? fileName[(lastSeparator + 1)..] : fileName;
    }

    private static async Task<byte[]> ReadWithLimitAsync(Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        byte[] chunk = new byte[BufferSize];

        while (true)
        {
            int read = await content.ReadAsync(chunk.AsMemory(), cancellationToken);
            if (read == 0)
            {
                break;
            }

            if (buffer.Length + read > MaxFileSizeBytes)
            {
                throw new ImportedFileException(
                    ImportFailureReason.FileTooLarge,
                    $"The file exceeds the maximum size of {MaxFileSizeBytes / BytesPerMegabyte} MB.");
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private static string DecodeText(byte[] bytes)
    {
        string text;

        try
        {
            text = _strictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            throw new ImportedFileException(ImportFailureReason.NotTextFile, "The file is not valid UTF-8 text.");
        }

        if (text.Contains('\0'))
        {
            throw new ImportedFileException(ImportFailureReason.NotTextFile, "The file looks like a binary file, not source code.");
        }

        return text.TrimStart('\uFEFF');
    }
}

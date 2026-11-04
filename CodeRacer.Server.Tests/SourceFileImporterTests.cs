using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CodeRacer.Server.Exceptions;
using CodeRacer.Server.Models;
using CodeRacer.Server.Services;

namespace CodeRacer.Server.Tests;

public class SourceFileImporterTests
{
    private readonly SourceFileImporter _importer = new();

    [Theory]
    [InlineData("Program.cs", ProgrammingLanguage.CSharp)]
    [InlineData("main.cpp", ProgrammingLanguage.Cpp)]
    [InlineData("script.py", ProgrammingLanguage.Python)]
    [InlineData("app.js", ProgrammingLanguage.JavaScript)]
    [InlineData("UPPERCASE.PY", ProgrammingLanguage.Python)]
    public async Task ImportAsync_SupportedExtension_DetectsLanguage(string fileName, ProgrammingLanguage expected)
    {
        ImportedSourceFile result = await _importer.ImportAsync(fileName, ToStream("code"));

        Assert.Equal(expected, result.Language);
        Assert.Equal(fileName, result.FileName);
        Assert.Equal("code", result.Content);
    }

    [Theory]
    [InlineData("notes.txt")]
    [InlineData("Hello.java")]
    [InlineData("archive.cs.zip")]
    [InlineData("noextension")]
    public async Task ImportAsync_UnsupportedExtension_Throws(string fileName)
    {
        var ex = await Assert.ThrowsAsync<ImportedFileException>(
            () => _importer.ImportAsync(fileName, ToStream("code")));

        Assert.Equal(ImportFailureReason.UnsupportedExtension, ex.Reason);
    }

    [Fact]
    public async Task ImportAsync_MissingFileName_Throws()
    {
        var ex = await Assert.ThrowsAsync<ImportedFileException>(
            () => _importer.ImportAsync(null, ToStream("code")));

        Assert.Equal(ImportFailureReason.NoFile, ex.Reason);
    }

    [Fact]
    public void Rules_ExposeSupportedExtensionsAndMaxSize()
    {
        ImportRules rules = _importer.Rules;

        Assert.Equal(SourceFileImporter.MaxFileSizeBytes, rules.MaxFileSizeBytes);
        Assert.Equal(4, rules.SupportedExtensions.Count);
        Assert.Contains(".cs", rules.SupportedExtensions);
        Assert.Contains(".cpp", rules.SupportedExtensions);
        Assert.Contains(".py", rules.SupportedExtensions);
        Assert.Contains(".js", rules.SupportedExtensions);
    }

    [Fact]
    public async Task ImportAsync_FileExactlyAtLimit_IsAccepted()
    {
        using var stream = new MemoryStream(Filled((int)SourceFileImporter.MaxFileSizeBytes));

        ImportedSourceFile result = await _importer.ImportAsync("big.py", stream);

        Assert.Equal(SourceFileImporter.MaxFileSizeBytes, result.Content.Length);
    }

    [Fact]
    public async Task ImportAsync_FileOverLimit_Throws()
    {
        using var stream = new MemoryStream(Filled((int)SourceFileImporter.MaxFileSizeBytes + 1));

        var ex = await Assert.ThrowsAsync<ImportedFileException>(
            () => _importer.ImportAsync("big.py", stream));

        Assert.Equal(ImportFailureReason.FileTooLarge, ex.Reason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \r\n\t  ")]
    public async Task ImportAsync_EmptyOrWhitespaceFile_Throws(string text)
    {
        var ex = await Assert.ThrowsAsync<ImportedFileException>(
            () => _importer.ImportAsync("empty.cs", ToStream(text)));

        Assert.Equal(ImportFailureReason.EmptyFile, ex.Reason);
    }

    [Fact]
    public async Task ImportAsync_BinaryContent_Throws()
    {
        using var stream = new MemoryStream(new byte[] { 0x4D, 0x5A, 0x00, 0x01 });

        var ex = await Assert.ThrowsAsync<ImportedFileException>(
            () => _importer.ImportAsync("fake.cs", stream));

        Assert.Equal(ImportFailureReason.NotTextFile, ex.Reason);
    }

    [Fact]
    public async Task ImportAsync_InvalidUtf8_Throws()
    {
        using var stream = new MemoryStream(new byte[] { 0xC3, 0x28 });

        var ex = await Assert.ThrowsAsync<ImportedFileException>(
            () => _importer.ImportAsync("bad.js", stream));

        Assert.Equal(ImportFailureReason.NotTextFile, ex.Reason);
    }

    [Fact]
    public async Task ImportAsync_Content_StripsBomAndNormalizesLineEndings()
    {
        ImportedSourceFile result = await _importer.ImportAsync(
            "a.py",
            ToStream("\uFEFFif x:\r\n\tprint(x)\r\n"));

        Assert.Equal("if x:\n    print(x)", result.Content);
    }

    [Theory]
    [InlineData("../../secret/evil.py")]
    [InlineData("/etc/secret/evil.py")]
    [InlineData("C:\\fakepath\\evil.py")]
    [InlineData("..\\..\\secret\\evil.py")]
    [InlineData("folder/sub\\evil.py")]
    public async Task ImportAsync_ClientSuppliedPath_IsDiscarded(string fileName)
    {
        ImportedSourceFile result = await _importer.ImportAsync(fileName, ToStream("x = 1"));

        Assert.Equal("evil.py", result.FileName);
    }

    [Theory]
    [InlineData("folder/")]
    [InlineData("C:\\folder\\")]
    public async Task ImportAsync_NameEndingWithSeparator_Throws(string fileName)
    {
        var ex = await Assert.ThrowsAsync<ImportedFileException>(
            () => _importer.ImportAsync(fileName, ToStream("x = 1")));

        Assert.Equal(ImportFailureReason.NoFile, ex.Reason);
    }

    private static MemoryStream ToStream(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }

    private static byte[] Filled(int length)
    {
        byte[] bytes = new byte[length];
        Array.Fill(bytes, (byte)'a');
        return bytes;
    }
}

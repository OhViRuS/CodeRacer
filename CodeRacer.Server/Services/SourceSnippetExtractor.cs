using System;
using System.Collections.Generic;
using System.Linq;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Services;

public class SourceSnippetExtractor : ISourceSnippetExtractor
{
    /// <summary>
    /// Extracts suitable typing challenge snippets from source code
    /// based on the programming language.
    /// </summary>
    public IReadOnlyList<ExtractedSourceSnippet> Extract(
        ProgrammingLanguage language,
        string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        return language switch
        {
            ProgrammingLanguage.CSharp => ExtractBraceBasedSnippets(content),
            ProgrammingLanguage.Cpp => ExtractBraceBasedSnippets(content),
            ProgrammingLanguage.JavaScript => ExtractBraceBasedSnippets(content),
            ProgrammingLanguage.Python => ExtractPythonSnippets(content),
            _ => Array.Empty<ExtractedSourceSnippet>()
        };
    }

    private static IReadOnlyList<ExtractedSourceSnippet> ExtractBraceBasedSnippets(string content)
    {
        var snippets = new List<ExtractedSourceSnippet>();

        string[] lines = content
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (!TryFindMethodOpeningBrace(lines, i, out int openingBraceLine))
            {
                continue;
            }

            int braceDepth = 0;
            bool foundOpeningBrace = false;
            bool inBlockComment = false;
            int endLine = openingBraceLine;

            for (int j = openingBraceLine; j < lines.Length; j++)
            {
                int braceChange = CountStructuralBraces(
                    lines[j],
                    ref inBlockComment,
                    out bool hasOpeningBrace);

                braceDepth += braceChange;
                foundOpeningBrace |= hasOpeningBrace;

                if (foundOpeningBrace && braceDepth == 0)
                {
                    endLine = j;
                    break;
                }
            }

            if (!foundOpeningBrace || braceDepth != 0)
            {
                continue;
            }

            string code = string.Join(
                "\n",
                lines[i..(endLine + 1)]);

            snippets.Add(new ExtractedSourceSnippet
            {
                Code = code.Trim()
            });

            i = endLine;
        }

        return snippets;
    }

    private static int CountStructuralBraces(
        string line,
        ref bool inBlockComment,
        out bool hasOpeningBrace)
    {
        int braceChange = 0;
        bool inString = false;
        bool inCharacter = false;
        bool inTemplateString = false;
        bool escaped = false;

        hasOpeningBrace = false;

        for (int i = 0; i < line.Length; i++)
        {
            char character = line[i];

            if (inBlockComment)
            {
                if (character == '*' &&
                    i + 1 < line.Length &&
                    line[i + 1] == '/')
                {
                    inBlockComment = false;
                    i++;
                }

                continue;
            }

            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (inCharacter)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '\'')
                {
                    inCharacter = false;
                }

                continue;
            }

            if (inTemplateString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '`')
                {
                    inTemplateString = false;
                }

                continue;
            }

            if (character == '/' &&
                i + 1 < line.Length &&
                line[i + 1] == '/')
            {
                break;
            }

            if (character == '/' &&
                i + 1 < line.Length &&
                line[i + 1] == '*')
            {
                inBlockComment = true;
                i++;
                continue;
            }

            if (character == '"')
            {
                inString = true;
                continue;
            }

            if (character == '\'')
            {
                inCharacter = true;
                continue;
            }

            if (character == '`')
            {
                inTemplateString = true;
                continue;
            }

            if (character == '{')
            {
                braceChange++;
                hasOpeningBrace = true;
            }
            else if (character == '}')
            {
                braceChange--;
            }
        }

        return braceChange;
    }

    private static bool TryFindMethodOpeningBrace(
        string[] lines,
        int startLine,
        out int openingBraceLine)
    {
        openingBraceLine = -1;

        string firstLine = lines[startLine].Trim();

        if (!firstLine.Contains('('))
        {
            return false;
        }

        var signatureLines = new List<string>();

        for (int i = startLine; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            signatureLines.Add(line);

            if (!line.Contains(')'))
            {
                continue;
            }

            string signature = string.Join(" ", signatureLines);

            if (!LooksLikeMethodSignature(signature))
            {
                return false;
            }

            if (line.Contains('{'))
            {
                openingBraceLine = i;
                return true;
            }

            int nextLine = i + 1;

            while (nextLine < lines.Length &&
                   string.IsNullOrWhiteSpace(lines[nextLine]))
            {
                nextLine++;
            }

            if (nextLine < lines.Length &&
                lines[nextLine].TrimStart().StartsWith('{'))
            {
                openingBraceLine = nextLine;
                return true;
            }

            return false;
        }

        return false;
    }

    private static bool LooksLikeMethodSignature(string line)
    {
        if (!line.Contains('(') || !line.Contains(')'))
        {
            return false;
        }

        string[] excludedPrefixes =
        {
            "if",
            "for",
            "foreach",
            "while",
            "switch",
            "catch"
        };

        return !excludedPrefixes.Any(prefix =>
            line.StartsWith($"{prefix}(", StringComparison.Ordinal) ||
            line.StartsWith($"{prefix} (", StringComparison.Ordinal));
    }

    private static IReadOnlyList<ExtractedSourceSnippet> ExtractPythonSnippets(string content)
    {
        var snippets = new List<ExtractedSourceSnippet>();

        string[] lines = content
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string trimmedLine = lines[i].TrimStart();

            if (!trimmedLine.StartsWith("def ", StringComparison.Ordinal) &&
                !trimmedLine.StartsWith("async def ", StringComparison.Ordinal))
            {
                continue;
            }

            int functionIndent = lines[i].Length - trimmedLine.Length;
            int endLine = lines.Length - 1;

            for (int j = i + 1; j < lines.Length; j++)
            {
                if (string.IsNullOrWhiteSpace(lines[j]))
                {
                    continue;
                }

                string trimmedCurrentLine = lines[j].TrimStart();
                int currentIndent = lines[j].Length - trimmedCurrentLine.Length;

                if (currentIndent <= functionIndent)
                {
                    endLine = j - 1;
                    break;
                }
            }

            string code = string.Join(
                "\n",
                lines[i..(endLine + 1)]);

            snippets.Add(new ExtractedSourceSnippet
            {
                Code = code.Trim()
            });

            i = endLine;
        }

        return snippets;
    }
}

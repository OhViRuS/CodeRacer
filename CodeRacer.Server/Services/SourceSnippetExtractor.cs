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
            string line = lines[i].Trim();

            if (!LooksLikeMethodSignature(line))
            {
                continue;
            }

            int openingBraceLine = i;

            if (!line.Contains('{'))
            {
                openingBraceLine++;

                while (openingBraceLine < lines.Length &&
                       string.IsNullOrWhiteSpace(lines[openingBraceLine]))
                {
                    openingBraceLine++;
                }

                if (openingBraceLine >= lines.Length ||
                    !lines[openingBraceLine].TrimStart().StartsWith('{'))
                {
                    continue;
                }
            }

            int braceDepth = 0;
            bool foundOpeningBrace = false;
            int endLine = openingBraceLine;

            for (int j = openingBraceLine; j < lines.Length; j++)
            {
                foreach (char character in lines[j])
                {
                    if (character == '{')
                    {
                        braceDepth++;
                        foundOpeningBrace = true;
                    }
                    else if (character == '}')
                    {
                        braceDepth--;
                    }
                }

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

using CodeRacer.Server.Models;
using CodeRacer.Server.Services;

namespace CodeRacer.Server.Tests;

public class SourceSnippetExtractorTests
{
    [Fact]
    public void Extract_CSharpMethod_ReturnsSnippet()
    {
        const string content = """
            public class Calculator
            {
                public int Add(int a, int b)
                {
                    return a + b;
                }
            }
            """;

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.CSharp,
            content);

        Assert.Single(result);
        Assert.Contains("public int Add", result[0].Code);
        Assert.Contains("return a + b;", result[0].Code);
    }

    [Fact]
    public void Extract_CSharpWithTwoMethods_ReturnsBothSnippets()
    {
        const string content = """
            public class Calculator
            {
                public int Add(int a, int b)
                {
                    return a + b;
                }

                public int Subtract(int a, int b)
                {
                    return a - b;
                }
            }
            """;

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.CSharp,
            content);

        Assert.Equal(2, result.Count);
        Assert.Contains("public int Add", result[0].Code);
        Assert.Contains("public int Subtract", result[1].Code);
    }

    [Fact]
    public void Extract_CSharpMethodWithNestedBlock_ReturnsCompleteSnippet()
    {
        const string content = """
            public class Calculator
            {
                public int Absolute(int number)
                {
                    if (number < 0)
                    {
                        return -number;
                    }

                    return number;
                }
            }
            """;

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.CSharp,
            content);

        Assert.Single(result);
        Assert.Contains("if (number < 0)", result[0].Code);
        Assert.Contains("return number;", result[0].Code);
    }

    [Fact]
    public void Extract_CSharpMethodWithBraceInString_ReturnsCompleteSnippet()
    {
        const string content = """
            public class Example
            {
                public string GetValue()
                {
                    return "{";
                }
            }
            """;

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.CSharp,
            content);

        Assert.Single(result);
        Assert.Contains("return \"{\";", result[0].Code);
        Assert.Equal(4, result[0].Code.Split('\n').Length);
    }

    [Fact]
    public void Extract_CSharpMethodWithBraceInComment_ReturnsCompleteSnippet()
    {
        const string content = """
            public class Example
            {
                public int GetValue()
                {
                    // {
                    return 1;
                }
            }
            """;

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.CSharp,
            content);

        Assert.Single(result);
        Assert.Contains("// {", result[0].Code);
        Assert.Contains("return 1;", result[0].Code);
        Assert.Equal(5, result[0].Code.Split('\n').Length);
    }

    [Fact]
    public void Extract_PythonFunction_ReturnsSnippet()
    {
        const string content = """
            def add(a, b):
                return a + b
            """;

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.Python,
            content);

        Assert.Single(result);
        Assert.Contains("def add(a, b):", result[0].Code);
        Assert.Contains("return a + b", result[0].Code);
    }

    [Fact]
    public void Extract_PythonWithTwoFunctions_ReturnsBothSnippets()
    {
        const string content = """
            def add(a, b):
                return a + b

            def subtract(a, b):
                return a - b
            """;

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.Python,
            content);

        Assert.Equal(2, result.Count);
        Assert.Contains("def add(a, b):", result[0].Code);
        Assert.Contains("def subtract(a, b):", result[1].Code);
    }

    [Fact]
    public void Extract_CppFunction_ReturnsSnippet()
    {
        const string content = """
            int add(int a, int b)
            {
                return a + b;
            }
            """;

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.Cpp,
            content);

        Assert.Single(result);
        Assert.Contains("int add(int a, int b)", result[0].Code);
        Assert.Contains("return a + b;", result[0].Code);
    }

    [Fact]
    public void Extract_JavaScriptFunction_ReturnsSnippet()
    {
        const string content = """
            function add(a, b)
            {
                return a + b;
            }
            """;

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.JavaScript,
            content);

        Assert.Single(result);
        Assert.Contains("function add(a, b)", result[0].Code);
        Assert.Contains("return a + b;", result[0].Code);
    }
}

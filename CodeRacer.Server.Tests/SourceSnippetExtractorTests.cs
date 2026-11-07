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
    public void Extract_CSharpMethodWithMultilineVerbatimString_ReturnsCompleteSnippet()
    {
        const string content = """
            public class Example
            {
                public string GetText()
                {
                    var text = @"{
            }";
                    return text;
                }
            }
            """;

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.CSharp,
            content);

        Assert.Single(result);
        Assert.Contains("return text;", result[0].Code);
    }

    [Fact]
    public void Extract_JavaScriptFunctionWithMultilineTemplateString_ReturnsCompleteSnippet()
    {
        const string content = """
            function getText()
            {
                const text = `{
            }`;
                return text;
            }
            """;

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.JavaScript,
            content);

        Assert.Single(result);
        Assert.Contains("return text;", result[0].Code);
    }

    [Fact]
    public void Extract_CSharpMethodWithMultilineRawString_ReturnsCompleteSnippet()
    {
        const string content = """"
            public class Example
            {
                public string GetText()
                {
                    var text = """
                        }
                        """;
                    return text;
                }
            }
            """";

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.CSharp,
            content);

        Assert.Single(result);
        Assert.Contains("return text;", result[0].Code);
    }

    [Fact]
    public void Extract_CSharpMethodWithMultilineParameters_ReturnsSnippet()
    {
        const string content = """
            public class Calculator
            {
                public int Add(
                    int a,
                    int b)
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
        Assert.Contains("public int Add(", result[0].Code);
        Assert.Contains("return a + b;", result[0].Code);
    }

    [Fact]
    public void Extract_CSharpNestedMethod_NormalizesIndentation()
    {
        const string content = """
            namespace Example
            {
                public class Calculator
                {
                    public int Add(int a, int b)
                    {
                        return a + b;
                    }
                }
            }
            """;

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.CSharp,
            content);

        Assert.Single(result);

        const string expected =
            "public int Add(int a, int b)\n" +
            "{\n" +
            "    return a + b;\n" +
            "}";

        Assert.Equal(expected, result[0].Code);
    }

    [Fact]
    public void Extract_CSharpMethodWithTabs_NormalizesIndentation()
    {
        const string content =
            "\tpublic int Add(int a, int b)\n" +
            "\t{\n" +
            "\t\treturn a + b;\n" +
            "\t}";

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.CSharp,
            content);

        Assert.Single(result);
        Assert.Equal(
            "public int Add(int a, int b)\n{\n\treturn a + b;\n}",
            result[0].Code);
    }

    [Fact]
    public void Extract_OneLinePythonFunction_ReturnsNoSnippet()
    {
        const string content = "def add(a, b): return a + b";

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.Python,
            content);

        Assert.Empty(result);
    }

    [Fact]
    public void Extract_TwoLinePythonFunction_ReturnsSnippet()
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
    }

    [Fact]
    public void Extract_FortyLineMethod_ReturnsSnippet()
    {
        string body = string.Join(
            "\n",
            Enumerable.Range(0, 37)
                .Select(index => $"        int value{index} = {index};"));

        string content = $$"""
            public class Example
            {
                public void Run()
                {
            {{body}}
                }
            }
            """;

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.CSharp,
            content);

        Assert.Single(result);
    }

    [Fact]
    public void Extract_FortyOneLineMethod_ReturnsNoSnippet()
    {
        string body = string.Join(
            "\n",
            Enumerable.Range(0, 38)
                .Select(index => $"        int value{index} = {index};"));

        string content = $$"""
            public class Example
            {
                public void Run()
                {
            {{body}}
                }
            }
            """;

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.CSharp,
            content);

        Assert.Empty(result);
    }

    [Fact]
    public void Extract_MoreThanMaximumSnippetCount_ReturnsMaximumSnippetCount()
    {
        string content = string.Join(
            "\n\n",
            Enumerable.Range(0, 60)
                .Select(index =>
                    $"def function_{index}():\n    return {index}"));

        var extractor = new SourceSnippetExtractor();

        var result = extractor.Extract(
            ProgrammingLanguage.Python,
            content);

        Assert.Equal(50, result.Count);
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

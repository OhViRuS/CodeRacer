# CodeRacer - Coding Style Guide

This document defines the uniform coding style for the CodeRacer project. **All team members must follow these rules.**

## Rules
- Use 4 spaces for indentation, never tabs
- Use UTF-8 and end files with a newline
- Remove trailing whitespace
- Use PascalCase for classes, interfaces, enums, methods, and properties
- Use camelCase for local variables and parameters
- Use private fields with an underscore prefix, for example: `_stopwatch`
- Put opening braces on a new line (Allman style)
- Always use braces for `if`, `else`, and loops, even for one-line bodies
- Prefer clear and readable formatting over compressed code
- Sort `using` directives alphabetically, with `System` namespaces first
- Use file-scoped namespaces in C# files (`namespace CodeRacer.Server.Models;`), not block-scoped namespaces

## How to Apply Formatting

```bash
cd CodeRacer.Server

# Format the entire project
dotnet format

# Format only specific files (paths are relative to the current folder)
dotnet format --include Models/CodeSnippet.cs Services/FileSnippetProvider.cs

# Only check, without changing any files (run this before opening a pull request)
dotnet format --verify-no-changes
```

## Team Requirement
All code must be formatted before creating or updating a pull request. The repository should not contain mixed formatting styles.

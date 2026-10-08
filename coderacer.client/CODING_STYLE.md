# CodeRacer - Coding Style Guide

This document defines the uniform coding style for the CodeRacer project. **All team members must follow these rules.**

## Rules
- Use 4 spaces for indentation
- Use spaces, not tabs
- Use UTF-8 and end files with a newline
- Remove trailing whitespace
- Use PascalCase for classes, interfaces, enums, methods, and properties
- Use camelCase for local variables and parameters
- Use private fields with an underscore prefix, for example: _stopwatch
- Keep one brace style across the project
- Prefer clear and readable formatting over compressed code

## How to Apply Formatting

### Automatic formatting before committing:

```bash
# Format all C# files in the project
dotnet format

# Or format a specific file
dotnet format <filepath>
```

## Team Requirement
All code must be formatted before creating or updating a pull request. The repository should not contain mixed formatting styles.
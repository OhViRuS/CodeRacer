# CodeRacer

CodeRacer is a programming-focused typing and practice platform where users improve their familiarity with programming syntax by completing code-based typing challenges.

Unlike traditional typing games that use normal words or sentences, CodeRacer uses real programming code. Challenges can be categorized by programming language, difficulty, and programming concept. Users will also be able to import their own source code files and practice using snippets extracted from their own code.

## Problem

Traditional typing applications mainly focus on ordinary text and do not help programmers practice typing programming-specific syntax such as brackets, operators, method calls, indentation, and common programming structures.

Programmers, especially beginners, may also want a more engaging way to become familiar with common programming patterns and syntax. Existing typing exercises also generally provide predefined content rather than allowing programmers to practice directly with code from their own projects.

## Solution

CodeRacer provides programming-specific typing challenges using both predefined challenges and user-provided source code.

Users will be able to:

* Choose a programming language.
* Complete code typing challenges.
* Import supported source code files.
* Generate typing challenges from snippets extracted from imported code.
* Measure typing speed and accuracy.
* Track their results.
* Practice different programming concepts.
* Compare results with other users.
* Participate in competitive races in later versions.

This allows users to practice not only generic programming examples but also syntax and code structures that appear in their own projects.

The application will gradually develop from a basic single-player typing application into a competitive programming practice platform.

## Alpha Version

The Alpha version will contain the core functionality required for a complete user scenario.

Planned features:

* Basic user interface.
* Selection of programming language.
* Predefined programming code snippets.
* Typing challenge.
* Incorrect character detection.
* Timer.
* Typing speed calculation.
* Accuracy calculation.
* Result screen.
* Basic backend API.
* In-memory or file-based challenge storage.

Example user flow:

1. User chooses a programming language.
2. The application provides a code snippet.
3. The user types the code.
4. The application checks the input.
5. When the challenge is finished, the result is calculated and displayed.

## Beta Version

The Beta version will expand the application with persistent data, user-oriented functionality, and custom code challenges.

Planned features:

* User accounts.
* Entity Framework and relational database.
* Saved race history.
* Personal statistics.
* Programming language and topic filtering.
* Difficulty levels.
* Challenge management.
* Source code file importing.
* Validation of uploaded source code files.
* Extraction of suitable typing snippets from imported code.
* Ability to practice using personally imported code.
* Leaderboards.
* Dependency Injection.
* Async database and file operations.
* Custom exception handling.
* Unit and integration tests.
* CI pipeline.

### Custom Code Import

Users will be able to upload supported source code files, for example:

```text
Program.cs
calculator.cpp
main.py
UserService.java
```

The backend will process the uploaded file and extract sections of code that are suitable for typing challenges.

For example, an imported file may contain:

```csharp
public bool IsAdult(User user)
{
    return user.Age >= 18;
}

public string GetDisplayName(User user)
{
    return $"{user.FirstName} {user.LastName}";
}
```

Instead of requiring the user to type the entire file, CodeRacer could extract individual functions or other suitable sections and turn them into separate challenges.

```text
Uploaded file
     ↓
File validation
     ↓
Code processing
     ↓
Snippet extraction
     ↓
Typing challenges
```

This functionality allows users to practice typing code that is relevant to their own projects and programming habits.

## Final Version

The Final version will focus on competitive functionality, personalization, and overall application quality.

Planned features:

* Multiplayer races.
* Private race rooms.
* Real-time race progress.
* Server-managed race state.
* Global rankings.
* Advanced statistics.
* Programming skill profiles.
* Personalized challenge recommendations.
* Improved custom code processing.
* Challenges generated from user-imported code.
* Automatic Entity Framework migrations.
* At least 80% unit and integration test coverage.
* CI pipeline with quality gates.
* Application monitoring and health metrics.
* Stable final application.

## Technologies

Planned technology stack:

### Backend

* C#
* ASP.NET Core
* Entity Framework Core
* REST API
* SignalR for real-time multiplayer functionality

### Frontend

* React
* JavaScript or TypeScript
* HTML
* CSS

### Database

* Relational database using Entity Framework Core

### Development

* Git
* GitHub
* Pull Requests
* GitHub Actions

## Development Workflow

All changes to the project will be made through separate branches and reviewed using GitHub Pull Requests.

The `main` branch is protected by repository rules, requiring changes to be made and reviewed through Pull Requests before they can be merged.

Example workflow:

```text
main
 |
 └── feature/code-file-import
          |
          └── Pull Request
                  |
                  └── Review
                          |
                          └── Merge into main
```

## Current Status

The project is currently in the initial setup stage.

The first goal is to create a basic working connection between the React frontend and ASP.NET Core backend before implementing the CodeRacer gameplay.

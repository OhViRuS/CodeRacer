# CodeRacer

CodeRacer is a programming-focused typing and practice platform where users improve their familiarity with programming syntax by completing code-based typing challenges.

Unlike traditional typing games that use normal words or sentences, CodeRacer uses real programming code. Challenges can be categorized by programming language, difficulty, and programming concept. Users will also be able to import their own source code files and practice using snippets extracted from their own code.

## Team Members

1. Jonas Kirkilovskis (Team Leader, Backend) - *OhViRuS*
2. Haroldas Vaitkus (Backend) - *Bobas44*
3. Benas Lazdauskas (Backend) - *Benukage*
4. Lukas Gudonis (Frontend) - *lycoris-mori*
5. Kristupas Jurkus (Frontend, QA) - *JurkusKristupas*

## Problem

Traditional typing applications mainly focus on ordinary text and do not help programmers practice typing programming-specific syntax such as brackets, operators, method calls, indentation, and common programming structures.

Programmers, especially beginners, may also want a more engaging way to become familiar with common programming patterns and syntax. Existing typing exercises also generally provide predefined content rather than allowing programmers to practice directly with code from their own projects.

## Solution

CodeRacer provides programming-specific typing challenges using both predefined challenges and user-provided source code.

Users will be able to:

* Choose a programming language (e.g., C#, C++, Python, JavaScript).
* Complete code typing challenges.
* Import supported source code files.
* Generate typing challenges from snippets extracted from imported code.
* Measure typing speed and accuracy.
* Track their results.
* Practice different programming concepts.
* Compare results with other users.
* Participate in competitive races.

This allows users to practice not only generic programming examples but also syntax and code structures that appear in their own projects.

## Development Roadmap

The planned development of CodeRacer, including the Alpha, Beta, and Final versions, is described in [ROADMAP.md](ROADMAP.md).

## Technologies

Planned technology stack:

### Backend

* C#
* ASP.NET Core
* Entity Framework Core
* REST API
* SignalR for real-time multiplayer functionality

### Frontend

* React + JavaScript
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

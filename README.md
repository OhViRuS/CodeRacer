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


## Contributing

Branch naming, code formatting and pull request process: see [CONTRIBUTING.md](CONTRIBUTING.md). Every new pull request also gets [`docs/pull_request_template.md`](docs/pull_request_template.md) pre-filled.

## Development Roadmap

The planned development of CodeRacer, including the Alpha, Beta, and Final versions, is described in [ROADMAP.md](ROADMAP.md).

## Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A current [Node.js LTS](https://nodejs.org/) release with npm
- [Visual Studio 2026](https://visualstudio.microsoft.com/downloads/) with the ASP.NET and web development workload, [Visual Studio Code](https://code.visualstudio.com/) with C# Dev Kit, or JetBrains Rider
- A trusted ASP.NET Core development certificate: `dotnet dev-certs https --trust`
- [PostgreSQL](https://www.postgresql.org/download/) running locally (or reachable from your machine); all application data is stored there

## Database Setup

CodeRacer stores all its data (users, code snippets, race results) in PostgreSQL through Entity Framework Core.

1. Install PostgreSQL and make sure it is running.
2. Give the server a connection string named `CodeRacer`. Do not commit your password; set it as an environment variable instead (PowerShell, in the terminal you start the server from):

   ```pwsh
   $env:ConnectionStrings__CodeRacer = "Host=localhost;Port=5432;Database=coderacer;Username=postgres;Password=<your password>"
   ```

3. Start the server (see below). On startup it creates or updates the database by applying all pending EF Core migrations and then adds any starter snippets from `Data/snippets.json` that are missing. There is no separate `dotnet ef database update` step.

If PostgreSQL is not running or the connection string is wrong, the server stops at startup with a connection error.

After changing an entity or the model, add a migration from `CodeRacer.Server` and commit the generated files in `Migrations/`:

```pwsh
dotnet ef migrations add <MigrationName>
```

## Build and Run

From the repository root:

```pwsh
cd CodeRacer.Server
dotnet restore
dotnet build
dotnet run
```

On the first Debug build, MSBuild installs the frontend dependencies. Running the backend starts the React development server through the ASP.NET Core SPA proxy. Open the HTTPS address printed by `dotnet run` (the default project profile uses `https://localhost:7161`).

Useful frontend commands can also be run directly:

```pwsh
cd coderacer.client
npm ci
npm run lint
npm run build
```

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

* PostgreSQL, accessed through Entity Framework Core (migrations are applied automatically at startup)

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

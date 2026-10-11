## Before You Start

See [`docs/definition-of-done.md`](docs/definition-of-done.md) for what "finished" means here, and [`CODING_STYLE.md`](CODING_STYLE.md) for the uniform coding style that must be used.

## Branch Naming

Pattern: `[initials]/[issue-number]-[short-title]`

Example &mdash; Tomas picks up issue #12, "Add session search":

`tj/12-add-session-search`

- Initials first (lowercase), separated from the rest by `/`.
- Issue number next, then a short kebab-case description of the task.
- Branch off `main`; open a pull request back into `main` when ready for review.

## Code Formatting

- **Backend (C#):** run `dotnet format` before committing so contributions read the same regardless of which IDE wrote them.
- **Frontend (JavaScript):** match the style already used under `coderacer.client/src` - most editors pick this up automatically if you have a formatter (Prettier/ESLint) configured.
- Whichever IDE you use (Visual Studio, VS Code, or Rider, turning on "format on save" makes this automatic instead of a manual step before every commit.

## Opening a Pull Request

- Every change ships as a pull request into `main` - no direct pushes, even for small fixes.
- GitHub pre-fills the description from [`docs/pull_request_template.md`](docs/pull_request_template.md) - fill it in for real, don't leave the placeholders.
- Before marking it ready for review, check it against [`docs/definition-of-done.md`](docs/definition-of-done.md).

## Filing Issues

Use GitHub Issues for anything that isn't ready to be a pull request yet - a bug, a roadmap item worth tracking, a question. A short, specific title beats a detailed body nobody reads; add detail in the first comment if it's needed.
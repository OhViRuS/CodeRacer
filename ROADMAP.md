# CodeRacer Roadmap

This roadmap describes the planned development of CodeRacer
through the Alpha, Beta and Final versions and maps the planned
functionality to the requirements of each lab assignment.

## Alpha - matches Lab Assignment #1 (lectures 1-6)

### Features

- [ ] Select a programming language.
- [ ] Receive a predefined code snippet.
- [ ] Complete a typing challenge.
- [ ] Detect incorrectly typed characters.
- [ ] Track completion time.
- [ ] Calculate typing speed.
- [ ] Calculate accuracy.
- [ ] Display race results.

### Requirement Coverage

- [ ] Own `class`, `struct`, `record` and `enum`.
- [ ] At least one immutable type.
- [ ] Properties in `class` and `struct`.
- [ ] Named and optional arguments.
- [ ] Extension method.
- [ ] Appropriate collection iteration.
- [ ] Stream used to load code snippets.
- [ ] LINQ to Objects.
- [ ] Standard .NET interface implementation.

## Beta - matches Lab Assignment #2 (lectures 7-10)

### Features

- [ ] User accounts.
- [ ] Save typing challenge results.
- [ ] Personal statistics and race history.
- [ ] Difficulty and topic filtering.
- [ ] Import source code files with supported extensions (`.cs`, `.cpp`, `.py`, `.java`) and a maximum file size of 5 MB.
- [ ] Validate imported files.
- [ ] Extract suitable code snippets from imported files.
- [ ] Generate typing challenges from a user's own code.
- [ ] Leaderboards.

### Requirement Coverage

- [ ] Entity Framework and relational database.
- [ ] Generic type and generic method.
- [ ] Custom exception.
- [ ] `async`/`await` for I/O.
- [ ] Shared memory/concurrency scenario.
- [ ] Dependency Injection.
- [ ] At least 50% unit/integration test coverage.

## Final - matches Lab Assignment #3 (lectures 11-14)

### Features

- [ ] Multiplayer races.
- [ ] Private race rooms.
- [ ] Real-time player progress.
- [ ] Global rankings.
- [ ] Advanced statistics.
- [ ] Personalized challenge recommendations.
- [ ] Programming skill profiles.

### Multiplayer Format

Multiplayer races use a free-for-all format with up to 5 players.
All players receive the same code snippet and begin typing after
a synchronized countdown.

Each player competes individually. The first player to correctly
complete the entire code snippet wins. After the race, players are
ranked based on completion time. Typing speed, accuracy and mistakes
are also displayed.

Players can create private race rooms and invite other players using
a room code or invitation link. The room creator can configure race
settings such as programming language and difficulty.

### Requirement Coverage

- [ ] Automatic Entity Framework migrations.
- [ ] At least 80% unit/integration test coverage.
- [ ] CI pipeline gating pull requests.
- [ ] Automated tests and an additional CI quality gate.
- [ ] Live application metrics/monitoring.
- [ ] Stable application.
- [ ] 4-6 minute demonstration video.

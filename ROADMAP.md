# CodeRacer Roadmap

This roadmap describes the planned development of CodeRacer
through the Alpha, Beta and Final versions and maps the planned
functionality to the requirements of each lab assignment.

## Alpha - matches Lab Assignment #1 (lectures 1-6)

### Features

- [x] Select a programming language.
- [x] Receive a predefined code snippet.
- [x] Complete a typing challenge.
- [x] Detect incorrectly typed characters.
- [x] Track completion time.
- [x] Calculate typing speed.
- [x] Calculate accuracy.
- [x] Display race results.

### Requirement Coverage

- [x] Own `class`, `struct`, `record` and `enum`.
- [x] At least one immutable type.
- [x] Properties in `class` and `struct`.
- [x] Named and optional arguments.
- [x] Extension method.
- [x] Appropriate collection iteration.
- [x] Stream used to load code snippets.
- [x] LINQ to Objects.
- [x] Standard .NET interface implementation.

## Beta - matches Lab Assignment #2 (lectures 7-10)

### Features

- [ ] User accounts.
- [x] Save typing challenge results.
- [x] Personal statistics and race history.
- [ ] Difficulty and topic filtering.
- [x] Import source code files with supported extensions (`.cs`, `.cpp`, `.py`, `.js`) and a maximum file size of 5 MB.
- [X] Validate imported files.
- [x] Extract suitable code snippets from imported files.
- [x] Generate typing challenges from a user's own code.
- [ ] Leaderboards.

### Requirement Coverage

- [x] Entity Framework and relational database.
- [ ] Generic type and generic method.
- [x] Custom exception.
- [x] `async`/`await` for I/O.
- [ ] Shared memory/concurrency scenario.
- [x] Dependency Injection.
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

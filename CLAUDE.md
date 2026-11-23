## About

Nexora is a cross-platform desktop audio player.
- .NET version: .NET 10
- UI: Avalonia (MVVM, CommunityToolkit.Mvvm)
- Playback: LibVLC

## Project rules

- `src/Core` must not reference other projects.
- Services are wired with Microsoft.Extensions.DependencyInjection; register new ones in `src/Nexora/CompositionRoot.cs`.
- Test helpers shared by different test projects go in `tests/Tests.Shared`.
- Package versions live in `Directory.Packages.props`; `PackageReference` items in `.csproj` have no `Version`.
- All text in the project must be in English.

## Commands

Debug build:
```
dotnet build Nexora.slnx -v q -nologo
```
Unit tests (all test projects):
```
dotnet test Nexora.slnx -v q -nologo --logger "console;verbosity=minimal"
```
Single test: add `--filter "FullyQualifiedName~LruCacheTests"`.
Coverage and cyclomatic complexity of methods; fails unless each has 100% coverage and complexity of at most 8
(runs all tests with coverage; `--no-test` reuses the last run). Pass any number of methods and classes:
```
scripts/method-metrics.sh LruCache.TryGet TrackControlVmRegistry
```
Run: `dotnet run --project src/Nexora` (GUI; blocking — run in background).
Publish: `scripts/Publish.ps1` / `scripts/publish.sh`.
Profile (Linux, perf + Hotspot): `scripts/profile.sh [--dwarf] [output file]` (GUI; blocking).

## UI tests

`tests/Nexora.UiTests` runs the real `MainWindow` on Avalonia's headless platform (xUnit v3), so no display is needed.
- Mark tests with `[AvaloniaFact]` and open the window with `using var app = new TestMainWindow();`.
  It wires services through `CompositionRoot` with a mocked audio player and loudness analyzer.
- To test playback end to end, pass a `FakeMediaEngine` (`tests/Tests.Shared/Playback`):
  `new TestMainWindow(mediaEngine)` runs the real audio player on top of it. Call `Dispatcher.UIThread.RunJobs()`
  after adding a track to the queue and before playing it.
- Interact with `app.Click(control)` or Avalonia.Headless input methods on `app.Window` (`KeyPress`, `MouseMove`, ...).
- When a test ends, the last rendered frame is saved to `tests/Nexora.UiTests/screenshots/<test method>.png`
  (ignored by git); open it to check how the UI looks.

## Style analyzers

`src/Analyzers` reports these on every build:
- `NX0001` lines are at most 120 characters.
- `NX0002` no trailing whitespace.
- `NX0003` members go in order: properties, fields, events, constructors, then methods.
- `NX0004` one blank line between members; none between consecutive single-line fields/properties, events
  or bodiless methods.
- `NX0005` an `if` that is not the first statement in its block is preceded by a blank line.
- `NX0006` use `== null` / `!= null`, not `is null` / `is not null`.
- `NX0007` no local functions; use a lambda or a method.
- `NX0008` no tuples; declare a small class with named properties.
- `NX0009` no range or spread `..` syntax.
- `NX0010` wrapped parameters, arguments and collections go one per line.
- `NX0011` spell out built-in types (`int`, `string`, ...) instead of `var`.

## Null safety

- Annotate types precisely: `T?` only where `null` is a valid value, non-nullable otherwise.
- Handle `null` explicitly (checks, `?.`, `??`, `!= null`) instead of suppressing warnings.
- Use the `!` operator only when non-null is guaranteed by logic the compiler can't see; never to silence a warning.
- No `#nullable disable` / `#pragma warning disable`.

## AOT

Release builds use `PublishAot` and `TreatWarningsAsErrors`, so:
- Avoid reflection and dynamic type loading; if a library needs it, preserve types explicitly using `[DynamicDependency]`.
- Compiled bindings are on by default — every axaml view/control must set `x:DataType`.

Build in Release (`dotnet build Nexora.slnx -c Release -v q -nologo`) only when the user asks.

## Commits

Conventional Commits, with a scope when the change belongs to one area (`feat(core): ...`, `refactor(viewmodels): ...`)
and without one for repo-wide changes (`chore: ...`). Lowercase, imperative, no trailing period.

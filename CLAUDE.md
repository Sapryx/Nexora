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
CRAP score of a method (runs all tests with coverage; `-NoTest` reuses the last run):
```
pwsh scripts/Crap.ps1 -Method "LruCache.TryGet"
```
Run: `dotnet run --project src/Nexora` (GUI; blocking — run in background).
Publish: `scripts/Publish.ps1` / `scripts/publish.sh`.

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

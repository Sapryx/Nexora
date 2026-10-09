# Nexora

Cross-platform desktop audio player on .NET 10 + Avalonia (MVVM, CommunityToolkit.Mvvm), playback via LibVLC.

## Language

All text in the project must be in English: code, comments, log messages, exception messages, UI strings, docs, text/config files, commit messages.

## Structure

- `src/Core` — domain logic with no UI or third-party libraries: `Playback` (`IAudioTrack`, `IAudioPlayer`), `Playlists` (`Playlist`, `PlaylistRegistry` with `GlobalPlaylist`), `Storage` (`FileTrackLoader`, music directory/format/parallelism providers), `Collections/LruCache`, integration interfaces.
- `src/Infrastructure` — implementations of Core interfaces: `AudioPlayer` (LibVLC), `TagLib*Loader` (metadata and covers), `DiscordRichPresenceService`. Depends only on Core.
- `src/Nexora.Theme` — styles and resources (`*.axaml`), no logic.
- `src/Nexora` — the application: `ViewModels/` (`*Vm`, factories in `Factories/`), `Controls/` and `Views/` (axaml + code-behind), `Media/CoverCache`, `Render/` (SkSL dithering shader), `Logging/`.
- DI registrations: `src/Nexora/CompositionRoot.cs`. Register new services there.
- `tests/Core.Tests` — xUnit + Moq, covers Core only.

## Commands

Build (errors only):
```
dotnet build Nexora.sln -v q -nologo -clp:ErrorsOnly
```
Tests (minimal output):
```
dotnet test tests/Core.Tests -v q -nologo --logger "console;verbosity=minimal"
```
Single test: add `--filter "FullyQualifiedName~LruCacheTests"`.
Run: `dotnet run --project src/Nexora` (GUI; blocking — run in background).
Publish: `scripts/Publish.ps1` / `scripts/publish.sh`.

## Debugging

- Logs: `%APPDATA%/Nexora/logs/session.log` (overwritten on every launch), crashes — `crash-*.log` in the same folder. Read the log tail instead of taking screenshots.
- Music is loaded from `Environment.SpecialFolder.MyMusic` (`MusicDirectoryProvider`).
- Avalonia DevTools (F12) are available in Debug.

## Rider MCP

If the `rider` MCP server is available (Rider must be running with this project):
- file problems/inspections, renames and refactorings, find usages — via Rider;
- build and tests — via `dotnet` (commands above), it's faster and produces shorter output.

## Code style

- Allman braces, `if(`/`foreach(` with no space before the parenthesis, file-scoped namespaces.
- Private fields in camelCase without `_`, assigned via `this.field = field`.
- VM properties: `[ObservableProperty] public partial T Name { get; set; }`, reactions — `partial void OnNameChanged`.
- Local variables: lowercase built-in types (`bool`, `byte`, `sbyte`, `char`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `nint`, `nuint`, `float`, `double`, `decimal`, `string`) are spelled out explicitly (`int count = 0;`); everything else uses `var`.
- No local functions. Lambdas are fine.
- Collections via collection expressions (`[]`). UI updates from background events — via `Dispatcher.UIThread.Post`.
- Tests: names `Method_Condition_Result`, mocks in `xxxMock` fields, lambdas `it => ...`, `NullLogger<T>.Instance` for loggers.

## Logging

- Inject `ILogger<T>` via the constructor.
- Log through the `Core.Logging` extensions (ZLogger-based) with interpolated strings: `logger.Info($"Loaded {count} tracks")`. Levels: `Trace`, `Debug`, `Info`, `Warn`, `Error`, `Crit`; each has an overload taking an `Exception` first.
- Do not use `LogInformation`/`ZLogInformation` and friends directly.

## Null safety

Nullable reference types are enabled in all projects; Release uses `TreatWarningsAsErrors`, so any nullability warning fails the build (in `Core.Tests` — in every configuration).
- Annotate types precisely: `T?` only where `null` is a valid value, non-nullable otherwise.
- Handle `null` explicitly (checks, `?.`, `??`, `is not null`) instead of suppressing warnings.
- Use the `!` operator only when non-null is guaranteed by logic the compiler can't see; never to silence a warning. No `#nullable disable` / `#pragma warning disable`.

## AOT

Release builds use `PublishAot` and `TreatWarningsAsErrors`, so:
- avoid reflection and dynamic type loading; if a library needs it, preserve types explicitly (see `[DynamicDependency]` in `TagLibMetadataLoader`);
- compiled bindings are on by default — every axaml view/control must set `x:DataType`;
- don't introduce warnings: they break the Release build.

Build in Release (`dotnet build Nexora.sln -c Release -v q -nologo -clp:ErrorsOnly`) only when the user asks.

## Commits

Conventional Commits with a scope: `feat(core): ...`, `fix(ui): ...`, `perf(storage): ...`, `refactor(viewmodels): ...`, `chore: ...`. Lowercase, imperative, no trailing period.

## Do not read

`bin/`, `obj/`, `.idea/`, `Test Results/`, `Report/`.

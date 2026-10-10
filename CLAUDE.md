# Nexora

Cross-platform desktop audio player on .NET 10 + Avalonia (MVVM, CommunityToolkit.Mvvm), playback via LibVLC.

## Language

All text in the project must be in English: code, comments, log messages, exception messages, UI strings, docs, text/config files, commit messages.

## Structure

- `src/Core` — domain logic and its implementations, no UI: `Playback` (`IAudioTrack`, `IAudioPlayer`, `AudioPlayer` on LibVLC), `Playlists` (`Playlist`, `PlaylistRegistry` with `GlobalPlaylist`), `Storage` (`FileTrackLoader`, `TagLib*Loader` for metadata and covers, music directory/format/parallelism providers), `Search` (`TrackSearchQuery`: substring search ignoring case, diacritics and punctuation over title/artists tolerating typos by optimal string alignment Damerau-Levenshtein distance; matches the whole query within one field or every word within any field; also tries the query retyped in other keyboard layouts via `KeyboardLayoutTranslator`, built at startup from `IKeyboardLayoutProvider`), `Collections/LruCache`, `Integrations` (`DiscordRichPresenceService`), `Logging`. Must not depend on Avalonia or other UI libraries.
- `src/Nexora.Theme` — styles and resources (`*.axaml`), no logic.
- `src/Nexora` — the application: `ViewModels/` (`*Vm`, factories in `Factories/`), `Controls/` and `Views/` (axaml + code-behind), `Media/CoverCache`, `Threading/` (`IUiDispatcher`), `Input/` (keyboard layout providers: Windows API, Wayland compositor keymap via libwayland-client + libxkbcommon, unsupported fallback; chosen in `CompositionRoot`), `Render/` (SkSL dithering shader), `Logging/`.
- DI registrations: `src/Nexora/CompositionRoot.cs`. Register new services there.
- `tests/Core.Tests` — xUnit + Moq, covers Core.
- `tests/Nexora.Tests` — xUnit + Moq, covers view models (no Avalonia platform needed: `IUiDispatcher` is mocked to run actions inline).
- Shared build properties: `Directory.Build.props`. Package versions are managed centrally in `Directory.Packages.props` — `PackageReference` items in `.csproj` have no `Version`.

## Commands

Build (errors only):
```
dotnet build Nexora.sln -v q -nologo -clp:ErrorsOnly
```
Tests (minimal output):
```
dotnet test tests/Core.Tests -v q -nologo --logger "console;verbosity=minimal"
dotnet test tests/Nexora.Tests -v q -nologo --logger "console;verbosity=minimal"
```
Single test: add `--filter "FullyQualifiedName~LruCacheTests"`.
Run: `dotnet run --project src/Nexora` (GUI; blocking — run in background).
Publish: `scripts/Publish.ps1` / `scripts/publish.sh`.

## Verification

Cover changes with unit tests. Run the app and verify the change in it (integration testing) only when unit tests can't cover the behavior: layout and rendering, focus and input handling, window behavior, real playback, startup and DI wiring. Pure logic (Core, view models) is verified by unit tests alone.

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
- No target-typed `new()`: always spell out the type (`new Playlist()`). Parentheses are required even with an object initializer: `new Metadata() { Title = title }`.
- Collections via collection expressions (`[]`). UI updates from background events — via the injected `IUiDispatcher` (`Nexora.Threading`), not `Dispatcher.UIThread` directly, so view models stay testable.
- Tests: names `Method_Condition_Result`, mocks in `xxxMock` fields, lambdas `it => ...`, `NullLogger<T>.Instance` for loggers.

## Logging

- Inject `ILogger<T>` via the constructor.
- Log through the `Core.Logging` extensions (ZLogger-based) with interpolated strings: `logger.Info($"Loaded {count} tracks")`. Levels: `Trace`, `Debug`, `Info`, `Warn`, `Error`, `Crit`; each has an overload taking an `Exception` first.
- Do not use `LogInformation`/`ZLogInformation` and friends directly.

## Null safety

Nullable reference types are enabled in all projects; Release uses `TreatWarningsAsErrors`, so any nullability warning fails the build (in the test projects — in every configuration).
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

Every commit must build on its own. A feature may be incomplete, but the app must start and have no critical bugs at each commit. When splitting changes into several commits, make sure each intermediate state satisfies this.

## Do not read

`bin/`, `obj/`, `.idea/`, `Test Results/`, `Report/`.

# Nexora

Cross-platform desktop audio player on .NET 10 + Avalonia (MVVM, CommunityToolkit.Mvvm), playback via LibVLC.

## Language

All text in the project must be in English: code, comments, log messages, exception messages, UI strings, docs, text/config files, commit messages.

## Structure

- `src/Core` — domain logic and its implementations, no UI: `Playback` (`IAudioTrack`, `IAudioPlayer`, `AudioPlayer` on LibVLC), `Playlists` (`Playlist`, `PlaylistRegistry` with `GlobalPlaylist`), `Storage` (`FileTrackLoader`, `TagLib*Loader` for metadata and covers, music directory/format/parallelism providers, `FileVolumeStorage` for the volume kept between launches), `Search` (`TrackSearchQuery`: substring search ignoring case, diacritics and punctuation over title/artists tolerating typos by optimal string alignment Damerau-Levenshtein distance; matches the whole query within one field or every word within any field; also tries the query retyped in other keyboard layouts via `KeyboardLayoutTranslator`, built at startup from `IKeyboardLayoutProvider`), `Collections/LruCache`, `Integrations` (`DiscordRichPresenceService`), `Logging`. Must not depend on Avalonia or other UI libraries.
- `src/Nexora.Theme` — styles and resources (`*.axaml`), no logic.
- `src/Nexora` — the application: `ViewModels/` (`*Vm`, factories in `Factories/`), `Controls/` and `Views/` (axaml + code-behind), `Media/CoverCache`, `Threading/` (`IUiDispatcher`), `Input/` (keyboard layout providers: Windows API, Wayland compositor keymap via libwayland-client + libxkbcommon, unsupported fallback; chosen in `CompositionRoot`), `Render/` (SkSL dithering shader), `Logging/`.
- DI registrations: `src/Nexora/CompositionRoot.cs`. Register new services there.
- `tests/Core.Tests` — xUnit + Moq, covers Core.
- `tests/Nexora.Tests` — xUnit + Moq, covers view models (no Avalonia platform needed: `IUiDispatcher` is mocked to run actions inline).
- `tests/Tests.Shared` — class library with test helpers shared by both test projects (`Logging/TestLogger`, `Logging/LogEntry`). Put a helper here instead of copying it into each test project.
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

- Logs: `%APPDATA%/Nexora/logs/session-<yyyy-MM-dd_HH-mm-ss>.log` (`~/.config/Nexora/logs` on Linux), one file per launch, the 10 newest are kept. Unhandled exceptions are written there as `[Crit ]` with the stack trace. Level names are padded to 5 characters inside the brackets (`[Info ]`, `[Warn ]`, `[Crit ]`) so messages line up. Read the log tail instead of taking screenshots.
- Minimum log level is `Info` by default; pass `--log-level trace|debug|info|warn|error|crit` to change it (`LogLevelArgument`), e.g. `dotnet run --project src/Nexora -- --log-level trace`. The log starts with a level legend (`LoggingInitializer.LogLevelLegend`): one line per level, written at that level so the console shows its color, with `<--` at the chosen one; its category bypasses the minimum level, so these lines (including `[Error]` and `[Crit ]`) are in every log and are not real errors. An unknown value logs a warning and keeps the default.
- The same logs go to stdout; on Windows (`WinExe`) the app attaches to the console of the terminal it was started from, so `dotnet run` prints them. Console lines are colored as a whole with ANSI codes (gray Trace/Debug, yellow Warn, red Error, white on red Crit, including the exception text), also when stdout is redirected; only the log file is plain text. When grepping captured stdout, strip `\x1b[...m` codes first.
- Only one instance runs at a time (named mutex in `Program.Main`): a second launch exits immediately without logging.
- Music is loaded from `Environment.SpecialFolder.MyMusic` (`MusicDirectoryProvider`).
- Volume is owned by `AudioPlayer`, not read back from LibVLC: before playback LibVLC reports 0 on Linux (no PulseAudio stream yet) and the per-app mixer level on Windows, and it sends `VolumeChanged` -1 when the audio output is destroyed (ignored). The last volume is saved to `Nexora/volume.txt` next to the logs and restored on start (50 on first launch); delete the file to reset it.
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
- No tuples (`(int, string)`, `(a, b) = ...`, tuple literals in asserts): declare a small class with named properties instead.
- No range syntax with two dots: neither ranges (`text[..length]`, `items[1..]`) nor spread in collection expressions (`[.. items]`). Use `Substring`, `Take`/`Skip`, `ToList()`/`Concat()` instead.
- Collections via collection expressions (`[]`). UI updates from background events — via the injected `IUiDispatcher` (`Nexora.Threading`), not `Dispatcher.UIThread` directly, so view models stay testable.
- Tests: names `Method_Condition_Result`, mocks in `xxxMock` fields, lambdas `it => ...`, `NullLogger<T>.Instance` for loggers (`TestLogger<T>` when the test checks what is logged).

## Logging

- Everything logging-related (adapters for third-party loggers, sinks, crash logging, test loggers) lives in a `Logging` directory and namespace: `Core.Logging`, `Nexora.Logging`, `Tests.Shared.Logging`, and `Core.Tests.Logging`/`Nexora.Tests.Logging` for tests of logging code.
- Inject `ILogger<T>` via the constructor.
- Log through the `Core.Logging` extensions (ZLogger-based) with interpolated strings: `logger.Info($"Loaded {count} tracks")`. Levels: `Trace`, `Debug`, `Info`, `Warn`, `Error`, `Crit`; each has an overload taking an `Exception` first.
- Do not use `LogInformation`/`ZLogInformation` and friends directly.
- Levels:
  - `Trace`/`Debug` — details of individual items and steps;
  - `Info` — startup and one-off lifecycle events;
  - `Warn` — recoverable failures the app works around (a skipped file, a missing cover, an optional integration failing);
  - `Error` — an operation failed and its result is lost (track loading, playback);
  - `Crit` — only unhandled exceptions (`CrashLogger`), plus the empty `[Crit ]` line of the startup level legend.
- Name the subject in the message (file path, track) and pass the exception to the `Exception` overload instead of formatting it into the message.
- Catch-all `catch(Exception)` only at boundaries where one failure must not break the rest (per file, per cover), and always log it.
- Don't log in paths that run per keystroke, per frame or per playback position update.
- Third-party libraries log into the same session log with a prefix and adjusted levels:
  - `(Avalonia) <area>:` — `AvaloniaLogSink`, `Warning` as `Warn`, `Error`/`Fatal` as `Error`, lower levels dropped;
  - `(LibVLC) <module>:` — `LibVlcLogForwarder`, errors as `Error`, warnings and notices as `Debug`, debug messages dropped. LibVLCSharp raises `Log` via `Task.Run`, so these lines may be slightly out of order and the last ones before a crash may be missing;
  - `(Discord)` — `DiscordRpcLogger`, library errors as `Debug`, info and warnings as `Trace`, its trace dropped. The library reconnects forever while Discord is closed, so `LoggingInitializer` filters the `DiscordRpcLogger` category below `Information`, which hides all library output; lower that filter to debug the integration. `DiscordRichPresenceService` itself logs connecting, Discord errors (`Warn`) and, once per disconnect, that Discord is not running;
  - `(Diagnostics)` — `DiagnosticsLogListener` for `System.Diagnostics.Trace`/`Debug` output (Svg parsing errors, LibVLCSharp native library loading): `Error` as `Warn`, `Warning` and plain `WriteLine` as `Debug`. Prefixes must not reuse level names, or a line looks like it has two levels.
- Native libraries we call directly (libxkbcommon, libwayland-client) still print their own errors to stderr; our wrappers log the resulting failure.
- Tests that check log output use `TestLogger<T>` (records `LogEntry` items with level, message and exception); ZLogger messages can't be matched with Moq `Verify` because they are formatted only during the `Log` call.

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

---
name: ui-tests
description: How to write and debug Nexora's headless UI tests in tests/Nexora.UiTests (TestMainWindow, its helpers, fakes for playback, tag writes, time and Discord, screenshots). Use when adding or changing a UI test, testing a behavior end to end, or when a UI test fails or hangs.
---

# UI tests in Nexora

`tests/Nexora.UiTests` runs the real `MainWindow` on Avalonia's headless platform (xUnit v3), so no display is needed.

## Opening the window

- Mark tests with `[AvaloniaFact]` and open the window with `using var app = new TestMainWindow();`.
  It wires services through `CompositionRoot` with a mocked audio player (`app.AudioPlayerMock`) and loudness
  analyzer. Resolve any other service with `app.Services.GetRequiredService<T>()`.
- `TestMainWindow` only builds the window. It does not run `CompositionRoot.Start`, so services started there
  (`MainWindowVm.Initialize`, `MusicLibrary.LoadAsync`, `IRichPresenceService.Initialize`) are not running.
  Start the ones the test needs yourself.
- When a test ends, the last rendered frame is saved to `tests/Nexora.UiTests/screenshots/<test method>.png`
  (ignored by git). Open it to check how the UI looks.

## Helpers on `TestMainWindow`

- Find controls: `app.Find<PlaybackControl>()` (the single control of that type),
  `app.Find<Slider>("PlaybackPositionSlider")` (by `x:Name`), and `app.FindButton("play", app.Find<PlaybackControl>())`
  (the button showing that `Icon`, in the whole window when no container is given).
- Input: `app.Click(control)`, `app.Click(point)`, `app.Hover(control)`, `app.PressKey(Key.Left, PhysicalKey.ArrowLeft)`.
  Each call runs pending UI jobs afterwards.
- Points: `app.PointIn(control, 0.3, 0.5)` is the point at 30% of the control's width and half its height, in window
  coordinates. Use it for presses, moves and drags with `app.Window.MouseDown` / `MouseMove` / `MouseUp`.
- Call `Dispatcher.UIThread.RunJobs()` after raw `app.Window` input or after changing services directly.

## Fakes

- Playback end to end: pass a `FakeMediaEngine` (`tests/Tests.Shared/Playback`). `new TestMainWindow(mediaEngine)` runs
  the real audio player on top of it. Call `Dispatcher.UIThread.RunJobs()` after adding a track to the queue and before
  playing it. Drive it with `RaisePositionChanged` and `RaiseEndReached`; check `OpenedPath`, `OpenedStartTimeMs`,
  `OpenCount` and `SoughtPositions`.
- Tag writes: pass an `IMetadataSaver` as the second argument. `GatedMetadataSaver` holds each write until `Finish()`
  and fails it if the media engine holds the file, as Windows would. `TestWavFile` (`tests/Tests.Shared`) creates a
  real file to write tags to. `TrackTagWriter.SaveLoudnessAsync` writes on the calling thread, so don't call it on the
  UI thread while a gated saver holds the write.
- Time: `app.TimeProvider` is a `FakeTimeProvider` registered as the app's `TimeProvider`. Move it with
  `app.TimeProvider.Advance(...)`, then run UI jobs. Timers and throttles never fire on their own.
- Covers: `app.CoverLoader` is a `FakeTrackCoverLoader`; `app.CoverLoader.Add(audioPath, new PixelSize(w, h))` gives
  that track a generated PNG cover. Tracks without one have no cover. Add tracks to the library with
  `MusicLibrary.Playlist.AddTracks(...)` and run UI jobs. Covers load on background threads, so wait with
  `app.WaitUntil(condition)`, which runs UI jobs until the condition holds or 5 seconds pass.
- Display scale: the headless window always renders at 100%. Set `app.RenderScale.RenderScaling` (e.g. `1.5`) before
  covers load to make the app decode covers as it would at that scale.
- Discord: `app.DiscordPipe` is a `FakeDiscordPipe` that acts like Discord over the real library. It can `Start`,
  `Quit`, `Reload` and `SendError`, and shows the last presence in `Activity`. The library polls once per second, so
  use `WaitUntil(condition)` and mark the test as slow (see "Slow tests" in `CLAUDE.md`).

## Theme quirks

- Sliders use the app's own template: a fill bar with no `Thumb`. Press at a fraction of the slider's width with
  `app.PointIn(slider, fraction, 0.5)` instead of looking for a thumb.
- Headless tests can't hear audio. Check what the media engine received instead.

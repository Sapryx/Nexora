---
name: animations
description: How UI animations are configured in Nexora (animations.json → settings model → StaticResource keys) and how to add, change or tune one. Use when adding or changing a transition or animation, touching animations.json, AnimationSettings*, AnimationResourceFactory or AnimationResources, or when the user asks to change an animation's duration, easing or target value.
---

# Animations in Nexora

## How it works

All animation durations, easings and target values come from one JSON file and reach XAML as `StaticResource` keys.

1. `src/Nexora/Assets/animations.json` holds the defaults. Like every asset it is an `AvaloniaResource` (`Assets\**` in `Nexora.csproj`), so the values ship inside the build; `App` reads it with `AssetLoader.Open(new Uri("avares://Nexora/Assets/animations.json"))`. Assets never go next to code: `src/Nexora/Animations/` holds only C#. It is the only place defaults live: the settings classes have no initializers.
2. `AnimationSettingsLoader` (`src/Nexora/Animations/`) parses the default JSON. In Release it then deep-merges the optional user overrides from `%APPDATA%/Nexora/animations.json` (`CompositionRoot.AnimationsFilePath`) on top: only keys present there replace defaults, unknown keys are ignored, and invalid JSON or wrong types log a `Warn` and fall back to the defaults. The app never creates the user file, so new defaults from later builds still reach users. Debug builds don't read it at all (`AnimationsFilePath` is `null` under `#if DEBUG`).
3. The settings model is `AnimationSettings` with one class per group (`PopupAnimationSettings`, `TrackAnimationSettings`, …). Every property is `required`, so a key missing from the default JSON fails at startup. It is deserialized with source-generated `AnimationSettingsJsonContext` (snake_case, `RespectNullableAnnotations`, AOT-safe, no reflection).
4. `AnimationResourceFactory.Create(settings, defaults)` turns the settings into a `Dictionary<string, object>` keyed by the resource names the XAML uses. It converts milliseconds to `TimeSpan`, a color string to `ImmutableSolidColorBrush`, a scale to `TransformOperations`, and an easing name through an explicit map of Avalonia's built-in easings. An invalid color or easing logs a `Warn` with the JSON key (`track.hover_color`) and uses the default.
5. `App.Initialize` stores the result in `AnimationResources.Values` (`src/Nexora.Theme/Resources/AnimationResources.cs`) **before** `AvaloniaXamlLoader.Load(this)`. `AnimationResources` is a `ResourceDictionary` that copies `Values` in its constructor and is merged into `NexoraTheme.axaml`.

### Why the values must exist before the XAML loads

- `StaticResource` is resolved while the XAML loads, and `NexoraTheme` (the context menu animation) loads inside `App.Initialize`.
- `DynamicResource` does not work inside `Transitions` / `Animation`, because they are not in the logical tree. So always use `{StaticResource ...}` for animation values.
- Edits to the JSON therefore need a restart. `dotnet run` rebuilds the asset automatically.

## Current mapping

| JSON key | Resource key | Type | Used in |
|---|---|---|---|
| `popup.open_duration` | `PopupOpenDuration` | `TimeSpan` | `TrackEditor.axaml` (open transitions) |
| `popup.close_duration` | `PopupCloseDuration` | `TimeSpan` | `TrackEditor.axaml` (close transitions), `TrackEditor.axaml.cs` (hides the backdrop after it) |
| `popup.initial_scale` | `PopupClosedTransform` | `TransformOperations` | `TrackEditor.axaml`, `Border#Dialog` closed state |
| `context_menu.open_duration` | `FlyoutOpenDuration` | `TimeSpan` | `NexoraTheme.axaml`, `MenuFlyoutPresenter` animation |
| `segmented_control.slide_duration` | `SegmentSlideDuration` | `TimeSpan` | `SegmentedControl.axaml` indicator |
| `track.hover_duration` | `HoverDuration` | `TimeSpan` | `TrackControl.axaml`, `TrackTile.axaml` background |
| `track.hover_color` | `TrackHoverBrush` | `ImmutableSolidColorBrush` | same, `:pointerover` background |
| `track.cover.hover_duration` | `FadeDuration` | `TimeSpan` | `TrackControl.axaml`, `TrackTile.axaml` `Border#HoverEffect` |
| `track.cover.hover_opacity` | `TrackCoverHoverOpacity` | `double` | same, `:pointerover` opacity |
| `default.ease_in` | `DefaultEaseIn` | `Easing` | closing / leaving animations |
| `default.ease_out` | `DefaultEaseOut` | `Easing` | opening / appearing animations |

Easing names are Avalonia class names: `LinearEasing`, `{Sine,Quadratic,Cubic,Quartic,Quintic,Exponential,Circular,Back,Elastic,Bounce}Ease{In,Out,InOut}`.

## Adding or changing an animation

1. **JSON.** Add the value to `src/Nexora/Assets/animations.json` under the group of the UI element (snake_case; create a new group or a nested object if needed). Durations are integer milliseconds, scales and opacities are numbers, colors and easings are strings.
2. **Model.** Add a `required` property to the matching `*AnimationSettings` class (a new group gets its own class in `src/Nexora/Animations/` and a `required` property on `AnimationSettings`). No initializers.
3. **Factory.** Map it in `AnimationResourceFactory.Create` to a PascalCase resource key, converting it to the type the XAML property expects. Reuse `TimeSpan.FromMilliseconds`, `CreateScale`, `CreateBrush` and `CreateEasing`. For a string value that can fail to parse, pass the JSON key and the default from `defaults`, log a `Warn` and fall back.
4. **XAML.** Reference it with `{StaticResource Key}` in the transition (`Duration`, `Easing`) or the setter that holds the target value. Use `DefaultEaseOut` for things that appear and `DefaultEaseIn` for things that leave. Add a new easing setting only when the user asks for a different curve.
5. **Code-behind** that must wait for an animation (as `TrackEditor` waits before hiding the backdrop) reads the duration with `this.FindResource("Key") is TimeSpan duration` instead of hard-coding it.
6. **Tests** in `tests/Nexora.Tests/Animations/`: extend `LoadDefaults_AssetFile_ReturnsAllGroups` and `Create_Defaults_ReturnsDefaultResources` with the new key (tests read the asset copied to their output by a linked `None` item in `Nexora.Tests.csproj`, because `AssetLoader` needs the Avalonia platform); add an `Invalid...LogsWarningAndUsesDefault` case for a new parsed string type.
7. Build, then tell the user what to check in the app: animations can't be covered by unit tests.

## Patterns

- **Different open and close durations or easings.** A transition runs with the `Transitions` of the state being entered. Put the close transition in the base style and the open transition in the active style (`[Tag=True]`, `:pointerover`, …), listed before the value setter. See `Panel#Backdrop` and `Border#Dialog` in `TrackEditor.axaml`.
- **Prefer `Transitions` over `Style.Animations`.** Transitions start from the current value, so an interrupted animation reverses smoothly. Use keyframe `Style.Animations` only for one-shot entrance effects such as the context menu.
- **Animate `Opacity` and `RenderTransform`**, not `Width`/`Height`/`Margin`: they skip layout and stay smooth in long track lists.
- **Durations.** About 80–150 ms for hover and press, 150–250 ms for menus and small popups, 250–400 ms for large panels. Closing about 20–30% faster than opening.
- **Not configurable on purpose.** Natural end points (opacity 0 → 1, `scale(1)`, `ScaleY` 1, the context menu's starting `ScaleY` 0), the `SegmentedControl` indicator position (computed in `SegmentedControl.cs`), theme colors (`Colors.axaml`) and `SmoothScroller` physics. Don't move these to JSON unless asked.
- **No close animation for the context menu.** Avalonia removes the flyout immediately, so `context_menu` has only `open_duration`.

## Rules recap

- Every animation value goes through `animations.json`. No literals for durations, easings or tuned target values in XAML or code.
- Keep the JSON, the settings model and the factory in sync: a JSON key without a `required` property is silently ignored, and a property without a JSON key fails at startup.
- Keep AOT safety: no `Easing.Parse` (reflection) and no reflection-based `JsonSerializer` calls. Always pass `AnimationSettingsJsonContext.Default.AnimationSettings`.

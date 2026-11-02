# Windows-to-Avalonia layout porting

## Source map

| Linux surface | Windows source of truth | Linux target |
| --- | --- | --- |
| Startup | `ScreenToGif/Windows/Other/Startup.xaml` | `ScreenToGif.Linux/StartupWindow.axaml` |
| Editor shell and ribbon | `ScreenToGif/Windows/Editor.xaml` | `ScreenToGif.Linux/MainWindow.axaml` |
| Recorder frame and command bar | `ScreenToGif/Windows/Recorder.xaml` and `Recorder.xaml.cs` | `ScreenToGif.Linux/RecorderWindow.axaml` with `Themes/CaptureShell.axaml` |
| Webcam preview and command bar | `ScreenToGif/Windows/Webcam.xaml` and `Webcam.xaml.cs` | `ScreenToGif.Linux/WebcamWindow.axaml` with the `Window.capture-shell.webcam-shell` block in `Themes/CaptureShell.axaml` |
| Repeated button layout | `ScreenToGif/Controls/ExtendedButton.cs` and `ScreenToGif/Themes/Button.xaml` | A ScreenToGif-specific Avalonia button and `ControlTheme` |
| Window chrome | `ScreenToGif/Controls/ExWindow.cs` | Native Avalonia chrome unless a requested visual mismatch requires a bounded replacement |

The Windows project’s `Themes/` files provide template and state values; screenshots are acceptance evidence, not the only specification.

## Translation order

1. Identify the matching WPF window and its control-template dependencies.
2. Copy the outer window dimensions and top-level grid before porting children.
3. Port groups in their Windows order, retaining the space of unavailable commands as disabled controls.
4. Extract only repeated visual behavior into a compatibility control/theme.
5. Bind each enabled Linux command to its existing behavior; disabled placeholders must not invent behavior.

Port the Windows code-behind's visibility rules along with the XAML. A control Windows collapses in some state is hidden there, not disabled: the Recorder collapses its frame-rate field, indicator, and unit in Manual mode and relabels the unit per mode (`DetectCaptureFrequency` in `Recorder.xaml.cs`). The disabled-with-tooltip placeholder is for a capability Linux lacks, not for a control Windows itself hides.

## Compatibility-control test

Create or extend a custom Avalonia control only when all three are true:

- the WPF source has a reusable custom-control/template contract;
- at least two call sites would otherwise duplicate exact icon/text/alignment markup; and
- keeping it local to each call site risks visible drift.

For ScreenToGif buttons, prefer a `Button` derivative or a templated content control with styled properties for the icon, label, icon bounds, and optional key gesture. Its `ControlTheme` should carry the vertical/horizontal template, disabled opacity, focus, hover, and pressed states. Do not custom-draw an interactive button.

## Fluent defaults that override copied geometry

Copying a WPF height is not enough where a Fluent template carries a larger minimum. `NumericUpDown` and the `ButtonSpinner` inside its template both have minimum heights that outrank an explicit `Height`, so a 24-unit field in a compact bar renders taller than a 24-unit `ComboBox` beside it and reaches the bar's edges. Set `MinHeight="0"` on the `NumericUpDown` and, through `/template/ ButtonSpinner`, on the spinner as well; setting it on the inner `TextBox` alone changes nothing.

Fluent draws a filled background on a disabled `Button` through its template's `PART_ContentPresenter`, which overrides a transparent `Background` set on the button. On a dark tool bar that fill reads as a pressed or checked tool. Clear it with a `:disabled /template/ ContentPresenter#PART_ContentPresenter` style that sets `Background` and `BorderBrush` to transparent, and keep disabled state to opacity.

Fluent's disabled foreground and fill brushes are brighter than the capture shells' dark command bar, so a disabled button rendered heavier than its enabled neighbours and the one control that could not be pressed was the boldest thing in the row. Keep disabled tools flat and fade them with opacity instead of letting the theme's disabled brushes through.

A `NumericUpDown` in the command bar inherited a dark foreground and showed its value dark on dark, so the field looked empty. Set its `Foreground` explicitly, and confirm the value is visible in a capture rather than in the markup.

Fluent's `Expander` paints its header's hover and pressed states from theme resources (`ExpanderHeaderBackgroundPointerOver`, `ExpanderChevronBackgroundPointerOver` and their siblings) that a `/template/ ToggleButton` style does not reach. On a dark panel the hover state drew a black strip across the header. Override those brushes in the `Expander`'s own `Resources`, where the theme looks them up; a style `Foreground` setter on the toggle also outranks the theme's hover foreground, so do not add resources that it will shadow.

Both of the first two were found by sampling pixels in a real render, not by reading markup; see [verification](verification.md). The `Expander` strip only appears while the pointer rests on the header, which is where a screenshot taken straight after clicking it leaves the pointer.

## Capture-shell command bars

`Themes/CaptureShell.axaml` is shared by every capture shell, and the Recorder's command bar is pinned by an ADR. Scope a shell's own restyling to a class on its window, as the Webcam does with `Classes="capture-shell webcam-shell"` and a `Window.capture-shell.webcam-shell` selector block, rather than editing the shared rules. Two branches that each restyled the shared rules for their own shell collided in a rebase, and either resolution would have changed the other shell's bar.

Take a shell's minimum width from what its command bar needs, not from the capture size. The Webcam window sized itself from the camera frame, so a 640x480 camera at the default half scale opened it at 420 pixels and carried Stop off the right edge; its minimum is now the bar's width and the scale clamps to it.

Give a live counter, such as a frame count updated fifteen times a second, a fixed minimum width and tabular figures (`FontFeatures="tnum"`), or it reflows the controls beside it on every update.

## Icon registry

`WindowsIcon` registers its glyphs with indexer initialisers (`["Pause"] = …`), so a duplicate key does not fail the build: the later entry silently replaces the earlier one. When merging branches that each added icons, search the registry for repeated keys; a webcam branch's `Pause` entry once repainted the Recorder's pause glyph this way.

## Known baseline: startup

The Windows source is a 500×220 outer window with a 45-unit header, four equal shortcut columns, and shortcut margins of `5,0,5,5`. On the tested Avalonia/X11 chrome, `Window.Width` and `Height` describe the client area rather than the WPF outer extent: use the calibrated 486×182 client window to reproduce the Windows 45-unit header and 132-unit shortcut-card height. Preserve the source header and card values; translate only the top-level window extent.

The Windows vertical `ExtendedButton` uses a 36×36 icon. Stretch each Avalonia shortcut into its Grid column—Avalonia otherwise measures the button to its content—and use 0.7 disabled opacity so unavailable shortcuts remain visibly positioned. Use a vector Options icon, not a font glyph.

## Known baseline: editor

The Windows editor uses a ribbon, central preview/workspace, horizontal frame strip, and lower status band. Linux now uses the same seven visible ribbon tabs and the shared compatibility controls for the standard button and tab shapes. Read [editor-ribbon.md](editor-ribbon.md) before changing that surface; it records the source values verified in an empty editor render.

Native title bars differ by desktop environment. Do not claim pixel parity for non-client chrome; compare the client area at the same logical window size.

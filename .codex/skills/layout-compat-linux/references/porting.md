# Windows-to-Avalonia layout porting

## Source map

| Linux surface | Windows source of truth | Linux target |
| --- | --- | --- |
| Startup | `ScreenToGif/Windows/Other/Startup.xaml` | `ScreenToGif.Linux/StartupWindow.axaml` |
| Editor shell and ribbon | `ScreenToGif/Windows/Editor.xaml` | `ScreenToGif.Linux/MainWindow.axaml` |
| Repeated button layout | `ScreenToGif/Controls/ExtendedButton.cs` and `ScreenToGif/Themes/Button.xaml` | A ScreenToGif-specific Avalonia button and `ControlTheme` |
| Window chrome | `ScreenToGif/Controls/ExWindow.cs` | Native Avalonia chrome unless a requested visual mismatch requires a bounded replacement |

The Windows project’s `Themes/` files provide template and state values; screenshots are acceptance evidence, not the only specification.

## Translation order

1. Identify the matching WPF window and its control-template dependencies.
2. Copy the outer window dimensions and top-level grid before porting children.
3. Port groups in their Windows order, retaining the space of unavailable commands as disabled controls.
4. Extract only repeated visual behavior into a compatibility control/theme.
5. Bind each enabled Linux command to its existing behavior; disabled placeholders must not invent behavior.

## Compatibility-control test

Create or extend a custom Avalonia control only when all three are true:

- the WPF source has a reusable custom-control/template contract;
- at least two call sites would otherwise duplicate exact icon/text/alignment markup; and
- keeping it local to each call site risks visible drift.

For ScreenToGif buttons, prefer a `Button` derivative or a templated content control with styled properties for the icon, label, icon bounds, and optional key gesture. Its `ControlTheme` should carry the vertical/horizontal template, disabled opacity, focus, hover, and pressed states. Do not custom-draw an interactive button.

## Known baseline: startup

The Windows source is a 500×220 outer window with a 45-unit header, four equal shortcut columns, and shortcut margins of `5,0,5,5`. On the tested Avalonia/X11 chrome, `Window.Width` and `Height` describe the client area rather than the WPF outer extent: use the calibrated 486×182 client window to reproduce the Windows 45-unit header and 132-unit shortcut-card height. Preserve the source header and card values; translate only the top-level window extent.

The Windows vertical `ExtendedButton` uses a 36×36 icon. Stretch each Avalonia shortcut into its Grid column—Avalonia otherwise measures the button to its content—and use 0.7 disabled opacity so unavailable shortcuts remain visibly positioned. Use a vector Options icon, not a font glyph.

## Known baseline: editor

The Windows editor uses a ribbon, central preview/workspace, horizontal frame strip, and lower status band. Linux now uses the same seven visible ribbon tabs and the shared compatibility controls for the standard button and tab shapes. Read [editor-ribbon.md](editor-ribbon.md) before changing that surface; it records the source values verified in an empty editor render.

Native title bars differ by desktop environment. Do not claim pixel parity for non-client chrome; compare the client area at the same logical window size.

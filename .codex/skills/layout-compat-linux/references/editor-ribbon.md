# Editor ribbon baseline

Use this reference only for `ScreenToGif.Linux/MainWindow.axaml` and its shared ribbon controls. It is a source-backed, rendered baseline—not a replacement for the corresponding section of `ScreenToGif/Windows/Editor.xaml`.

## Shared geometry

| Element | Windows value | Linux compatibility surface |
| --- | ---: | --- |
| Ribbon content height | 94 | Ribbon tab content grid |
| Ribbon outer margin | 2 | Tab content grid margin |
| Group label row | 16 | Final group grid row |
| Inter-group column | 7 | Explicit spacer column; separator stays 1 wide with `3,2` margin |
| Primary ribbon button | min width 55; 28×28 icon | `RibbonButton` default template |
| Full-width transport endpoint icon | 38×28 | `RibbonButton IconWidth="38"` |
| Compact stacked action | min width 60; 25×25 icon | `RibbonButton` with `compact` class in two equal rows |
| Editor tab icon | 14×14; `10,2,2,2` margin | `RibbonTabItem` |

Use explicit `Auto,7,Auto` column patterns for groups. Do not simulate separators using cumulative border padding: that changes the command positions as groups vary.

## Reusable controls

- `RibbonButton` is the Avalonia counterpart to the Windows vertical and horizontal `ExtendedButton` templates. The default presentation is a full-height vertical button; the `compact` class is the two-row horizontal action. Keep text bounded to the shared 68-unit maximum so labels wrap rather than expand their column or collide with the next command.
- `RibbonTabItem` is the counterpart to `AwareTabItem`. Give every visible editor tab an `IconKind`; it owns the 14px icon box, header margins, hover treatment, and selected border.
- `WindowsIcon` is the local vector bridge. Reuse or extend it when a missing icon makes controls visually indistinguishable. Do not replace it with Unicode glyphs, which vary by desktop font and baseline.

Every `IconKind` referenced from `MainWindow.axaml` must have a `WindowsIcon` definition. Before shipping an icon pass, compare the two sets (for example, with `rg`); a silent fallback or a reused document glyph is not acceptable for a visible action. Geometries with asymmetric visual weight, especially Play, must be optically centered in the 40-unit canvas rather than merely anchored at coordinate zero.

## Group patterns verified in the empty editor

- **Home:** primary Undo/Paste buttons next to compact Reset/Redo and Copy/Cut pairs; Zoom ends with a compact Fit action; Select ends with compact Inverse/Deselect.
- **Playback:** five primary transport buttons, then a two-row options group.
- **Edit:** Frames end in compact Delete before/after; Reorder begins with compact Reverse/Yoyo, then two primary move controls. Preserve the functional Linux delay field when its existing handlers still require it.
- **Image:** Size is one primary plus a compact pair; Text is two primary buttons plus a compact pair; Overlays reserve seven primary columns plus a compact pair, even while unavailable commands are disabled.
- **Transitions:** two primary commands in one group.

When a Linux-only implementation needs a control absent from Windows (currently the editable Delay field and export actions), retain it only where it preserves an existing working command. Keep its group spacing and disabled behavior consistent with adjacent Windows-derived controls.

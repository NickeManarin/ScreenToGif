# Options layout parity

Use this reference for the Options shell or any of its settings pages.

## Source map

| Surface | Windows source | Linux target or evidence |
| --- | --- | --- |
| Options shell and navigation | `ScreenToGif/Windows/Options.xaml` | `ScreenToGif.Linux/OptionsWindow.axaml` |
| Page content | `ScreenToGif/Views/Settings/*Settings.xaml` | Named page controls in `OptionsWindow.axaml` |
| Rendered page references | Supplied Windows captures | `.codex/skills/options-<page>.png` |

The captures cover Application, Recorder, Editor, Tasks, Shortcuts, Language, Storage, Cloud, Extras, Donate, and About. Match each page against its own capture; shared shell alignment does not prove page-level parity.

The WPF shell declares `Width="800"`, `Height="610"`, `MinWidth="780"`, and `MinHeight="460"`. Screenshot pixel dimensions include capture and non-client differences, so use the WPF logical values to establish the shell and compare the client area. Preserve the left navigation, page content region, and bottom confirmation band as separate grid regions.

## Settings groups

WPF settings pages use compact section headers with a separator line. Avalonia's stock `Expander` header is materially taller, so use one Options-local `ControlTheme` for the repeated 24-unit header/content shape and verify both expanded content and collapse interaction in a real render. In the tested Avalonia version, layering a second custom `ToggleButton` theme inside that template disturbed measure/render behavior; add nested control themes only with rendered evidence.

Class styles on the header `ToggleButton` work where a nested theme did not. Three traps need them. The toggle is checked whenever the group is expanded, so it picks up Fluent's accent fill and every expanded header reads as a pressed button; clear `Background` for `:checked`, `:pointerover`, and `:pressed` through `/template/ ContentPresenter#PART_ContentPresenter`. The toggle measures to its content unless stretched, which collapses the starred column that draws the header rule, so set `HorizontalAlignment="Stretch"`. Stretching it also stretches any border it carries, which then draws a second hairline under the rule, so give it no border. A static chevron does not say whether a group is open; rotate it with a `:not(:checked)` style rather than recolouring the header.

## Rows that depend on another setting

Avalonia `TextBlock`s have no disabled appearance, so disabling only the input in a label, field, and hint row leaves the label and hint at full strength beside a dead field. Put the row in one container, disable the container, and dim it with a `:disabled` opacity style. Give it a tooltip with `ToolTip.ShowOnDisabled="True"` naming the setting it waits on, as the out-of-scope rows name their missing subsystem. Where Windows binds a dependency such as `IsEnabled="{Binding RecorderRememberSize}"` with `UncheckOnDisable`, mirror both halves, and re-evaluate on the parent's own change event: a rule refreshed only by unrelated handlers leaves the dependent row stuck in whichever state the last unrelated toggle computed.

The shared `CheckBox` style fixes its height at 28 units, so its content never wraps. A hint appended to the label text is clipped at the default 800-unit shell with no ellipsis; keep hints short or put them in a separate muted, wrapping `TextBlock` as the delay and countdown rows do.

Keep unavailable features in their Windows positions with disabled affordances and tooltips. The Donate page content exists for parity evidence, but its navigation item is deliberately disabled and must remain inaccessible.

## Initialization and navigation

`SelectedIndex="0"` on the navigation `ListBox` can raise `SelectionChanged` during `InitializeComponent`, before later named page fields have been assigned. Gate page switching until initialization and settings loading complete, or attach the handler afterward. A successful compile does not detect this failure; launch the window and visit every enabled page.

When headless interaction appears to miss a navigation item, follow the focus guidance in [verification](verification.md) before changing hit targets or selection logic.

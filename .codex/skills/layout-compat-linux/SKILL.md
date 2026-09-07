---
name: layout-compat-linux
description: Port ScreenToGif Windows layouts to Avalonia on Linux with matching geometry, alignment, and disabled-state affordances. Use when adding or restyling Linux UI surfaces for Windows visual parity; not for feature-porting alone.
---

# Layout compatibility for Linux

Make each Linux surface read like its Windows counterpart in structure, button placement, sizing, alignment, hierarchy, and disabled states. Keep the established Linux palette unless the task explicitly changes it.

## Start from the Windows source

Treat the matching WPF XAML and its control templates as the geometry source of truth. Copy scalar layout values—row and column definitions, widths, heights, margins, padding, icon bounds, font sizes, borders, and group separators—directly into Avalonia unless a documented platform or framework difference prevents it.

Read [the porting guide](references/porting.md) before changing a surface. It maps the source of truth, defines the compatibility-control boundary, and records the editor baseline.

When changing the editor ribbon, also read [the editor ribbon reference](references/editor-ribbon.md). It records the proven shared metrics and when to use each compatibility control.

## Keep compatibility narrow

Use stock Avalonia controls where their layout and state behavior can be styled to match. When a WPF custom control is the repeated source of alignment, create one ScreenToGif-specific Avalonia counterpart with a `ControlTheme`; do not repeat hand-built icon/text stacks at each call site.

The first candidates are `ExtendedButton` equivalents and the ribbon/tab surface. Their templates own icon size, icon-to-text spacing, padding, wrapping, borders, and disabled states. A new subclass must exist to remove repeated drift, not merely to mirror a WPF class name.

Preserve feature position even when its behavior is unavailable: render it disabled with the same geometry and grouping, then make its unavailable state clear through the existing tooltip convention.

## Verify rendered geometry

Source parity is insufficient. Capture the Linux surface at the matching window size, compare it with the supplied Windows screenshot or its matching WPF layout, and correct the first visible positional mismatch before cosmetic polish.

For capture setup, known environment limitations, and the required comparison checks, read [verification](references/verification.md). Use the capture helper there only when a virtual X display is available.

## Scope discipline

Port the requested surface before creating placeholder windows or implementing Windows-only behavior. Do not let a visual-parity pass become a feature port; only introduce an enabled command when its Linux behavior is already implemented and tested.

# Discover and map Windows behavior

Use this reference when beginning parity work for a component whose Linux behavior is not yet established. The Windows implementation is evidence of the user contract, not a design to transliterate into Avalonia.

## Trace the complete Windows path

Start from the visible control in its owning Windows XAML, then follow the command rather than searching only for its label. For editor controls the main route begins in `ScreenToGif/Windows/Editor.xaml`:

1. Resolve its routed-command resource and matching `CommandBindings` entry, or follow its data binding through the view's `DataContext`. The route must identify both availability (`CanExecute` or equivalent) and execution behavior.
2. Read both paths in the owning code-behind or view model. Availability rules reveal empty, selection, loading, and boundary states that the happy path does not. Editor routed-command handlers are in `ScreenToGif/Windows/Editor.xaml.cs`.
3. If the handler calls `ShowPanel`, follow its `PanelTypes` case, instantiated user control, settings or view model, and apply callback. Defaults and validation are often split across those locations.
4. Follow the apply callback into the domain helper and every state effect around it: playback pause, history capture, frame/file changes, selection restoration, preview reload, persistence, statistics, status, and cleanup.
5. Search the command name, handler name, panel type, settings properties, and localization keys across the repository. These expose alternate entry points and error or no-op behavior that the primary handler alone can hide.

Write the resulting contract before choosing Linux APIs: valid scope and selection, parameters and defaults, output, preserved frame properties, history unit, observable refreshes, cancellation boundary, failure behavior, owned artifacts, and platform-only dependencies. Resolve contradictions in favor of the visible user behavior and current project-format invariants, not incidental WPF sequencing.

## Choose the equivalent by responsibility

| Windows responsibility | Linux mapping |
|---|---|
| WPF control, routed command, panel, or dispatcher wiring | Avalonia control/handler as a thin adapter around the same availability and result contract |
| Frame, sequence, timing, history, archive, or statistics logic | Platform-neutral immutable operation or injected service, called by the window adapter |
| Bitmap mutation or media probing/encoding | The existing normalized-PNG and `IFfmpegTool` boundaries; preserve pixels, timing, and atomic adoption rather than WPF imaging calls |
| Dispatcher timer or UI-clock progression | Pure state machine driven by an injected monotonic clock, with the Avalonia timer only scheduling its result |
| Registry, shell association, or Windows launcher behavior | The corresponding freedesktop/XDG/GIO contract, verified through the real Linux parser or launcher as well as static validation |
| Clipboard or temporary files whose Windows lifetime is implicit | An explicitly owned workspace whose copies survive project replacement and history replay |
| Adorner, capture metadata, or authoring model with no Linux foundation | Keep the command unavailable and name the missing subsystem in that control's tooltip; a partial destructive imitation is not parity |

Prefer an existing Linux service or coordinator when its contract matches. If several controls need the same bookkeeping, extract one testable service/state machine and keep Avalonia-specific selection, dialogs, and visual updates at the edge. Do not share a helper merely because the Windows code did; share the responsibility that must remain consistent.

## Verify the behavioral contract

- Test pure calculations and state transitions without Avalonia controls. Cover empty, single, boundary, discontiguous, invalid, cancellation, and later-step failure shapes implied by the traced contract.
- Test filesystem and FFmpeg seams with injected dependencies and disposable workspaces. Assert exact pixels, ordering, delays, paths, cleanup, and one-step Undo/Redo where applicable; file existence or a changed digest alone is weak parity evidence.
- Exercise the real Avalonia control in a launched editor. Confirm availability, parameter collection, preview/timeline/status refresh, selection continuity, dynamic tooltip or glyph state, and recovery after failure.
- Recheck every alternate surface that invokes or reports the same state. Ribbon and status playback controls, timeline and preview focus, dirty state and discard prompts, and Statistics are common places where a locally correct port becomes globally contradictory.

### Verify X11 input shapes across the native hierarchy

An Avalonia X11 platform handle may not be the only native window receiving input. Avalonia can place a full-size child beneath that client, and a window manager such as GNOME Shell can reparent the client into its own frame. X Shape input regions from descendants are unioned into their ancestors' effective input region, so shaping only the advertised client XID can look correct when queried while a child or frame still consumes the viewport click. A bare Xvfb session can miss the window-manager layer entirely.

For click-through behavior, inspect the client, its native descendants, and every ancestor below the root on the real target session. Shape descendants to the interactive client intersection and preserve the frame's title bar, resize borders, and interactive client regions. Reapply after move, resize, activation, and initial layout because toolkit or window-manager configure work can replace a shape or resize a child after the first callback. Use [the X11 input-shape inspector](../scripts/query-x11-input-shape.py) with a window ID to print geometry, client/root-relative positions, and server-reported input rectangles for the client, all descendants, and every ancestor below the root.

Bank new guidance only when implementation or verification exposes a non-obvious decision or failure mode that future runs would otherwise have to rediscover. Put component-specific findings in that component's reference; keep this workflow limited to discovery and mapping patterns that transfer across components.

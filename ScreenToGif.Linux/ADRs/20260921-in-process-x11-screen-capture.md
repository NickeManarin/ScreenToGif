# ADR: In-process X11 screen capture for the Linux Recorder

## Status
Accepted

## Area
Recorder

## Context
The Linux Recorder window is a scaffold: its controls are disabled and point at a "Linux desktop-capture subsystem" that does not exist. Making the Recorder functional requires choosing how frames get off the screen and into the Editor's normalized-PNG project model (`EditorFrame` = PNG path + delay, wrapped in a `LoadedProject`).

The Windows Recorder's user contract, traced from `ScreenToGif/Windows/Recorder.xaml.cs` and `Controls/BaseScreenRecorder.cs`, is: a Stopwatch-paced capture loop that records each frame's real elapsed delay (unless the fixed-frame-rate setting is on), instant Manual snaps, Pause/Resume without re-creating the project, the cursor composited into the frame when enabled, and frames written as PNG by a background consumer.

Constraints in play: the Linux editor performs all imaging through the `IFfmpegTool` process boundary and has no in-process PNG encoder; the Linux app already carries an X11 P/Invoke precedent (`Services/X11WindowInputRegion.cs`); the target session for this increment is X11 (the Recorder frame's click-through already depends on the X Shape extension). Wayland exposes screen content only through the xdg-desktop-portal ScreenCast/PipeWire path, which neither X11 approach can reach.

Measurements taken during the decision (GNOME on X11, 5120x1440 desktop):

| Probe | Result |
|-------|--------|
| `ffmpeg -f x11grab`, single frame 320x200 | 0.74 s wall (process startup dominates) |
| `ffmpeg -f x11grab`, 8 frames at 15 fps | 1.43 s wall |
| `XGetImage` 502x203 (default region) | 0.9 ms per frame |
| `XGetImage` 1280x720 | 4.8 ms per frame |
| `XGetImage` 1920x1080 | 14.7 ms per frame |
| `XFixesGetCursorImage` | 0.04 ms |

## Decision
Implement capture in-process against X11: grab the region with `XGetImage` (ZPixmap) from the root window via P/Invoke to libX11, fetch and composite the cursor with `XFixesGetCursorImage` when the show-cursor setting is on, pace the loop and record per-frame delays with an injected monotonic clock in a platform-neutral state machine, and encode frames to PNG in a background writer using Avalonia's `Bitmap.Save` into an `EditorWorkspace` batch. The Avalonia window stays a thin adapter over that state machine.

This was chosen because it is the only option that preserves the Windows timing contract (real elapsed delays, instant Manual snaps, Pause/Resume without restart) at a per-frame cost that leaves headroom at 15 fps for 1080p regions, and because it yields a pure, clock-driven core that can be unit-tested without a display.

Capture stays X11-only in this increment. Under a Wayland session (no `XID` platform handle) the Record control remains disabled with a tooltip naming the missing portal-based subsystem. Adding a Wayland backend is a separate future decision, not a reversal of this one.

## Alternatives Considered
- **FFmpeg `x11grab` subprocess writing PNGs**: spawn `ffmpeg -f x11grab` per recording and let it write numbered PNGs. — Process startup makes Manual snaps take ~0.7 s each; a running grab cannot pause, resume, or retarget the region without a restart; constant-rate output replaces real elapsed delays with a fixed cadence, changing the Windows semantics; timing is opaque to tests.
- **In-process X11 grab piped as raw video into one FFmpeg process for PNG encoding**: keeps the "all imaging via FFmpeg" invariant. — Adds a long-lived pipe with back-pressure and lifecycle failure modes, and must keep a parallel delay list to re-associate timings with numbered outputs, for no pixel-fidelity gain over Avalonia's PNG encoder. Capture is a source, not a frame mutation, so the invariant's purpose (preserving pixels and timing through edits) is not at stake.
- **xdg-desktop-portal ScreenCast over PipeWire**: works on Wayland and X11. — Requires D-Bus session negotiation, a user consent dialog per recording, PipeWire stream consumption for which .NET has no first-class library, and the portal chooses monitor or window rather than the Recorder's own frame region, so frames would need cropping from a full-monitor stream. Far larger than the increment and does not remove the need for an X11 path on sessions without a portal.
- **Leave the Recorder as a scaffold**: no new subsystem. — The Recorder is the product's core capture surface; the Linux app without it is an editor only.

## Consequences

### Positive
- Windows timing semantics carry over: real per-frame delays, instant snaps, pause and resume on the same project.
- The capture core is a state machine with injected clock and screen source, testable without X11 or Avalonia.
- Reuses the existing X11 interop pattern and the existing workspace and `LoadedProject` hand-off, so the Editor needs only an entry point that accepts a project.
- No new runtime dependency beyond libX11 and libXfixes, both already required by Avalonia's X11 backend.

### Negative
- The Linux app gains an in-process PNG encoding path alongside the FFmpeg-only imaging in the editor; two encoders now exist in the codebase.
- Native Wayland sessions get no recording until a portal-based backend is built; the disabled state must be detected and explained honestly.
- `XGetImage` copies pixels through the X connection on every frame; very large regions at high fps will lag and record longer delays, as Windows does. A shared-memory (`XShm`) optimization is a later, separable step.
- Cursor compositing, pixel-format handling (depth 24 vs 32), and root-coordinate mapping under fractional scaling are new code the app must own.

## Source
- Session: figure-out log `~/.manifest-dev/logs/figure-out-log-20260921-170934.md` (2026-09-21)
- Related: none yet

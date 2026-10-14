# Recorder follow-ups

Capabilities the Windows recorder has that the Linux Recorder does not, each with what it would take. Written for a developer new to this port; the Recorder itself is described in [`recorder.md`](recorder.md), and the decision behind its capture path in [`../ADRs/20260921-in-process-x11-screen-capture.md`](../ADRs/20260921-in-process-x11-screen-capture.md).

A few terms recur. A **compositor** is the program that combines every window into the image on screen; on X11 it is optional, and the Recorder's see-through frame depends on one being present. **Global input hooks** are a way to observe pointer and keyboard events across the whole desktop rather than only inside your own windows; X11 offers this through the XInput2 extension, and Wayland deliberately does not offer it at all. **D-Bus** is the message bus desktop services talk over, and **PipeWire** is the media-routing daemon a Wayland desktop hands video streams through. **XWayland** is the X server a Wayland desktop runs so X11 applications keep working. An **XID** is the numeric identifier the X server gives a window, and the Recorder uses the presence of an XID to tell an X11 window from a Wayland one.

## Recording under Wayland

**Windows does:** records the screen regardless of which display server is in use, because there is only one.

**Missing on Linux:** a capture backend built on the `org.freedesktop.portal.Desktop` ScreenCast interface over D-Bus, feeding a PipeWire stream. A native Wayland session exposes no way for a client to read the screen, by design. X11 clients under XWayland can read other XWayland windows but cannot read native Wayland ones.

**Already established:** the ADR's Alternatives section evaluated the portal path and rejected it for this increment, not permanently. Its costs are recorded there: D-Bus session negotiation, a consent dialog for every recording, no first-class PipeWire library for .NET, and a portal that hands back a whole monitor or window rather than the Recorder's own rectangle, so frames would need cropping. Adding it is a new decision rather than a reversal of that one.

**Detected by:** `X11ScreenSource.IsSupportedSession`, which reads the window's platform handle descriptor and treats anything other than `XID` as not recordable. This predicate is unit-tested, but the disabled state it produces has never been exercised on a real Wayland session, and that is the one thing about this entry a reader should not assume. The session this port was built in reported a type of `x11` throughout, and no Wayland compositor was installed to switch to. Whether the port behaves under Wayland is an open question rather than a tested one.

**Corresponds to:** Record and Snap, disabled with a tooltip naming the portal backend.

## User-interaction mode, and desktop-wide hotkeys

**Windows does:** captures one frame on each mouse click or key press, and binds the recorder's start, stop, and discard keys globally, so they work while another application has focus.

**Missing on Linux:** global input hooks. Both features need the same thing, which is why they are one entry. On X11 this means XInput2 raw event selection on the root window, or an `XGrabKey` for each hotkey; under Wayland neither exists, and a desktop shortcut would have to be registered with the desktop environment itself.

**Already established:** the Recorder's shortcuts are deliberately window-scoped for this increment, fixed at `F7`, `F8`, and `F9`, and the Options rows that would rebind them are disabled rather than accepting a binding that would not take effect. That scoping was a decision rather than an omission. Desktop-wide keys need the same global input hooks this entry's **Missing on Linux** paragraph names, and rebinding needs a shortcut editor this port does not have. Both rows were disabled instead of being left to look as though they worked.

**Corresponds to:** the **User interaction** capture-frequency radio, and every row of the Shortcuts page's **Global** group, plus the recorder key fields, all disabled with tooltips.

## Snap to window

**Windows does:** resizes the recorder frame to match a window the user picks.

**Missing on Linux:** a window picker. The geometry half is already solved. `Services/X11WindowInputRegion.cs` walks the X11 window tree with `XQueryTree`, reads geometry with `XGetGeometry`, and translates coordinates with `XTranslateCoordinates`, which is everything needed to find a window's rectangle. What is missing is the interaction: letting the user point at a window, which needs a pointer grab across the desktop and therefore the same global input hooks as User-interaction mode and desktop-wide hotkeys.

**Already established:** this run found that the recorder's own rectangle cannot be derived from its window's position, which is the half of the problem a picker would inherit. Taking `Window.Position` and adding the client size gives the wrong origin, because a window manager draws decorations the position excludes, so every frame would be offset by the height of a title bar. The region is instead read from the viewport control with `PointToScreen`, and `Services/Capture/RecorderRegion.cs` explains that mapping where it is performed. A picker therefore has to work the mapping backwards, moving the window so that the viewport's interior, not the window, lands on the chosen target.

**Corresponds to:** the crosshair button in the command bar, disabled with a tooltip naming the window picker.

## Cursor following

**Windows does:** moves the recorder frame to keep the pointer inside it while recording.

**Missing on Linux:** continuous pointer tracking outside the application's own windows, which is global input hooks again. Reading the pointer's position on demand is possible through `XQueryPointer`, but polling it every frame to drive window movement is a different proposition from being told when it moves.

**Already established:** the pointer itself is already read every frame when the show-cursor setting is on. `X11ScreenSource` fetches it with `XFixesGetCursorImage` and blends it into the frame at its hotspot, which is a separate problem from following it. The ADR's decision section covers that compositing, and the interop details it cost to get right are written at each site in `Services/Capture/X11ScreenSource.cs`: the cursor pixel layout, the hotspot-relative origin, and the clipping against the frame's edges.

**Corresponds to:** **Enable cursor following** on the Options Recorder page, and the two cursor-following shortcut rows, all disabled with tooltips.

## Guidelines

**Windows does:** draws rule-of-thirds and crosshair guides inside the recorder frame to help compose the shot.

**Missing on Linux:** an overlay drawn inside the frame. This is the smallest item here and needs no new subsystem: the viewport is an Avalonia control and can draw lines. The obstacle is that the frame is see-through and click-through, so a guide has to be drawn without making the region opaque and without capturing pointer input. The input-shape machinery that already makes the frame click-through is in `Services/X11WindowInputRegion.cs`.

**Already established:** this run settled where the captured rectangle comes from, which is exactly the rectangle a guide would have to line up with. It is the viewport control's own interior, mapped to the screen with `PointToScreen`, rather than anything derived from the window frame, and `Services/Capture/RecorderRegion.cs` explains why. A guide drawn inside the viewport is therefore inside the recording by construction, which is the problem the following **Watch for** note describes rather than a coincidence.

**Watch for:** the guides must not appear in the recording. The Recorder grabs the composited screen with `XGetImage`, so anything it draws inside its own frame would be captured along with what is behind it.

**Corresponds to:** the **Guidelines** group on the Options Recorder page, which explains why there is nothing to set instead of offering a row.

## Shared-memory capture

**Windows does:** not applicable. This is a Linux-specific optimization of the path this port already has.

**Missing on Linux:** the MIT-SHM extension, `XShmGetImage`, which puts frames in a shared memory segment instead of copying every pixel through the X connection.

**Already established:** the ADR measured the current `XGetImage` path on this machine: 0.9 ms for the default 502 by 203 region, 4.8 ms at 1280 by 720, and 14.7 ms at 1920 by 1080. At 15 frames per second a 1080p region therefore spends about a fifth of each interval in the grab, which leaves headroom but not much. The ADR names this as a later, separable step and its Consequences section records the cost as accepted rather than overlooked.

**Corresponds to:** no control. Nothing in the interface offers or withholds it; a recording of a large region at a high frame rate records longer delays, which is what the Windows recorder does under load too. Encoding is the separate limit. It runs off the capture loop, so a frame rate it cannot keep up with builds a backlog rather than slowing the grab. The recording pauses itself and says so after that backlog passes its budget.

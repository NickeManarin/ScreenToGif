# Rendering verification

## Acceptance checks

At the matching logical window size, compare the Linux client area against the Windows reference in this order:

1. Outer grid bands and group boundaries align.
2. Command positions, button bounds, and icon boxes align.
3. Icon/text baselines, wrapping, and label spacing align.
4. Disabled commands still occupy their Windows positions and read as unavailable.
5. Focus, hover, pressed, and disabled states do not shift surrounding layout.

Check both an empty editor and an editor with media, because the preview and frame strip change state.

## Headless capture

Run `scripts/capture-window.sh` with the already-built executable. The helper isolates `XDG_CONFIG_HOME` in a temporary directory and captures the named window rather than editing project state.

The helper captures the X root and crops it to the target geometry. Keep that path: on the tested Xvfb/ImageMagick combination, `import -window <id>` sometimes returned a black image even though the same window was visible in a root capture.

When driving the window with `xdotool` under Xvfb without a window manager, the first click may only focus the window and `_NET_ACTIVE_WINDOW` may be unsupported. Confirm the selected state after a second click or select a different item before the target; do not treat one ignored synthetic click as application evidence.

If Xvfb fails to expose a display, treat that as a runner limitation, not application evidence. Verify build and source geometry locally, then leave rendered comparison as an explicit acceptance gate for an environment with a working X/Wayland display.

## Measuring a render

Judge heights, alignment, and rule counts by sampling pixels, not by eye. Scan a fixed pixel column through the capture for a known border or rule colour: the rows where it appears give each control's top and bottom and the number of rules under a header, and two controls meant to share a height either share those rows or do not. Crop small regions and scale them up two to four times before viewing, since a one-pixel difference is invisible at 1x.

`xdotool getwindowgeometry` and `import -window` include the compositor's shadow margin on the real session, so a window's client area sits inset inside the capture. Locate the command bar or header by content rather than by a fixed offset from the capture's edge.

## Real session

Some checks need the user's real session: under a bare Xvfb there is no compositor, so transparency renders opaque black, and focus and stacking behave differently. Before driving it, confirm the session is unlocked, kill only processes you started, and guard every click and key press against landing in another application's window. The functionality skill's recorder reference has the specifics.

## Comparison boundaries

Windows’ custom `ExWindow` title bar depends on Windows APIs and cannot be copied directly. Compare the client area first; document any native-window-frame difference separately. Do not use that difference to excuse client-area layout drift.

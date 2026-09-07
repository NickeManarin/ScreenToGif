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

When a real display already has matching windows open, normalize `xdotool search` output to space-delimited IDs before comparing it with the pre-launch set. `xdotool` emits one ID per line; treating that string as space-delimited can accidentally recapture an older window instead of the temporary verification instance.

This environment previously failed to expose Xvfb (`XOpenDisplay failed` both for Avalonia and capture utilities). Treat that as a runner limitation, not application evidence. When capture is unavailable, verify build and source geometry locally, then leave rendered comparison as an explicit acceptance gate for an environment with a working X/Wayland display.

## Comparison boundaries

Windows’ custom `ExWindow` title bar depends on Windows APIs and cannot be copied directly. Compare the client area first; document any native-window-frame difference separately. Do not use that difference to excuse client-area layout drift.

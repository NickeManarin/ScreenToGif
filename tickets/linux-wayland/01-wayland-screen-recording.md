Kind: shaped
Status: open
Depends on: none
Claimed by:
Auto: yes — granted by the user, explicitly in this conversation on 2026-10-22
Type: feature

# Record the Wayland desktop through the screen-sharing portal

## Why

ScreenToGif's Linux application was developed on Ubuntu 24.04 with X11. Its user now runs Ubuntu 26.04 with Wayland and needs to record the desktop into the existing editor. The current capture path reads X11 pixels. An X11 window handle under XWayland does not establish access to native Wayland windows, so the current support check can offer recording that cannot capture the complete desktop.

The outcome is a working Wayland screen-recording workflow: choose a capture source, record it, pause or stop, edit the result, and export it.

## Scope

Add Wayland screen capture through the desktop ScreenCast portal and PipeWire, integrated with the existing Recorder, recording session, project ownership, and editor handoff. Support the existing recording controls where the portal permits their behavior. Preserve X11 recording. User steering also requires an administrator-protected setting to reuse a once-approved source; investigate supported first-use automation and retain normal first-use consent if no supported policy exists.

The user wants display/layout compatibility and consistent return from Editor to StartUp addressed first. This is preferred execution order, not a technical dependency. This ticket covers the subsequent Wayland recording work; it does not include another general UI redesign, a native Wayland UI-backend migration, new editing features, global desktop shortcuts, or unrelated deferred recorder features.

## Rules that must hold

- Use the desktop's consent flow. Permission denial, cancellation, and stream termination must produce a clear state without crashing or losing already recorded frames.
- Detect actual capture capability. An X11 window handle alone must not classify a Wayland desktop as fully recordable.
- Keep captured pixels, cursor placement, region mapping, scaling, and frame timing consistent with what the user chose. Expose portal-imposed selection limits honestly instead of promising unsupported window positioning or capture regions.
- Preserve existing pause/resume, discard confirmation, frame/resource limits, asynchronous recording behavior, settings ownership, and stop-to-editor behavior.
- Keep unsaved editor work safe when adopting recordings or closing windows. Explicit application Exit must still exit, and successful Editor close must follow the application’s established return-to-StartUp behavior.
- Keep existing X11 recording and media import/edit/export workflows working.
- Rebase when integration is needed; never merge.
- Report final behavior, validation evidence, and limits, without action-history bookkeeping.

## Watch out for

The current UI may run through XWayland even when the desktop session is Wayland. UI rendering and desktop capture are separate capabilities. Reading the XWayland root cannot prove capture of native Wayland content.

Portal source selection is an asynchronous user interaction. Test denial, cancellation, compositor/portal failure, and source disappearance as well as the happy path. Unattended execution is authorized by the user. Portal consent may still need a person on the live desktop. If consent or desktop access requires that person, preserve the work and report the blocker; the grant does not bypass the desktop consent flow.

Use the stream's real pixel dimensions and timestamps. Fractional scaling, cursor metadata/composition, and monitor geometry can differ from the application's logical coordinates. A working Xvfb test is not evidence of native Wayland capture.

## Suggested approach

Keep the existing recording-session and editor handoff contracts. Select an appropriate capture source behind the existing capture boundary; integrate portal session lifetime and PipeWire stream lifetime with cancellation and cleanup. Prefer existing supported platform libraries over ad hoc capture commands. Treat source selection and capture-region behavior as portal-constrained interactions, while keeping the user's existing workflow recognizable.

## Definition of done

- On Ubuntu 26.04 Wayland, a real Recorder instance requests a source through the desktop portal and records changing content from a native Wayland application. Recorded frames must show the selected content, rather than a blank image or only XWayland windows.
- The user can record, pause, resume, stop, and discard using the existing supported controls. Stopping hands the recording to Editor with correct dimensions, frame order, and measured delays. The user can play, edit, and export it; an independent decoder confirms the exported content and timing match the recorded project within the chosen format's documented precision.
- Tests and retained live evidence cover consent acceptance, denial and cancellation; source disappearance and stream failure; restarting after a canceled or completed session; and cleanup after closing Recorder or exiting the application. Failure preserves captured work and leaves a usable recovery path, with no test-owned portal sessions, streams, or subprocesses left running.
- Demonstrate correct selected-region/source mapping and cursor behavior at the desktop's current scale and an available fractional scale. If a configuration cannot be exercised, identify the missing evidence and do not claim it is supported based only on source inspection.
- Verify X11 recording in an actual X11 test session and rerun the existing Linux build and tests with no failures. Add meaningful regression tests for capability detection and portal/stream lifecycle, using isolated configuration and media. Required native Wayland behavior cannot pass through mocks or skipped tests alone.
- Update user-facing requirements, supported controls, consent behavior, known portal limitations, and the existing recorder follow-up documentation so they describe the shipped behavior accurately.
- Recorder Options exposes administrator-protected reuse of a once-approved source. Real administrator authentication controls a protected per-UID grant; user settings cannot enable it, cancellation preserves it, and GUI/capture remain unprivileged. Private single-use tokens rotate, unsupported restoration falls back to selection, and Forget forces selection for the next recording. Verify real authentication cancellation/enable/disable and a second native capture without another picker. Package and test the fixed helper, permissions and purge. Investigate supported first-use automation; retain normal first-use consent if no supported policy exists, as the user directed.

## Implementation and delivery

Wayland recording uses a dedicated ScreenCast portal session and the authorized PipeWire remote. Modifier-aware GPU import and explicit BGRA download preserve fractional-scale source geometry before bounded frame observation. The existing recording session owns pacing, pause, resource limits, frame storage and Editor handoff. Wayland dimensions are read-only and do not overwrite remembered X11 geometry. PNG input timestamps preserve millisecond export delays.

Optional source reuse uses a fixed root helper and protected per-UID grant, polkit administrator authentication, private mode-0600 single-use rotating tokens and public portal identity/version negotiation. The GUI and capture remain unprivileged. Options exposes next-recording semantics and Forget; Debian packaging supplies the helper/policy and removes administrator grants on purge. Normal first-use source approval remains required: no supported GNOME/portal administrator policy supplying that first source was found.

Delivery is directly to `linux-support`, as requested by the user. A pull request and published media are not required. The ticket remains open for the live verification listed below.

## Validation evidence

- Linux build: zero warnings/errors. Full suite: 457 passed, 7 optional live/hardware skips, zero failures (`/tmp/stage2-final-root-gpu.log`). Required native behavior does not pass through those skips.
- Actual Ubuntu 26.04 / GNOME 50.1 at temporary 125% compositor scaling: 37 changing frames from the selected native GTK test source at 835 × 533 pixels, including the complete bottom-right marker and embedded cursor in the expected target. A 1002 ms pause is excluded. Actual Editor playback, Reverse, independently decoded APNG pixel order/millisecond delays, Editor close to one StartUp and Exit pass. Original 100% display scaling is restored. Evidence: `/tmp/stage2-native-fractional-gpu-bgra/`.
- At 100%, the earlier Manual/hidden-cursor run passes three explicit snapshots at 125 ms each, no autonomous frames, cursor exclusion with the pointer inside the source, source disappearance, retained frames and playback/edit/export/return/Exit. This predates the GPU transport refinement and does not establish fresh GPU Manual/failure behavior. Evidence: `/tmp/stage2-native-manual-hidden-3/`.
- Controlled portal/native tests cover preparation ordering, decline/cancel/retry, late success/error and FD/session cleanup; crop, all eight orientations, stride/channel conversion, timestamps and bounded immutable buffers. Persistence tests cover registration before portal calls, unavailable registry fallback, version negotiation, replacement tokens, concurrent single-use consumption, 0600 permissions and UTF-8 size bounds. Deliberately late registration fails its order assertion (`/tmp/stage2-registration-order-negative/`); controlled GL conversion passes independently specified pixels.
- Fresh actual isolated X11 changing-source recording, cursor/region, pause/resume, Stop-to-Editor and independently decoded APNG pass. Desktop, controlled webcam and simulated 1.25-scale Board/Editor/Options checks pass (`/tmp/stage2-private-x11-root-gpu/`). The reuse Options panel scrolls fully into view without clipping at normal/minimum size (`/tmp/stage2-private-root-options-final/`). Disabling wrapping in a disposable copy fails its layout assertion (`/tmp/stage2-options-negative-ui/`). Simulated UI scaling is separate from native compositor capture.
- Updated DEB acceptance passes on Ubuntu 22.04, Ubuntu 24.04 and Debian 12: bare installation, root/non-root helper behavior, permissions, library resolution, help, Xvfb startup, MP4/WebM, lintian and grant/policy purge (`/tmp/stage2-gpu-reuse-deb-build.log`, `/tmp/stage2-gpu-reuse-deb-test.log`). These are package/X11 checks, not native Wayland support claims for those distributions. Existing lintian warnings concern manual page, changelog compression and bug closure; no errors.

## Remaining verification and limits

Real administrator authentication cancellation/enable/disable, approved-source restoration without another picker and Forget-to-picker remain unverified. The desktop was locked during host helper installation; the pending authentication was canceled without installing the helper/policy or enabling a grant.

Fresh GPU Manual/hidden-cursor/source-loss coverage, native cancel/close/retry/discard/active Exit and actual fractional-desktop Board/Options checks still need an unlocked desktop and normal user consent. The complete native lifecycle and visual review remain open. Evidence uses test-owned content and stays local; restore tokens must never appear in logs or media.

Native recording requires GStreamer 1.24+, a DMA_DRM-capable PipeWire plugin (tested with 1.6.2), OpenGL and a compatible DMA-BUF graphics driver. There is no known-bad CPU fallback. Other compositors/configurations remain unverified. Full-source selection is intentional; arbitrary overlay cropping, global shortcuts and native Wayland UI rendering remain outside this ticket. Existing X11 recording remains available.

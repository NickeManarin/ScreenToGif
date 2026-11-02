# ADR: Options leaves out Windows controls that Linux can never honor

## Status
Accepted. This narrows the rule that every Windows control whose subsystem is missing stays in place, disabled, for the Options window. The [Recorder command bar decision](20260921-recorder-command-bar-deviations.md) still governs the command bar.

## Area
Options, Recorder, Editor

## Context
The Linux Options window was built to read like the Windows one, with every Windows setting present and any setting Linux could not honor disabled beside a tooltip. The maintainer reviewed the port in [pull request 1474](https://github.com/NickeManarin/ScreenToGif/pull/1474#issuecomment-6565410203) and asked for unsupported features to leave the UI, giving the DirectX and BitBlt capture modes as the example, because a one-to-one match with Windows will not be possible.

Two different things were disabled. Some settings wait for a subsystem Linux could still gain: global hotkeys, cloud upload, tasks, updates, translations, caption authoring, cursor following and user-interaction capture. Others describe Windows internals that Linux does not have and never will, so a disabled control for them only asks when it will work.

## Decision
Options omits a control when it belongs to a Windows-only subsystem with no Linux counterpart:

- The BitBlt, DirectX, save-to-file and memory-cache capture modes, force memory cleanup, and remote-desktop performance. They select Windows capture back ends and a frame cache. The Linux recorder captures through X11 or the Wayland portal and writes each frame to disk as it is captured.
- The Old and New recorder interface choice, which picks between two WPF window implementations.
- Thin mode, which needs a custom window frame; Linux uses the desktop's window chrome.
- The switch for showing Discard while recording, because Linux always shows it.
- The Guidelines group, which held only a note. It returns with the overlay it describes.
- The "Not enough quota" workaround, which targets the WPF dispatcher.
- The editor's separate encodings window and extended title bar options.

A control whose subsystem could be ported stays in its Windows position, disabled, with a tooltip naming what is missing. The Recorder page's remaining rows follow [`docs/recorder.md`](../docs/recorder.md).

## Alternatives Considered
- **Keep every Windows control, disabled**: — This was the earlier rule. It keeps the Linux page aligned with the Windows captures, but it shows users choices that can never be made and hides which gaps are real.
- **Remove every disabled control, including the ones waiting on a subsystem**: — The window would be shortest, but it would also drop the placeholders for features the port intends to gain, and nothing would then say they are missing.

## Consequences

### Positive
- Every disabled control left in Options names a gap that could be closed.
- The Recorder page no longer opens with an interface choice and a capture back end the user cannot change.

### Negative
- The Linux pages no longer match the Windows captures row for row, so layout comparisons must account for the omitted rows.
- A user moving from Windows will not see these settings and may wonder where they went. The recorder documentation lists them and says why.

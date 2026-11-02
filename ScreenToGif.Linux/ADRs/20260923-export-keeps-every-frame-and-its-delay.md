# ADR: Export writes exactly the project's frames, with the last frame's delay set explicitly

## Status
Accepted. The 2026-10-22 Wayland recording increment applies the fine per-entry frame rate considered below, together with the existing final-delay handling. The original 40 ms grid limitation is resolved; the regression test independently decodes non-grid APNG delays.

## Area
Editor export

## Context
The Linux exporter feeds FFmpeg's concat demuxer a list of PNG files, each followed by a `duration` line. The concat demuxer ignores the last entry's duration, so the exporter inherited the common workaround of listing the last file a second time as a duration marker, pinned by a test that asserted four packets for three frames. With FFmpeg 6.1 that repeated entry is encoded as a frame of its own, so every export held one frame more than the project: a 28-frame recording exported as a 29-frame GIF. Dropping the repeated entry fixed the count but lost the last frame's delay: a held final frame exported at 40 ms as a GIF and at the previous frame's delay as an APNG, and videos ended early. The GIF and APNG muxers accept an explicit final delay, but refuse one outside 0 to 65535 (centiseconds for GIF, seconds for APNG) instead of clamping it as they do for interior frames.

## Decision
Animated images (GIF, APNG) list one concat entry per frame and set the last frame's delay through the muxer's `-final_delay`, clamped to the muxer's range. Videos (MP4, WebM) keep the repeated final entry: a video has no per-frame delay to set, and the extra frame, a copy of the last, is what gives the last frame its duration. The export test asserts the frame count and the last frame's duration in every format.

## Alternatives Considered
- **Keep the repeated entry everywhere**: — GIF and APNG exports carry an extra frame, visible in any frame count and in players that step frames.
- **Cap the output with `-frames:v N`**: — Tested; gives the same result as dropping the entry, including the lost last delay.
- **Set a fine per-entry frame rate (`option framerate`)**: — Removes the 40 ms grid, but loses the last frame's duration on its own. The current combination uses a 1000 fps input time base with `-final_delay` to preserve the final duration.

## Consequences

### Positive
- An animated export holds exactly the project's frames, and its last frame lasts its own delay, up to the format's limit.
- The test fails on any regression of either property.

### Negative
- A video export runs one input-tick frame longer than the project, since the copy frame keeps a default duration.
- At the time of the original decision, every exported delay landed on a 40 ms grid, because the concat inputs are read at the image demuxer's default 25 fps: 66 ms frames alternate 80/40 ms, 33 ms frames play at 40 ms (a 30 fps recording plays about 20% slow), and 16 ms frames are dropped. This predated the decision and was resolved on 2026-10-22 by specifying a 1000 fps PNG input time base. Output formats retain their own timing precision.
- The behavior was established against FFmpeg 6.1.1; other versions were not tested.

Enforced by: `LinuxEditorPipelineTests.Apng_preserves_measured_millisecond_delays_without_a_25fps_grid` and the existing GIF/MP4/WebM export checks.

## Source
- Session: Linux Recorder implementation run, 2026-09-21 to 2026-09-23 (execution log `~/.manifest-dev/logs/do-log-20260921-175218.md`)
- Related: none

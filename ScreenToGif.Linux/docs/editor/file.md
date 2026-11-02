# File

File can create a dimension/count/delay-defined blank project, reopen saved Linux
projects from a pruned most-recent list, and discard the active workspace with
unsaved-work confirmation. Project saves replace the destination only after a
complete temporary archive is ready.

Animated-image and video imports preserve per-frame durations when the source
exposes them, with stream timing used only as a fallback. Imports preflight
declared workloads before materializing timeline PNGs, then recheck actual
generated frames and dimensions before they reach the timeline. Projects are
limited to 10,000 frames and imports to one billion decoded pixels. Project loads
apply the same decoded-image bounds, and every timeline mutation stays within the
archive's per-frame and total storage limits.

Mixed-size exports center frames on the largest black canvas. MP4 and WebM
canvases are rounded up to even dimensions for their encoders. GIF export
requires frame delays of at least 10 ms.

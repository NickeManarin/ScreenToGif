# Playback tab

- Keep timeline-time resolution pure: given current positive delays, a start index, elapsed monotonic milliseconds, and loop state, return the visible frame, timeline time, and completion state. Tests should not wait on a real dispatcher timer.
- The resolver also returns the remaining time to the next frame boundary. After scheduler lag, scheduling the resolved frame's full delay preserves the lag instead of catching up.
- Normal playback may advance one frame on each delay-sized timer tick. “Drop frames when behind” must instead resolve from the monotonic elapsed clock on every tick so scheduler lag skips directly to the correct frame without modifying frame order.
- Non-looping completion stops predictably on the last frame. Looping uses timeline modulo, including when playback started from a middle selection.
- Play is a toggle in the ribbon: show Stop while active and restore Play on completion, mutation, navigation, explicit stop, empty timeline, or window cleanup.
- Every timeline mutation and active-project replacement stops playback before disposing or reconstructing frame objects. Do this at shared mutation boundaries so new commands cannot accidentally leave the timer pointing into stale state.
- First/Previous/Next/Last operate safely on empty and single-frame timelines and always synchronize selection, preview, scroll position, and status.
- Resolve one focused current-frame index after range-selection focus settles, then feed that same index to preview, delay, status, navigation, and Statistics. Disable empty and boundary-inapplicable playback controls instead of advertising no-op actions.
- Put play/stop/restart/drop progression in a playback-session seam with an injected monotonic clock. The dispatcher timer should only schedule the returned frame and residual delay; tests should drive the same session state used by the window.
- Test the session itself for one-frame display-then-completion and for stop→timeline count/timing mutation→restart. Stateless resolver examples, or advancing only after an already-stopped session, do not prove that active playback drops stale indices and adopts the new schedule.
- Dynamically disabled Undo/Redo and playback controls still need state-specific help such as empty timeline, current boundary, or no history entry. A disabled handler cannot deliver the status message it would show if invoked, so update its tooltip/accessibility explanation alongside `IsEnabled`.
- Playback advances a separate current/playhead marker and preview without changing the multi-frame editing selection. Preserve selected items across Play, ticks, completion, and Stop so the user can inspect motion and continue the same edit scope.
- Keep every surface that invokes the same play/stop action visually synchronized. When playback starts or stops, update both ribbon and status-bar glyphs, labels, and tooltips together; a Stop handler hidden behind a Play icon is a contradictory control state.

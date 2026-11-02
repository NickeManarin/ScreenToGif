# Editor session invariants

Use one mutation coordinator for the bookkeeping shared by every tab. A handler may own its dialog, validation, and domain operation, but it must hand a completed mutation to the coordinator so history, dirty state, command availability, timeline numbering, selection-derived statistics, and the focused preview refresh together.

Keep the window shell lean. Group handlers in focused partials or controllers by tab/workflow, and keep reusable history, transform, sequence, archive, playback, and statistics behavior in services with direct tests. Do not leave superseded single-command abstractions in production merely because their isolated tests still pass; delete them once the shared abstraction owns the behavior.

For async mutations, finish all cancellable/file-producing work before committing the new snapshot. For synchronous sequence changes, prepare the complete target snapshot before replacing live frames. A failure before commit must leave both the live editor and history unchanged.

Prepare any potentially large thumbnail set off the UI thread, including import/load, paste, generated transitions, image transforms, sequence edits, and Undo/Redo/Reset snapshot replay. Observe cancellation after worker completion and dispose prepared native resources before adoption; once the final check passes, adopt the timeline and commit history without another yield or cancellation check that could split those two state changes.

Enforce the project format's total frame ceiling at every timeline-growth boundary, using the current timeline count plus the proposed insertion count. Reject import, paste, transition, smooth-loop, and yoyo growth before copying or generating files; a per-operation batch limit can otherwise produce a live project that its own saver refuses.

Apply the project archive's per-frame and aggregate storage budgets to the complete proposed timeline before committing any mutation, including path-replacing image transforms and path-repeating sequence edits. Saving must not be the first time an oversized live project is rejected; prune newly generated but unreachable batches when a canceled or failed operation exits.

Extract cross-tab state machines from the Avalonia window: operation serialization/cancellation/close deferral, destructive-confirmation serialization, playback progression, and control availability must accept injected callbacks or clocks and run in deterministic tests without a dispatcher or rendered controls. Keep the window as the adapter that updates XAML surfaces around those results.

Keep the XAML visual tree expanded according to its real container hierarchy. Compressing sibling ribbon controls, item templates, overlays, or the status bar onto thousand-character lines hides parent/child ownership and makes ordinary parity changes opaque in review.

Stop playback before capturing selection or opening the first modal command dialog, not only when the eventual operation begins. A playback tick behind an open parameter dialog can otherwise move the visible selection while the handler retains an earlier target.

Treat branch-file pruning as best-effort housekeeping all the way through directory enumeration and history notification. Once a timeline mutation and history branch commit, a cleanup I/O failure must not surface as though the edit failed or corrupt Undo/Redo state.

Bound undo history as an artifact-ownership policy, not only a memory optimization. When the oldest retained edit is evicted, notify the same best-effort pruning seam used for discarded redo branches while retaining the explicit clean baseline needed by Reset and dirty-state comparison.

When returning a status control from an explicit error brush to its styled normal color in Avalonia, clear the local property value. Assigning `null` can keep overriding the style and make successful or guidance text render invisible.

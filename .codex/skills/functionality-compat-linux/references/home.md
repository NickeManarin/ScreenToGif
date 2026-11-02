# Home tab

- The Linux editor clipboard owns a dedicated workspace independent of the active project, not a list of current `EditorFrame` references. Create another unique set of project-owned files for every paste. This keeps cut/paste history valid across project replacement and after source frame objects are disposed.
- Do not replace clipboard contents until every selected file has copied successfully. Cancellation or a failed later copy must leave the previous clipboard usable and delete the incomplete new directory.
- Clipboard copy, cut, paste, and any other awaited timeline analysis must enter the same serialized operation boundary as FFmpeg edits. Otherwise two handlers can capture the same before-state and make one Undo silently consume both edits.
- Capture history selection as sorted timeline indices, not object references. Undo and redo reconstruct `EditorFrame` objects, so reference-based selection becomes stale.
- Set the reset baseline after a successful import or project load. Reset is itself one undoable edit: obtain its target without mutating history, materialize it, then record only after restoration succeeds and refresh the history controls.
- Numeric zoom accepts 10–800%. `Fit` is a distinct preview mode, not a parseable numeric value. Actual-size and numeric zoom set explicit rendered dimensions; Fit returns to `Stretch.Uniform` with automatic dimensions.
- Every zoom mode checks that a preview exists before reporting success; Fit must not claim it fitted a nonexistent image on an empty timeline.
- Selection math belongs in a pure boundary: invert the valid zero-based index set, and resolve Go to from a user-facing one-based number. This makes empty and boundary behavior independent of control layout.
- Exercise at least two pastes completely: assert both batches' count, order, delays, contents, existence, disjoint paths, and survival after one batch is removed. A disjointness assertion alone passes vacuously when the second paste is empty.

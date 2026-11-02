# Edit tab

- Express sequence operations over immutable `FrameState` lists and normalized sorted indices. Rebuild the UI collection from the result and record one before/after snapshot; do not let each `ObservableCollection` move become a separate history action.
- An empty selection means whole-timeline scope only for commands where that is understandable (Duplicates, Reduce, Smooth loop, Reverse, Yoyo). Delete, move, delay-selected, and delete-before/after require an explicit selection and should say so.
- Treat Delete-before at the first selected frame and Delete-after at the final selected frame as explicit no-ops: preserve the full discontiguous selection, report that no frames exist on that side, and do not create a selection-only history entry.
- Duplicate removal hashes one decoded frame through FFmpeg, not the encoded PNG bytes: identical pixels can have different compression or metadata. Remove only consecutive duplicates inside a contiguous selected scope; crossing an unselected gap resets comparison state.
- Force decoded duplicate inputs to one canonical pixel format such as RGBA before hashing. Raw-frame hashes otherwise differ for visually identical RGB and RGBA sources because the byte layouts differ.
- Reduce keeps the first frame in scope and then every Nth frame by scope ordinal. Reject factors below 2 before mutation.
- Reverse replaces only the selected positions with their reversed values, preserving all unselected positions. Stable left/right movement shifts each selected run by one without changing order inside the run.
- A yoyo appends only the reverse interior frames; repeating the endpoints creates visible pauses. A smooth loop is different: generate Fade intermediates from the last scoped frame toward the first using the same staged transition service.
- Route Delete, Move, and delay changes through the general history too. Leaving older one-off history code active makes the Home Undo control inconsistent even when new commands are correct.
- Exercise every sequence and delay command through the immutable-list boundary over empty, single, boundary, discontiguous, and repeated-content shapes, and record at least one exact before/after Undo/Redo oracle per command. Happy-path output examples alone missed the boundary-selection no-op defect.

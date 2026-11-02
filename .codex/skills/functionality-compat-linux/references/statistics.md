# Statistics tab

- Calculate statistics from a small immutable metric projection (`width`, `height`, `delay`) plus selection indices, current index, and display scale. Do not make calculation tests instantiate Avalonia controls or decode files.
- One tracker compares complete projected snapshots and notifies only when a value changes. Route every timeline/selection refresh through it so import, delay, transform, transition, playback, undo, and discard cannot each grow separate statistics wiring.
- Notification tests must drive the tracker through empty→single→mixed-size/mixed-delay→selection-only→empty snapshots and assert both exact emissions and duplicate suppression. Calculation-only cases do not prove that live Statistics refreshes for those state transitions.
- Distinguish absence from a known zero: empty dimensions/current/average/selected delay render `—`; mixed dimensions or selected delays render `Mixed`.
- Current timeline time is the sum of delays before the current zero-based frame. Total duration uses all delays and `long` arithmetic; average delay may be fractional.
- Image dimensions come from live source-pixel metadata refreshed when transformed frames commit, never from bounded thumbnail dimensions. Display scale comes from the rendered top-level scale rather than assuming 100%.
- Keep labels and values co-visible in the ribbon and use aligned, consistent units. Statistics are observation only and must never become a second source of editor state.

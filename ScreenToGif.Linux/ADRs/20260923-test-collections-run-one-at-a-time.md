# ADR: Test collections run one at a time

## Status
Accepted

## Area
Build, tests

## Context
Many tests in `ScreenToGif.Linux.Tests` start real FFmpeg processes, write large trees under the shared temporary directory, or start their own Xvfb server. With xUnit's default of running test collections in parallel, unrelated tests failed intermittently on a loaded machine. Two tests failed with two different symptoms: a project load that could not find its own extracted manifest, and an image edit that expected a failure and got none. Neither was one race that could be fixed at its site. Both came from contention for processes, disk and temporary space. Before the change, about 1 run in 8 failed.

One real race was found and fixed along the way: the workspace scavenger deleted a workspace whose owner marker it read mid-write, which `EditorWorkspace.WriteOwnerMarker` now prevents by publishing the marker in one rename. The other failures remained after that fix.

## Decision
`ScreenToGif.Linux.Tests/xunit.runner.json` sets `"parallelizeTestCollections": false`. Tests within one collection already run one at a time, so the whole suite now runs serially.

## Alternatives Considered
- **Keep parallel runs and fix each flaky test as it appears**: Each failure looked like a different bug, and chasing contention one symptom at a time never finishes while new FFmpeg-driven tests keep arriving.
- **Put only the FFmpeg and Xvfb tests into one shared collection**: Needs every future test author to know which collection a heavy test belongs in, and a test that forgets rejoins the contention silently.
- **Retry failed tests**: Hides a real failure behind a pass.

## Consequences

### Positive
- The suite's result depends on the code, not on what else the machine is doing: 15 consecutive runs passed after the change.

### Negative
- The suite takes about ten seconds longer.
- Code that is only wrong under concurrency is no longer shaken out by the test run. Code that must be thread-safe needs a test that creates the concurrency on purpose.

## Source
- Session: Linux Webcam recorder implementation, 2026-09-22 (commit "Run test collections one at a time")

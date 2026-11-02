# ADR: The Linux projects use LF line endings and a final newline

## Status
Accepted

## Area
Build

## Context
The repository root `.editorconfig` declares `end_of_line = crlf` and `insert_final_newline = false`, the Windows project's conventions. Every file in `ScreenToGif.Linux` and `ScreenToGif.Linux.Tests` is authored on Linux with LF endings and a trailing newline, so `dotnet format ScreenToGif.Linux.sln --verify-no-changes` failed on the whole solution: 57 final-newline errors and 6 import-ordering errors, all predating the Recorder work. A formatting gate on the Linux solution could not pass without deciding which convention the Linux projects follow.

## Decision
Each Linux project carries its own `.editorconfig` declaring `end_of_line = lf` and `insert_final_newline = true`, and inherits every other rule from the root file. The remaining import-ordering and blank-line fixes were applied once by the formatter.

Two small files align the tooling with how the port is actually written, where conforming would have meant stripping the final newline from 57 files and rewriting their line endings on a platform whose tools expect the opposite.

## Alternatives Considered
- **Conform the Linux files to the root convention**: strip final newlines and convert to CRLF. — Fights every Linux editor and tool, and the next file created on Linux fails the gate again.
- **Change the root `.editorconfig`**: — Changes the Windows application's conventions, which the Linux port must not touch.
- **Exclude the rules from the formatting check**: — Leaves the gate unable to catch real formatting drift in the Linux projects.

## Consequences

### Positive
- `dotnet format --verify-no-changes` on the Linux solution is a usable gate.
- Files authored on Linux need no special handling.

### Negative
- The repository holds two line-ending conventions, split by project directory; a file moved between the Windows and Linux trees changes convention.
- The one-time formatting commit touched about 20 files unrelated to any feature.

## Source
- Session: Linux Recorder implementation run, 2026-09-21 to 2026-09-23 (execution log `~/.manifest-dev/logs/do-log-20260921-175218.md`)
- Related: none

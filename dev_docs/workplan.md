# Karaoke Development Workplan

## Project Snapshot
- Build a Windows 11 desktop karaoke manager that imports prioritized song directories, supports split-screen search and queueing, dual-display playback, and auto-levels audio.

## Tooling Prerequisites
- Visual Studio 2025 Community with workloads: Desktop development with C#, Windows App SDK, and .NET profiling tools.
- .NET 8 SDK and Windows App SDK runtime (installed with the workload but verify versions during setup).
- Git for version control and the "dotnet-format" global tool for consistent styling.
- Optional: FFmpeg or NAudio for advanced audio processing, and 7-Zip for bulk media import automation.

## Milestones
1. Requirements & UX Definition
   - Translate project.md requirements into actionable user stories.
   - Wireframe the search plus playlist layout and finalize keyboard shortcut mappings.
2. Project Scaffolding
   - Initialize a WinUI 3 desktop app in src/UI; add class libraries for Player, Library, and Common domains.
   - Configure dependency injection, structured logging, configuration providers, and solution-wide analyzers.
   - Add a unit test project under tests and draft a CI pipeline stub for build and tests.
3. Library Ingestion
   - Implement directory scanners that recognize /Artist/Song and /Artist-Song conventions.
   - Persist metadata (artist, title, channels, priority) in SQLite; allow directory-level default priorities and per-track overrides.
   - Build settings UI to remap root drive letters without full rescans.
4. Search & Playlist Experience
   - Implement pinyin-acronym indexing (for example hdyks -> Hao Da Yi Ke Shu) with incremental filtering.
   - Create an artist browser and dual-pane view; support double-click or drag to enqueue.
   - Add queue management commands (reorder, jump, delete) with keyboard shortcuts.
5. Playback & Audio Consistency
   - Integrate the audio engine (BASS.NET or NAudio) with channel selection and an independent playback window for a second monitor.
   - Remember window placement, expose quick toggles for left or right vocals, and ensure smooth transitions between tracks.
   - Implement loudness normalization (ReplayGain-style) and manual gain overrides per song.
6. Stabilization & Release
   - Expand automated tests (unit and integration over sample assets) and collect coverage reports.
   - Document operations, package via MSIX, and finalize a smoke test checklist.

## Next Actions
- Confirm the preferred audio SDK and its licensing.
- Decide on the catalog storage engine (SQLite vs. LiteDB) and backup approach.
- Assemble a representative sample library for development and automated tests.


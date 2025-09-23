# Karaoke Staged Development Plan

This staged plan elaborates the development milestones into actionable phases with explicit deliverables, dependencies, and validation activities.

## Stage 0 - Environment Validation
- **Goals**
  - Confirm every developer workstation has the required SDKs, IDE workloads, and tooling prerequisites.
  - Establish shared configuration baselines (dotnet SDK version, Windows App SDK toolchain, formatting rules).
- **Deliverables**
  - Environment checklist stored under `dev_docs/` with version pins.
  - Validated `global.json` (if needed) to lock the repository to .NET 8.
  - Documented instructions for restoring sample assets and running the solution.
- **Engineering & Operational Tasks**
  - Run `dotnet --info`, `dotnet-format --version`, `ffmpeg -version`, and `7z i` to capture versions.
  - Verify Visual Studio workloads: Desktop development with C#, Windows App SDK, and Windows App Runtime.
  - Add build tooling prerequisites to the onboarding section in `project.md`.
- **Testing**
  - Execute `dotnet restore` (root) to ensure package feeds resolve.
  - Run `dotnet build` on a placeholder solution (created in Stage 2) to catch missing workloads.
  - Manual smoke checklist for FFmpeg and 7-Zip command-line usage.
- **Exit Criteria**
  - Team agrees the environment report is complete and reproducible.
  - CI agent image (if any) mirrors the local setup requirements.

## Stage 1 - Requirements & UX Definition
- **Goals**
  - Translate business goals and the Chinese requirements brief into user stories and acceptance criteria.
  - Finalize interaction flows for search, queue management, and dual-monitor playback.
- **Deliverables**
  - User story backlog with priority tags and acceptance tests in `dev_docs/requirements/`.
  - Wireframes covering search panel, queue panel, playback window, and settings screens.
  - Keyboard shortcut matrix and localization considerations.
- **Engineering & Operational Tasks**
  - Workshop stories derived from `project.md` and `workplan.md` milestones.
  - Define navigation shell architecture (WinUI window layouts, navigation patterns).
  - Capture data glossary (artist, title, channel metadata, priority enums).
- **Testing**
  - Create story-based acceptance checklists (Given/When/Then format) to be automated later with UI tests.
  - Validate wireframes with lightweight usability walkthrough (manual review).
- **Exit Criteria**
  - Product owner signs off on backlog scope and wireframes.
  - Stories tagged with traceability IDs for future test automation.

## Stage 2 - Project Scaffolding
- **Goals**
  - Stand up solution structure, domain libraries, and base infrastructure.
  - Ensure the repository builds, runs, and has a test harness stub.
- **Deliverables**
  - `Karaoke.sln` referencing `src/Common`, `src/Library`, `src/Player`, and `src/UI` projects.
  - Shared `Directory.Build.props` enabling nullable reference types, analyzers, and consistent coding style.
  - xUnit test project under `tests/` mirroring the namespace layout.
  - Continuous integration stub (GitHub Actions or Azure DevOps) for restore, build, and test.
- **Engineering Tasks**
  - Implement dependency injection container (Microsoft.Extensions.Hosting) and logging plumbing.
  - Configure configuration providers to load `config/settings.json` with environment overrides.
  - Add sample domain models (SongDto, QueueEntryDto) and basic services interfaces.
- **Testing**
  - Unit tests: bootstrap smoke test asserting the service provider resolves core services.
  - Build validation: `dotnet build Karaoke.sln -c Debug` on CI.
  - Static analysis: enable nullable warnings as errors, run `dotnet format --verify-no-changes` in CI.
- **Exit Criteria**
  - CI pipeline green for restore/build/test/format checks.
  - Developers can `dotnet run --project src/UI/Karaoke.UI.csproj` to launch an empty shell window.

## Stage 3 - Library Ingestion
- **Goals**
  - Implement media catalog ingestion, metadata persistence, and drive letter remapping.
  - Provide settings UI to manage library roots and priority rules.
- **Deliverables**
  - Library scanner service with pluggable parsers for `/Artist/Song` and `Artist-Song` naming.
  - SQLite-backed repository (EF Core or Dapper) storing song metadata, channels, and priority levels.
  - Settings view model plus WinUI page for root path management and drive remapping.
  - Sample dataset under `assets/sample/` with varied naming conventions and channel metadata.
- **Engineering Tasks**
  - Build incremental scan pipeline with hashing or timestamp checks to avoid full rescans.
  - Implement priority inheritance (directory default with per-track overrides) and persistence logic.
  - Add background task infrastructure to run scans without blocking the UI thread.
- **Testing**
  - Unit tests: parser coverage for filename conventions, priority resolution logic, drive remap service.
  - Integration tests (tests/Library/Integration): ingest sample assets and verify persisted records.
  - Performance regression test: measure scan time on a synthetic dataset (scripted).
  - Code coverage target: >=80% line coverage for `src/Library`.
- **Exit Criteria**
  - Library settings page updates state and persists configuration across sessions.
  - Ingestion tests run in CI using `dotnet test --collect:"XPlat Code Coverage"` and meet threshold.

## Stage 4 - Search & Playlist Experience
- **Goals**
  - Deliver responsive search plus playlist UI with pinyin-acronym matching and queue controls.
  - Provide dual-pane desktop experience mirroring Windows 11 Fluent design.
- **Deliverables**
  - Search view models with incremental filtering, debounce, and scoring.
  - Queue management service supporting reorder, jump-to, delete, and priority overlay.
  - WinUI pages for search, queue, artist browser, with drag/drop and keyboard shortcuts.
  - Telemetry hooks for search usage (optional) stored locally for diagnostics.
- **Engineering Tasks**
  - Implement pinyin or acronym index builder (external library or custom trie).
  - Add virtualization for large result sets (ItemsRepeater or GridView with incremental loading).
  - Wire command routing for keyboard shortcuts; ensure focus management for split panes.
- **Testing**
  - Unit tests: search scoring, pinyin index lookups (`tests/Library/SearchTests.cs`).
  - UI interaction tests: WinAppDriver or Playwright (if feasible) for enqueue actions and drag/drop.
  - Snapshot tests for queue serialization and persistence across sessions.
  - Accessibility audit using WinUI Accessibility Insights scripts.
- **Exit Criteria**
  - User can locate songs via English, pinyin acronym, or direct title and enqueue seamlessly.
  - Queue operations reflect in UI and persisted state without race conditions (verified by tests).

## Stage 5 - Playback & Audio Consistency
- **Goals**
  - Integrate the audio engine, dual-display playback window, and channel controls.
  - Normalize audio levels and persist per-track overrides.
- **Deliverables**
  - Audio playback service (for example, an NAudio wrapper) with state machine for play/pause/next.
  - Secondary playback window remembering monitor and position, synchronized with primary UI.
  - Loudness normalization pipeline (ReplayGain-style) and manual gain settings in UI.
  - Channel toggle controls for left/right vocals with immediate feedback.
- **Engineering Tasks**
  - Implement cross-fade or smooth transition logic between tracks.
  - Handle format compatibility (MP3, MP4, MKV) using FFmpeg probes for metadata.
  - Persist playback history and last-known settings in local app data.
- **Testing**
  - Unit tests: playback state transitions, gain calculation, channel toggle logic.
  - Integration tests: using sample assets to verify FFmpeg probing and playback bootstrap.
  - Manual dual-monitor smoke test to validate window placement persistence.
  - Audio regression harness measuring peak levels before and after normalization.
- **Exit Criteria**
  - Playback window runs independently on a second monitor without tearing or lag.
  - Audio normalization differences logged and within acceptable thresholds (document results).

## Stage 6 - Stabilization & Release Prep
- **Goals**
  - Harden the application, improve coverage, and prepare deployment artifacts.
  - Document operations and finalize release checklist.
- **Deliverables**
  - Expanded automated test suite covering edge cases and regression scenarios.
  - MSIX packaging setup with signing configuration (test certificate) and install docs.
  - Operations guide documenting library management, hotkeys, and troubleshooting.
  - Coverage reports stored under `dev_docs/reports/` with trend tracking.
- **Engineering Tasks**
  - Address telemetry or logging gaps; configure structured logging sinks.
  - Perform performance profiling (CPU and memory) under typical workloads.
  - Conduct security review for metadata sanitization and path handling.
- **Testing**
  - Comprehensive regression run: `dotnet test --collect:"XPlat Code Coverage"` with thresholds enforced.
  - UI smoke tests across supported Windows builds (Windows 11 latest plus previous release).
  - Installer validation: install and uninstall MSIX, verify file associations and shortcuts.
  - Beta tester feedback loop with defect triage tracking.
- **Exit Criteria**
  - Coverage targets met (>=80% for Library and Player, >=70% overall).
  - Release checklist completed, artifacts uploaded to distribution channel, and project documentation updated.
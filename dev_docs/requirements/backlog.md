# User Story Backlog

This backlog translates the product requirements into traceable user stories with priorities and acceptance criteria. Priorities use `P0` (critical), `P1` (high), and `P2` (nice-to-have after launch).

## Library & Settings

### LIB-001 - Scan Artist/Song folder hierarchies (P0)
**Description**
- As a library manager, I want to import music stored as `Artist/Title` so that existing structured folders are recognized automatically.

**Acceptance Criteria**
- Given a root folder with nested `Artist/Title` subfolders, when I trigger a library scan, then songs are imported with artist and title populated from the folder names.
- Given two artists with subfolders, when the scan completes, then each track has a stable identifier composed of path and filename.
- Given unsupported files (non-audio extensions), when the scan runs, then they are skipped and listed in a warning log.

### LIB-002 - Scan Artist-Title filenames (P0)
**Description**
- As a library manager, I want to import files named `Artist-Title.ext` so that flat folder libraries are supported.

**Acceptance Criteria**
- Given a folder containing `Artist-Title.mp4`, when I scan, then artist and title are parsed from the filename.
- Given filenames with hyphens in artist or title, when I define a delimiter preference, then parsing respects the configured delimiter.
- Given an ambiguous filename, when parsing fails, then the track is flagged for manual review in the scan report.

### LIB-003 - Default priority inheritance (P0)
**Description**
- As a host, I want default priorities applied per folder so that newly added songs inherit the stage order I expect.

**Acceptance Criteria**
- Given a folder with a defined default priority, when new songs are scanned, then they are stored with that priority unless overridden.
- Given a folder with no default priority configured, when songs are scanned, then they default to priority `2`.
- Given a folder priority change, when I rescan, then existing songs update to the new default unless they already have explicit overrides.

### LIB-004 - Track-level priority overrides (P0)
**Description**
- As a host, I want to adjust an individual song's priority without changing the rest of the folder.

**Acceptance Criteria**
- Given a song, when I edit its priority in the UI, then the new value persists to the catalog.
- Given an overridden priority, when the library rescan occurs, then the override remains intact.
- Given a priority of `0`, when the queue renders, then the track shows as VIP/fast pass.

### LIB-005 - Persist metadata to SQLite (P0)
**Description**
- As an operator, I need the catalog stored reliably so that searches and playback are consistent across sessions.

**Acceptance Criteria**
- Given a successful scan, when it completes, then song metadata (artist, title, path, channels, priority, duration) is stored in SQLite.
- Given an application restart, when I open the app, then previously imported songs are available without re-scanning.
- Given concurrent scans, when a second scan starts, then the system queues the request or rejects it with a clear message.

### SET-001 - Manage library root directories (P0)
**Description**
- As an administrator, I want to add, remove, and reorder library roots so that the system knows where to ingest media.

**Acceptance Criteria**
- Given the settings page, when I add a new root directory, then it appears in the root list and is scheduled for scanning.
- Given multiple roots, when I reorder them, then the preferred scan order persists in settings storage.
- Given a removed root, when I confirm deletion, then associated songs are marked inactive but retained for historical reference.

### SET-002 - Drive letter remapping (P0)
**Description**
- As a user who moves drives between machines, I want to remap drive letters without rescanning everything.

**Acceptance Criteria**
- Given an existing root at `D:\Karaoke`, when I change the drive letter to `E:`, then the catalog updates path references without a full rescan.
- Given multiple mappings, when I configure them, then the UI lists each mapping pair so I can review changes.
- Given a missing drive, when the app launches, then it prompts me to remap or temporarily disable the library.

### SET-003 - Library scan scheduling and status (P1)
**Description**
- As an operator, I want to trigger scans manually and view their progress so that I know when new songs are ready.

**Acceptance Criteria**
- Given the settings page, when I press `Scan Now`, then a background scan starts and displays a progress bar.
- Given a running scan, when I open the settings page, then I see elapsed time, estimated completion, and pending file count.
- Given a completed scan, when results contain warnings, then they are shown with actionable follow-up steps.

## Search & Queue Experience

### SRCH-001 - Basic text search (P0)
**Description**
- As a host, I want to search by song title or artist so that I can locate tracks quickly.

**Acceptance Criteria**
- Given an indexed library, when I type a title, then matching songs appear ordered by priority and relevance.
- Given Unicode characters, when I search using native language input, then results include matching entries.
- Given no matches, when a search returns nothing, then the UI displays a friendly message with tips.

### SRCH-002 - Pinyin acronym search (P0)
**Description**
- As a host, I want to type pinyin acronyms (for example `hdyks`) and see the corresponding Chinese title.

**Acceptance Criteria**
- Given an indexed library with pinyin tokens, when I type an acronym, then songs with matching initials are returned.
- Given multiple matches, when the acronym is ambiguous, then results are sorted by priority and historical play count.
- Given mixed input (letters and Chinese characters), when I search, then both pinyin and direct matches are considered.

### SRCH-003 - Incremental filtering with large catalogs (P1)
**Description**
- As a user managing thousands of songs, I want results to update as I type without lag.

**Acceptance Criteria**
- Given a 20k-song catalog, when I type quickly, then results refresh within 200ms after I pause typing.
- Given new search terms, when I continue typing, then previous results are replaced without flicker or layout shifts.
- Given a high-latency filter, when performance thresholds are exceeded, then diagnostic telemetry captures the delay.

### UI-001 - Search results layout (P0)
**Description**
- As a host, I want a dual-pane layout showing results and the queue so that I can keep context while searching.

**Acceptance Criteria**
- Given the main window, when search results load, then the left pane lists songs with priority badges and icons for channel availability.
- Given a selected song, when I preview details, then the right pane highlights queue controls without hiding search results.
- Given a narrow window, when screen width drops below 1280px, then the layout adapts to stacked panes with a toggle header.

### QUEUE-001 - Enqueue from search (P0)
**Description**
- As a host, I want to double-click or press Enter to add a song to the queue so that I can work quickly.

**Acceptance Criteria**
- Given a highlighted search result, when I double-click it, then the song is appended to the active queue.
- Given keyboard focus on results, when I press Enter, then the enqueue action triggers without reloading the list.
- Given duplicates, when the same song is added twice, then the queue shows both entries with separate queue positions.

### QUEUE-002 - Queue management commands (P0)
**Description**
- As a host, I want to reorder, jump, and delete queue items so that I can adjust the show on the fly.

**Acceptance Criteria**
- Given a queue item, when I drag it up or down, then the order updates and persists.
- Given a selected item, when I press `Ctrl+J`, then playback jumps to that song and relocates it to the Now Playing slot.
- Given an item, when I press Delete, then it is removed and the queue renumbers without gaps.

### QUEUE-003 - Priority visualization in queue (P1)
**Description**
- As an emcee, I need the queue to show priority tiers so that VIP songs stand out.

**Acceptance Criteria**
- Given queued songs, when the list renders, then priority `0` items show a prominent accent color and badge text.
- Given priority changes, when a song's priority is edited, then the queue updates badges in real time.
- Given a long queue, when I scroll, then sticky headers summarize counts per priority tier.

### HOTKEY-001 - Search and queue navigation shortcuts (P0)
**Description**
- As a power user, I want keyboard shortcuts to switch focus between search, queue, and playback controls.

**Acceptance Criteria**
- Given the main window, when I press `Ctrl+1`, then focus moves to the search box and the cursor is visible.
- Given the main window, when I press `Ctrl+2`, then focus shifts to the queue list without reselection.
- Given the playback overlay, when I press `Ctrl+3`, then the playback controls gain focus for arrow-key navigation.

### HOTKEY-002 - Playback control shortcuts (P0)
**Description**
- As an emcee, I want dedicated keyboard controls for play/pause, skip, and channel toggles.

**Acceptance Criteria**
- Given any focus state, when I press Space, then playback toggles play/pause.
- Given any focus state, when I press `Ctrl+Right`, then the next track starts and the queue advances.
- Given any focus state, when I press `Ctrl+L`, then the lead vocal channel toggles and the UI shows the new state.

## Playback & Audio

### PLY-001 - Dual-screen playback window (P0)
**Description**
- As a host, I want a secondary window for lyrics on the external display while keeping controls on the main screen.

**Acceptance Criteria**
- Given a system with two monitors, when I enable external display mode, then a borderless playback window appears on the secondary screen.
- Given window placement, when I reopen the app, then the playback window remembers the target display and resolution.
- Given a missing second monitor, when playback starts, then the system falls back to an integrated overlay with a warning.

### PLY-002 - Channel selection controls (P0)
**Description**
- As a singer, I want to mute left or right vocal channels so that I can listen to instrumental or vocal guides.

**Acceptance Criteria**
- Given stereo tracks with distinct channels, when I press the channel toggle, then the audio mix updates instantly.
- Given persisted preferences, when I reopen the app, then previous channel settings restore automatically.
- Given mono tracks, when I toggle channels, then the control is disabled with a tooltip explaining why.

### PLY-003 - Loudness normalization (P0)
**Description**
- As a host, I want consistent playback volume so that singers are not surprised by sudden changes.

**Acceptance Criteria**
- Given tracks with varying loudness, when they play sequentially, then perceived volume stays within +/- 2 dB LUFS of the target.
- Given a track override, when I adjust gain manually, then the new value remains until I reset it.
- Given normalization failures, when FFmpeg cannot analyze a file, then the track plays at original volume and the log captures the exception.

### PLY-004 - Seamless track transitions (P1)
**Description**
- As an audience member, I want smooth cuts between songs so there is no dead air.

**Acceptance Criteria**
- Given consecutive songs, when one ends, then the next fades in with a configurable crossfade duration.
- Given manual skips, when I advance to the next track, then the crossfade occurs unless disabled in settings.
- Given an error loading the next track, when a failure happens, then the UI alerts me and preserves the current track.

### PLY-005 - Playback history and resume (P2)
**Description**
- As an operator, I want to see recently played songs and resume partially played tracks.

**Acceptance Criteria**
- Given a completed performance, when I open the history panel, then the last 100 songs are listed with timestamps and singers (if provided).
- Given a partially played track, when the app restarts, then the queue offers an option to resume from the previous position.
- Given privacy mode enabled, when history recording is off, then no data is stored and the UI indicates the state.

## Analytics & Localization

### ANA-001 - Local telemetry for diagnostics (P2)
**Description**
- As a maintainer, I want anonymous usage telemetry stored locally so that I can troubleshoot performance issues.

**Acceptance Criteria**
- Given telemetry enabled, when search latency exceeds thresholds, then events are written to a rolling log file.
- Given the diagnostics view, when I export logs, then a sanitized JSON package is produced.
- Given telemetry disabled, when I review settings, then the toggles clearly show no data is being collected.

### LOC-001 - Localization readiness (P1)
**Description**
- As a bilingual team, we want the UI prepared for English and Simplified Chinese strings.

**Acceptance Criteria**
- Given the WinUI resource files, when I switch languages, then view labels, menu items, and tooltips display the selected language.
- Given user-generated metadata, when text includes Chinese characters, then they render correctly in the queue and playback windows.
- Given untranslated strings, when a resource key is missing, then the system logs the key and displays a safe fallback.

## Non-Functional

### NFR-001 - Performance baselines (P1)
**Description**
- As a team, we need performance targets defined so we can guard against regressions.

**Acceptance Criteria**
- Given a 50k-song library, when the app starts, then it reaches interactive search within 5 seconds.
- Given playback, when a track begins, then audio starts within 500ms of selecting play.
- Given queue operations, when I drag items, then the UI updates within 100ms.

### NFR-002 - Security hardening (P0)
**Description**
- As maintainers, we must sanitize metadata to prevent script injection or markup issues.

**Acceptance Criteria**
- Given imported song metadata, when text contains HTML or script tags, then they are escaped before display.
- Given file paths, when a path includes relative segments, then the importer normalizes them or rejects the entry.
- Given UI bindings, when a string is displayed, then it uses safe text blocks (no markup parsing).
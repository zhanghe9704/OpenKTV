# UX Layout Notes

These notes describe the planned WinUI layouts that back the Stage 1 wireframes. Each section outlines structure, key components, and responsive behavior.

## Main Shell (Search + Queue)

```
+------------------------------------------------------------+
| Command Bar: [Search Box][Clear][Scan Status][Settings]     |
+---------------------------+-------------------------------+
| Search Pane               | Queue Pane                    |
| ------------------------- | ----------------------------- |
| - Filters (priority,      | - Now Playing header          |
|   language)               | - Queue list with drag handles|
| - Result list (virtualized| - Quick actions (Jump, Skip)  |
|   rows, badges, icons)    | - Queue metrics footer        |
+---------------------------+-------------------------------+
| Transport Strip: [Prev][Play/Pause][Next][Gain][Channel]   |
+------------------------------------------------------------+
```

- Layout uses a `Grid` with two columns (40% search / 60% queue) on widths >= 1280px.
- Below 1280px the layout switches to stacked panes with pivot headers for Search and Queue.
- Result list relies on `ItemsRepeater` with data templates showing title, artist, priority badge, channel icons, duration.
- Transport strip is pinned to the bottom using `TeachingTip`-style callouts for keyboard references.

## Settings View

```
+------------------------------------------------------------+
| Settings Nav: [Library][Playback][Hotkeys][About]           |
+------------------------------------------------------------+
| Section Header + Description                               |
|                                                            |
| Card: Library Roots                                        |
|  - ListView of paths, drag handle, priority default badge  |
|  - Buttons: Add Root, Remove, Rescan                       |
|                                                            |
| Card: Drive Remapping                                      |
|  - Mapping table: Current Drive, New Drive, Status         |
|  - Button: Apply Mapping                                   |
|                                                            |
| Card: Scan Schedule                                        |
|  - Buttons: Scan Now, Cancel                               |
|  - Progress bar with ETA, warning banner area              |
+------------------------------------------------------------+
```

- Navigation uses a left-aligned `NavigationView` in pane display mode with icons.
- Each card is a `StackPanel` with `ContentDialog` support for confirmation flows (remove root, apply mapping).
- Warnings and logs surface via an `InfoBar` control.

## Playback Window (External Display)

```
+------------------------------------------------------------+
| Top Overlay: Song Title | Artist | Key/Tempo | Timer       |
+------------------------------------------------------------+
|                  Lyrics Canvas (centered text)              |
|                                                            |
|      Background video / visualizer fills remaining area     |
+------------------------------------------------------------+
| Bottom Overlay (auto-hide):                                |
|  [Prev][Play/Pause][Next]  [Lead Vocal Toggle][Right Toggle]|
+------------------------------------------------------------+
```

- Runs as a separate `Window` instance with borderless full-screen mode on the target monitor.
- Lyrics text uses `AnimatedVisualPlayer` for fade transitions and supports dual-language display (primary + romanization).
- Channel toggles mirror state from main shell via shared view models; status badges display inline with icons.

## Responsive and Accessibility Considerations

- All panes respect a minimum touch target of 40x40 px; keyboard focus order follows left-to-right, top-to-bottom navigation.
- High-contrast mode relies on WinUI resource dictionaries; priority badges switch to shape variations when color is insufficient.
- Screen reader landmarks: main shell exposes `search`, `queue`, and `status` regions; playback window exposes `main` and `controls` regions.

These descriptions guide initial component placement; detailed XAML sketches and pixel measurements will be produced as part of Stage 2 UI implementation.
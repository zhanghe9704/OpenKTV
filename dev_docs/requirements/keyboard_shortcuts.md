# Keyboard Shortcut Matrix

This matrix captures planned shortcut bindings for the desktop app. Commands are grouped by surface area and map to user stories HOTKEY-001 and HOTKEY-002.

| Scope | Shortcut | Action | Notes |
|-------|----------|--------|-------|
| Global | `Ctrl+1` | Focus search box | Returns caret to search input without clearing text. |
| Global | `Ctrl+2` | Focus queue list | Keeps current selection; scrolls the selected item into view. |
| Global | `Ctrl+3` | Focus playback controls | Highlights the transport control bar for arrow-key navigation. |
| Global | `Ctrl+Shift+F` | Toggle full-screen playback window | Sends or recalls the external display window. |
| Search | `Enter` | Enqueue highlighted result | Mirrors double-click behavior. |
| Search | `Ctrl+Enter` | Enqueue and mark as priority 0 | Adds VIP badge automatically. |
| Search | `Ctrl+F` | Clear search text | Preserves focus, resets filters. |
| Queue | `Delete` | Remove selected queue item | Prompts only when multiple items selected. |
| Queue | `Ctrl+J` | Jump selected item to Now Playing | Aligns with QUEUE-002 acceptance criteria. |
| Queue | `Ctrl+Up` / `Ctrl+Down` | Move item up or down | Supports continuous press for rapid reorder. |
| Playback | `Space` | Play/Pause toggle | Works regardless of focus. |
| Playback | `Ctrl+Right` | Skip to next track | Starts next track with configured transition effect. |
| Playback | `Ctrl+Left` | Restart current track | Seeks to time zero and resumes playback. |
| Playback | `Ctrl+L` | Toggle lead vocal channel | Flips between vocal mix presets. |
| Playback | `Ctrl+R` | Toggle right vocal channel | Complements `Ctrl+L` when independent control required. |
| Playback | `Ctrl+Shift+N` | Normalize current track | Forces loudness recalculation and reapplies gain. |
| Playback | `Ctrl+Shift+S` | Snapshot current mix | Saves manual gain and channel settings as overrides. |

Future shortcuts will be added as additional user stories enter scope; this document should remain in sync with in-app help overlays and tooltips.
# Karaoke Application - Keyboard Shortcuts

This document lists all the keyboard shortcuts available in the Karaoke application.

## Playback Controls

| Shortcut | Action | Description |
|----------|--------|-------------|
| `Ctrl+P` | Play | Start playback of the current song |
| `Space` | Pause | Pause/resume playback |
| `Esc` | Stop | Stop playback completely |
| `Ctrl+N` | Next | Skip to the next song in queue |
| `Ctrl+R` | Repeat | Restart the current song from the beginning |

## Display Controls

| Shortcut | Action | Description |
|----------|--------|-------------|
| `Ctrl+F` | Full Screen | Toggle full screen mode for the video player |

## Queue Management

| Shortcut | Action | Description |
|----------|--------|-------------|
| `Ctrl+↑` | Move Up | Move the selected song one position up in the queue |
| `Ctrl+↓` | Move Down | Move the selected song one position down in the queue |
| `Ctrl+←` | Move Top | Move the selected song to the position right after the currently playing song |
| `Ctrl+→` | Move Bottom | Move the selected song to the bottom of the queue |

## Application Controls

| Shortcut | Action | Description |
|----------|--------|-------------|
| `Ctrl+S` | Settings | Open the application settings dialog |

## Notes

- **Global Shortcuts**: All shortcuts work regardless of which control has focus (main window, Queue ListView, etc.)
- **Queue Focus Override**: When the Queue ListView has focus, `Ctrl+↑` and `Ctrl+↓` will move songs instead of performing the default ListView multi-selection navigation
- **Selected Song Required**: Queue movement shortcuts require a song to be selected in the queue
- **Movement Restrictions**: Songs that have already been played cannot be moved (this prevents the gold playing indicator from jumping around)

## Usage Tips

1. **Quick Queue Management**: Use `Ctrl+↑/↓` to fine-tune song order
2. **Priority Songs**: Use `Ctrl+←` to move a song to play next after the current song
3. **Defer Songs**: Use `Ctrl+→` to move songs to the end of the queue
4. **Instant Controls**: Use `Space` for quick pause/resume and `Esc` for immediate stop
5. **One-Hand Operation**: Most shortcuts use the left Ctrl key for easy one-handed operation while using the mouse

## Accessibility

All shortcuts are also available through button clicks in the main toolbar for users who prefer mouse interaction or cannot use keyboard shortcuts.
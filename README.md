# OpenKTV

OpenKTV is a modern karaoke application for Windows, built with .NET 8 and WinUI 3. It provides a comprehensive karaoke experience with dual-track audio support, library management, and recording capabilities.

## Features

- **Song Library Management**: Automatically scan and organize your karaoke collection
- **Dual-Track Audio Support**: Switch between vocal and instrumental tracks (Ctrl+V)
- **Smart Search**: Pinyin initials support for Chinese songs
- **Queue Management**: Drag-and-drop queue with visual playback indicators
- **Recording**: Record your performances with system audio (Ctrl+E)
- **Volume Normalization**: FFmpeg-powered loudness analysis
- **Customizable Metadata**: Flexible keyword format parsing (artist, song, language, genre, comment)
- **VLC-Powered Playback**: Supports multiple formats (MP3, MP4, MKV, AVI, MPG, RMVB, etc.)

## System Requirements

- Windows 10 or later (64-bit)
- .NET 8 Desktop Runtime (for framework-dependent build) OR
- Self-contained version (no prerequisites required)

## Installation

### Self-Contained Version (Recommended)
1. Download and extract the release package
2. Run `OpenKTV.exe`
3. No installation required!

### Framework-Dependent Version
1. Install [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Download and extract the framework-dependent release
3. Run `OpenKTV.exe`

## Building from Source

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 (recommended) or Visual Studio Code
- Windows 10 SDK (10.0.19041.0)

### Build Instructions

**Self-Contained Release:**
```batch
build-release.bat
```

**Framework-Dependent Release:**
```batch
build-release-framework.bat
```

Or using PowerShell directly:
```powershell
.\build-release.ps1
.\build-release-framework.ps1 -OutputDir "my-custom-folder"
```

## Keyboard Shortcuts

### Main Window
- `Ctrl+T`: Open Library Settings
- `Ctrl+F`: Focus on Search box
- `Ctrl+E`: Toggle Recording
- `Ctrl+V`: Toggle Vocal track
- `Ctrl+N`: Next song
- `Ctrl+R`: Repeat/Restart current song
- `F5`: Refresh library
- `Enter`: Play selected song / Queue song
- `Delete`: Remove from queue

### Player Window
- `F11`: Toggle Fullscreen
- `Ctrl+F`: Toggle Fullscreen
- `Space`: Pause/Resume
- `Escape`: Exit fullscreen
- `Ctrl+N`: Next song
- `Ctrl+R`: Repeat/Restart current song
- `Ctrl+V`: Toggle Vocal track
- `Right Arrow`: Volume up (+5%)
- `Left Arrow`: Volume down (-5%)

## Project Structure

```
OpenKTV/
├── src/
│   ├── UI/Karaoke.UI/              # WinUI 3 application
│   ├── Library/Karaoke.Library/     # Library management & database
│   ├── Player/Karaoke.Player/       # VLC playback & recording
│   └── Common/Karaoke.Common/       # Shared models & utilities
├── tests/                           # Unit tests
├── build-release.ps1                # Self-contained build script
├── build-release-framework.ps1      # Framework-dependent build script
└── OpenKTV.sln                      # Solution file
```

## Configuration

### Library Settings
Press `Ctrl+T` to configure:
- **Library Folders**: Add folders containing your karaoke files
- **Keyword Format**: Customize how filenames are parsed (e.g., `artist-song-language-genre`)
- **Volume Normalization**: Enable/disable FFmpeg loudness analysis
- **Drive Override**: Remap drive letters without rescanning (e.g., change `D:\Music` to `E:\Music`)
  - Useful when moving libraries to different drives or using external/network drives
  - Format: Enter drive letter with colon (e.g., `E:` or `F:`)
  - **Note**: Requires app restart to take effect

### First Run
On first launch, OpenKTV creates:
- `data/library.db` - SQLite database for your song library
- `config/settings.json` - Application settings
- `record/` - Folder for recorded performances (when recording is used)

## Supported File Formats

- Audio: MP3, WAV
- Video: MP4, MKV, AVI, MPG, MPEG, RMVB, RM, DAT

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

[Add your license here]

## Acknowledgments

- Built with [LibVLCSharp](https://github.com/videolan/libvlcsharp) for media playback
- Uses [NAudio](https://github.com/naudio/NAudio) for recording
- Powered by [WinUI 3](https://microsoft.github.io/microsoft-ui-xaml/)
- FFmpeg for volume normalization

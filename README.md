**Languages / 语言选择:** [简体中文](#简体中文) | [English](#english)

---

## 简体中文

# OpenKTV

OpenKTV 是一款基于 .NET 8 和 WinUI 3 开发的现代化卡拉OK Windows应用程序。它提供了完整的卡拉OK体验，支持双音轨音频和音乐库管理。

## 功能特性

- **歌曲库管理**：自动扫描和整理您的卡拉OK收藏
- **双音轨音频支持**：在人声和伴奏音轨间切换 (Ctrl+V)
- **智能搜索**：支持中文歌曲拼音首字母搜索
- **队列管理**：支持拖拽的播放队列，带有可视化播放指示器
- **音量标准化**：基于FFmpeg的响度分析
- **可定制元数据**：灵活的关键词格式解析（艺术家、歌曲、语言、流派、备注）
- **VLC驱动播放**：支持多种格式（MP3、MP4、MKV、AVI、MPG、RMVB等）

## 系统要求

- Windows 10 或更高版本（64位）
- .NET 8 桌面运行时（框架依赖版本需要）或
- 自包含版本（无需先决条件）

## 安装

### 自包含版本（推荐）
1. 下载并解压发布包
2. 运行 `OpenKTV.exe`
3. 无需安装！

### 框架依赖版本
1. 安装 [.NET 8 桌面运行时](https://dotnet.microsoft.com/download/dotnet/8.0)
2. 下载并解压框架依赖发布包
3. 运行 `OpenKTV.exe`

## 从源码构建

### 先决条件
- .NET 8 SDK
- Visual Studio 2022（推荐）或 Visual Studio Code
- Windows 10 SDK (10.0.19041.0)

### 构建说明

**自包含发布：**
```batch
build-release.bat
```

**框架依赖发布：**
```batch
build-release-framework.bat
```

或直接使用PowerShell：
```powershell
.\build-release.ps1
.\build-release-framework.ps1 -OutputDir "my-custom-folder"
```

## 键盘快捷键

### 主窗口
- `Ctrl+S`：打开音乐库设置
- `Ctrl+V`：切换人声音轨
- `Ctrl+N`：下一首歌
- `Ctrl+R`：重复/重新播放当前歌曲
- `Enter`：播放选中歌曲 / 添加歌曲到队列
- `Ctrl+↑`：将选中歌曲在队列中上移一位
- `Ctrl+↓`：将选中歌曲在队列中下移一位
- `Ctrl+←`：将选中歌曲移到下一首播放（当前歌曲之后）
- `Ctrl+→`：将选中歌曲移到队列末尾
- `Ctrl+D`：从队列中移除
- `Ctrl++`：音量增加5%
- `Ctrl+-`：音量减少5%
- `Ctrl+F`：切换全屏
- `Space`：暂停/继续

### 播放器窗口
- `F11`：切换全屏
- `Ctrl+F`：切换全屏
- `Space`：暂停/继续
- `Escape`：退出全屏
- `Ctrl+N`：下一首歌
- `Ctrl+R`：重复/重新播放当前歌曲
- `Ctrl+V`：切换人声音轨
- `Right Arrow`：音量增加（+5%）
- `Left Arrow`：音量减少（-5%）

## 项目结构

```
OpenKTV/
├── src/
│   ├── UI/Karaoke.UI/              # WinUI 3 应用程序
│   ├── Library/Karaoke.Library/     # 音乐库管理和数据库
│   ├── Player/Karaoke.Player/       # VLC播放和录制
│   └── Common/Karaoke.Common/       # 共享模型和工具
├── tests/                           # 单元测试
├── build-release.ps1                # 自包含构建脚本
├── build-release-framework.ps1      # 框架依赖构建脚本
└── OpenKTV.sln                      # 解决方案文件
```

## 配置

### 音乐库设置
按 `Ctrl+S` 进行配置：
- **音乐库文件夹**：添加包含卡拉OK文件的文件夹
- **关键词格式**：自定义文件名解析方式（如 `艺术家-歌曲-语言-流派`）
- **音量标准化**：启用/禁用FFmpeg响度分析
- **驱动器覆盖**：在不重新扫描的情况下重新映射驱动器号（如将 `D:\Music` 改为 `E:\Music`）
  - 在将音乐库移动到不同驱动器或使用外部/网络驱动器时有用
  - 格式：输入带冒号的驱动器号（如 `E:` 或 `F:`）
  - **注意**：需要重启应用才能生效

### 首次运行
首次启动时，OpenKTV会创建：
- `data/library.db` - 用于歌曲库的SQLite数据库
- `config/settings.json` - 应用程序设置
- 用户应该打开"音乐库设置"页面（`ctrl+s` 或点击"设置"）将音乐文件导入数据库

## 支持的文件格式

- 音频：MP3、WAV
- 视频：MP4、MKV、AVI、MPG、MPEG、RMVB、RM、DAT

## 致谢

- 使用 [LibVLCSharp](https://github.com/videolan/libvlcsharp) 进行媒体播放
- 使用 [NAudio](https://github.com/naudio/NAudio) 进行录制
- 基于 [WinUI 3](https://microsoft.github.io/microsoft-ui-xaml/) 构建
- FFmpeg 用于音量标准化
- Claude code
- codex cli
- gemini cli
- Qwen cli
- copilot cli

---

## English

# OpenKTV

OpenKTV is a modern karaoke application for Windows, built with .NET 8 and WinUI 3. It provides a comprehensive karaoke experience with dual-track audio support and library management.

## Features

- **Song Library Management**: Automatically scan and organize your karaoke collection
- **Dual-Track Audio Support**: Switch between vocal and instrumental tracks (Ctrl+V)
- **Smart Search**: Pinyin initials support for Chinese songs
- **Queue Management**: Drag-and-drop queue with visual playback indicators
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
- `Ctrl+S`: Open Library Settings
- `Ctrl+V`: Toggle Vocal track
- `Ctrl+N`: Next song
- `Ctrl+R`: Repeat/Restart current song
- `Enter`: Play selected song / Queue song
- `Ctrl+↑`: Move UpMove the selected song one position up in the queue
- `Ctrl+↓`: Move the selected song one position down in the queue
- `Ctrl+←` : Move the selected song to play next (after current song)
- `Ctrl+→` : Move the selected song to the bottom of the queue
- `Ctrl+D`: Remove from queue
- `Ctrl++`: Volume up by 5%
- `Ctrl+-`: Volume down by 5%
- `Ctrl+F`: Toggle Fullscreen`Space`: Pause/Resume
- `Space`: Pause/Resume

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
Press `Ctrl+S` to configure:
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
- user should open the "Library setting" page (`ctrl+s` or click "Setting"") to import the music files into the database.

## Supported File Formats

- Audio: MP3, WAV
- Video: MP4, MKV, AVI, MPG, MPEG, RMVB, RM, DAT

## Acknowledgments

- Built with [LibVLCSharp](https://github.com/videolan/libvlcsharp) for media playbook
- Uses [NAudio](https://github.com/naudio/NAudio) for recording
- Powered by [WinUI 3](https://microsoft.github.io/microsoft-ui-xaml/)
- FFmpeg for volume normalization
- Claude code
- codex cli
- gemini cli
- Qwen cli
- copilot cli

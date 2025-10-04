# OpenKTV - Release Build Script
# This script builds a self-contained release package ready for distribution

param(
    [string]$OutputDir = "release/OpenKTV",
    [switch]$SkipClean
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "OpenKTV Release Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean release directory
if (-not $SkipClean) {
    Write-Host "Step 1: Cleaning release directory..." -ForegroundColor Yellow
    if (Test-Path $OutputDir) {
        Remove-Item -Path $OutputDir -Recurse -Force
        Write-Host "  ✓ Removed existing release folder" -ForegroundColor Green
    }
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-Host "  ✓ Created fresh release folder" -ForegroundColor Green
    Write-Host ""
}

# Step 2: Build the application
Write-Host "Step 2: Building application (Release/x64)..." -ForegroundColor Yellow
Write-Host "  Note: Self-contained build may take a few minutes..." -ForegroundColor Cyan
Write-Host ""

# Run dotnet publish with visible output
& dotnet publish src/UI/Karaoke.UI/Karaoke.UI.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:Platform=x64 `
    -p:WindowsAppSDKSelfContained=true `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=false

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  ✗ Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "  ✓ Build completed successfully" -ForegroundColor Green
Write-Host ""

# Step 3: Copy files to release directory
Write-Host "Step 3: Copying files to release directory..." -ForegroundColor Yellow
$publishDir = "src/UI/Karaoke.UI/bin/x64/Release/net8.0-windows10.0.19041.0/win-x64/publish"

if (-not (Test-Path $publishDir)) {
    Write-Host "  ✗ Publish directory not found: $publishDir" -ForegroundColor Red
    exit 1
}

Copy-Item -Path "$publishDir/*" -Destination $OutputDir -Recurse -Force
Write-Host "  ✓ Files copied successfully" -ForegroundColor Green
Write-Host ""

# Step 4: Remove debug symbols
Write-Host "Step 4: Removing debug symbols (*.pdb files)..." -ForegroundColor Yellow
$pdbFiles = Get-ChildItem -Path $OutputDir -Filter "*.pdb" -Recurse
$pdbCount = $pdbFiles.Count
if ($pdbCount -gt 0) {
    $pdbFiles | Remove-Item -Force
    Write-Host "  ✓ Removed $pdbCount debug symbol files" -ForegroundColor Green
} else {
    Write-Host "  ✓ No debug symbols found" -ForegroundColor Green
}
Write-Host ""

# Step 5: Create/update settings.json with default configuration
Write-Host "Step 5: Configuring settings.json..." -ForegroundColor Yellow
$settingsPath = Join-Path $OutputDir "config/settings.json"
$settingsContent = @"
{
  "Library": {
    "DefaultPriority": 2,
    "DefaultChannel": "Stereo",
    "DatabasePath": "data/library.db",
    "SupportedExtensions": [
      ".mp3",
      ".wav",
      ".mp4",
      ".mkv",
      ".dat",
      ".avi",
      ".mpg",
      ".rmvb",
      ".rm",
      ".mpeg"
    ]
  }
}
"@

New-Item -ItemType Directory -Path (Join-Path $OutputDir "config") -Force | Out-Null
Set-Content -Path $settingsPath -Value $settingsContent -Force
Write-Host "  ✓ Created default settings.json" -ForegroundColor Green
Write-Host ""

# Step 6: Create README.txt
Write-Host "Step 6: Creating README.txt..." -ForegroundColor Yellow
$readmePath = Join-Path $OutputDir "README.txt"
$readmeContent = @"
Karaoke Application - Release Version
=====================================

This is a self-contained portable version of the Karaoke application.

System Requirements:
- Windows 10 or later (64-bit)
- No additional software installation required

How to Use:
1. Copy this entire folder to any location on your PC
2. Double-click "OpenKTV.exe" to start the application
3. On first run, the application will create:
   - data/library.db (song database)
   - config/settings.json (configuration file)

Getting Started:
1. Launch the application
2. Press Ctrl+T to open Library Settings
3. Click "Add Folder" to add your music/video folders
4. Click "Save" to scan and import your songs
5. Browse and play your karaoke songs!

Keyboard Shortcuts:
Main Window:
- Ctrl+T: Open Library Settings
- Ctrl+F: Focus on Search box
- Ctrl+E: Toggle Recording (record system audio to record folder)
- Ctrl+V: Toggle Vocal track
- Ctrl+N: Next song
- Ctrl+R: Repeat/Restart current song
- F5: Refresh library
- Enter: Play selected song / Queue song
- Delete: Remove from queue

Player Window:
- F11 / Ctrl+F: Toggle Fullscreen
- Space: Pause/Resume
- Escape: Exit fullscreen
- Ctrl+N: Next song
- Ctrl+R: Repeat/Restart
- Ctrl+V: Toggle Vocal
- Right Arrow: Volume up (+5%)
- Left Arrow: Volume down (-5%)

Features:
- Automatic song library management
- Dual-track audio support (vocals/instrumental)
- Queue management with drag-and-drop
- Search and filter by artist, title, language, genre, comment
- Volume normalization with ffmpeg
- Recording support
- Customizable keyword format for file parsing
- Drive override to remap library paths without rescanning

Notes:
- The "record" folder will be created when you start recording
- All user data is stored in the "data" and "config" folders
- You can safely move this folder to another PC
- Drive Override changes require app restart to take effect

For more information, visit: https://github.com/zhanghe9704/karaoke
"@

Set-Content -Path $readmePath -Value $readmeContent -Force
Write-Host "  ✓ Created README.txt" -ForegroundColor Green
Write-Host ""

# Step 7: Calculate release size
Write-Host "Step 7: Calculating release package size..." -ForegroundColor Yellow
$totalSize = (Get-ChildItem -Path $OutputDir -Recurse | Measure-Object -Property Length -Sum).Sum
$sizeMB = [math]::Round($totalSize / 1MB, 2)
Write-Host "  ✓ Total size: $sizeMB MB" -ForegroundColor Green
Write-Host ""

# Step 8: Verify essential files
Write-Host "Step 8: Verifying essential files..." -ForegroundColor Yellow
$essentialFiles = @(
    "OpenKTV.exe",
    "config/settings.json",
    "README.txt",
    "libvlc/win-x64/libvlc.dll"
)

$allPresent = $true
foreach ($file in $essentialFiles) {
    $filePath = Join-Path $OutputDir $file
    if (Test-Path $filePath) {
        Write-Host "  ✓ $file" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $file (MISSING!)" -ForegroundColor Red
        $allPresent = $false
    }
}

if (-not $allPresent) {
    Write-Host ""
    Write-Host "Build completed with missing files!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Release Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output Directory: $OutputDir" -ForegroundColor White
Write-Host "Total Size: $sizeMB MB" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Test the application by running: $OutputDir/OpenKTV.exe" -ForegroundColor White
Write-Host "2. Zip the '$OutputDir' folder for distribution" -ForegroundColor White
Write-Host "3. Users can extract and run on any Windows 10+ (64-bit) PC" -ForegroundColor White
Write-Host ""

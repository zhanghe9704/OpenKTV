# Karaoke Application - Build Versions

This project provides two release build options:

## 1. Self-Contained Build (Recommended for General Distribution)

**Scripts:**
- `build-release.ps1` / `build-release.bat`

**Output:** `release/Karaoke/`

**Characteristics:**
- ✅ Includes .NET 8 runtime (~435 MB)
- ✅ No installation required
- ✅ Works on any Windows 10+ (64-bit) PC
- ✅ Users just extract and run
- ❌ Larger download size

**Best for:**
- General public distribution
- Users who don't have .NET installed
- Maximum compatibility
- Simple "extract and run" experience

**Usage:**
```powershell
.\build-release.ps1
# or double-click
build-release.bat
```

---

## 2. Framework-Dependent Build (Smaller Size)

**Scripts:**
- `build-release-framework.ps1` / `build-release-framework.bat`

**Output:** `release/Karaoke-Framework/`

**Characteristics:**
- ✅ Much smaller size (~100 MB)
- ✅ Uses system .NET runtime
- ✅ Automatic updates via Windows Update
- ❌ Requires .NET 8 Desktop Runtime installed
- ❌ Extra step for users

**Best for:**
- Users who already have .NET 8 installed
- Organizations with .NET runtime deployed
- Bandwidth-limited scenarios
- Technical users

**Requirements:**
- .NET 8 Desktop Runtime
- Download: https://dotnet.microsoft.com/download/dotnet/8.0

**Usage:**
```powershell
.\build-release-framework.ps1
# or double-click
build-release-framework.bat
```

---

## Size Comparison

| Version | Approximate Size | .NET Runtime |
|---------|-----------------|--------------|
| Self-Contained | ~435 MB | ✅ Included |
| Framework-Dependent | ~100 MB | ❌ Requires separate install |

---

## Build Script Parameters

Both scripts support these parameters:

```powershell
# Custom output directory
.\build-release.ps1 -OutputDir "my-custom-folder"

# Skip cleaning (faster rebuilds)
.\build-release.ps1 -SkipClean

# Combine parameters
.\build-release-framework.ps1 -OutputDir "dist" -SkipClean
```

---

## Build Process

Both scripts perform the same steps:

1. ✅ Clean previous builds (unless `-SkipClean`)
2. ✅ Build application in Release mode
3. ✅ Copy all necessary files
4. ✅ Remove debug symbols (*.pdb)
5. ✅ Create clean `config/settings.json`
6. ✅ Generate `README.txt` for users
7. ✅ Verify essential files
8. ✅ Calculate package size

---

## Recommendation

**For most users:** Use the **Self-Contained Build**
- No prerequisites
- Simple distribution
- Works everywhere

**For advanced users:** Use the **Framework-Dependent Build**
- Smaller download
- Leverages system .NET runtime
- Better for enterprise deployment

---

## Distribution Checklist

After building:

- [ ] Test the application on a clean Windows machine
- [ ] Verify all features work correctly
- [ ] Zip the release folder
- [ ] Include README.txt in distribution
- [ ] For framework-dependent: Include .NET 8 installation instructions
- [ ] Test on target platforms (Windows 10, Windows 11)

---

## Quick Start

**Self-Contained (recommended):**
```batch
build-release.bat
```

**Framework-Dependent:**
```batch
build-release-framework.bat
```

Both builds will be in the `release/` directory.

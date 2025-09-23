# Environment Checklist

This checklist captures the verified development environment for Stage 0 and serves as onboarding guidance for new contributors.

## Tooling Versions
- dotnet SDK: 8.0.317 (pinned via `global.json`), 9.0.304 installed for future compatibility.
- dotnet-format: 5.1.250801+4a851ea9707f87d381166c2fc2b2d4b58a10a222
- FFmpeg: 2025-09-15-git-16b8a7805b-full_build-www.gyan.dev
- 7-Zip CLI: 16.02 (7z.dll located in `C:\Program Files\7-Zip`)
- Visual Studio: Community 2022 (17.14.13) with Windows App SDK workloads installed.

## Verification Commands
Run the following commands to confirm the toolchain on a new machine. Capture the output and compare it with the expected ranges above.

```powershell
# .NET SDKs and runtime info
dotnet --info

# Global tool verification
dotnet tool list -g

# Formatter version
dotnet-format --version

# Media tooling
ffmpeg -version
7z i

# Visual Studio workloads (requires vswhere)
& "$env:ProgramFiles(x86)\Microsoft Visual Studio\Installer\vswhere.exe" -products * -requires Microsoft.VisualStudio.ComponentGroup.WindowsAppSDK.Cs -property installationPath
```

## Visual Studio Workloads
- Confirm the following components in the Visual Studio Installer before continuing:
  - Desktop development with C#
  - Windows App SDK (WinUI 3) for C#
  - Optional: UWP development tools and C++ Windows App SDK components (needed for certain WinUI dependencies)
- Instance ID validated: `97650a31` (`C:\ProgramData\Microsoft\VisualStudio\Packages\_Instances\97650a31`).

## Configuration Artifacts
- `global.json` pins the repo to .NET SDK 8.0.317 with roll-forward to the latest patch.
- `dev_docs/environment_checklist.md` (this file) records the current baseline. Update version pins if tooling changes.

## Sample Assets and Solution Execution
- Sample fixtures under `assets/sample/` will be introduced in Stage 3. For now, ensure the folder exists but remains empty in source control.
- Once Stage 2 scaffolds the solution, developers should verify:
  - `dotnet restore`
  - `dotnet build Karaoke.sln -c Debug`
  - `dotnet run --project src/UI/Karaoke.UI.csproj`
- Until the solution exists, these commands will fail; re-run them after Stage 2 completes.

## Outstanding Items
- Mirror this setup on any CI agent image before Stage 2.
- Re-validate tool versions quarterly or when new SDK patches are released.
- Document any platform-specific troubleshooting tips encountered by the team.
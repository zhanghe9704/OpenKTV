@echo off
REM OpenKTV - Release Build Script
REM This batch file runs the PowerShell build script

echo.
echo ========================================
echo OpenKTV Release Builder
echo ========================================
echo.

REM Check if PowerShell is available
where powershell >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: PowerShell is not found on this system
    echo Please install PowerShell to run this build script
    pause
    exit /b 1
)

REM Run the PowerShell build script
echo Running build script...
echo.

powershell.exe -ExecutionPolicy Bypass -File "%~dp0build-release.ps1"

if %errorlevel% neq 0 (
    echo.
    echo Build failed with error code %errorlevel%
    pause
    exit /b %errorlevel%
)

echo.
echo Build completed successfully!
echo.
pause

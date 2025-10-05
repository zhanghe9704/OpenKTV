@echo off
REM OpenKTV - Framework-Dependent Release Build Script
REM This batch file runs the PowerShell build script for framework-dependent build

echo.
echo ========================================
echo OpenKTV Release Builder
echo (Framework-Dependent - Requires .NET 8)
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

powershell.exe -ExecutionPolicy Bypass -File "%~dp0build-release-framework.ps1"

if %errorlevel% neq 0 (
    echo.
    echo Build failed with error code %errorlevel%
    pause
    exit /b %errorlevel%
)

echo.
echo Build completed successfully!
echo.
echo IMPORTANT: This build requires .NET 8 Desktop Runtime!
echo Download: https://dotnet.microsoft.com/download/dotnet/8.0
echo.
pause

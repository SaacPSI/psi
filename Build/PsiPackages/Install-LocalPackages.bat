@echo off
REM Batch script to launch the PowerShell script for local NuGet packages cleaning

echo.
echo Local NuGet packages installation...
echo.

REM Check if PowerShell is available
where powershell >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: PowerShell is not available on this system.
    pause
    exit /b 1
)

REM Execute the PowerShell script
powershell -ExecutionPolicy Bypass -File "%~dp0Install-LocalPackages.ps1"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Installation failed.
    pause
    exit /b %ERRORLEVEL%
)

echo.
pause

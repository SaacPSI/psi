# Script to reinstall local NuGet packages from Build/PsiPackages
# This script removes packages from the local NuGet cache and reinstalls them from this folder

param(
    [switch]$Force
)

# Define colors for messages
function Write-Success {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Cyan
}

function Write-Warning {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Red
}

# Verify we are in the correct directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$packagesPath = $scriptPath

Write-Info "==================================================="
Write-Info "Local NuGet Packages Cleaning"
Write-Info "==================================================="
Write-Info ""

# Get all .nupkg files
$nupkgFiles = Get-ChildItem -Path $packagesPath -Filter "*.nupkg"

if ($nupkgFiles.Count -eq 0) {
    Write-Warning "No .nupkg files found in folder $packagesPath"
    exit 1
}

Write-Info "Found $($nupkgFiles.Count) package(s) to install"
Write-Info ""

# Get the global NuGet cache path
$nugetCachePath = Join-Path $env:USERPROFILE ".nuget\packages"
Write-Info "NuGet cache path: $nugetCachePath"
Write-Info ""

# Extract package names and versions from file names
$packagesToClean = @()
foreach ($nupkg in $nupkgFiles) {
    # Expected format: PackageName.Version.nupkg
    $filename = $nupkg.Name
    if ($filename -match '^(.+?)\.(\d+\.\d+\.\d+\.\d+.*?)\.nupkg$') {
        $packageName = $matches[1]
        $packageVersion = $matches[2]
        $packagesToClean += @{
            Name = $packageName
            Version = $packageVersion
            File = $nupkg.FullName
        }
    }
}

#Clean the local NuGet cache
Write-Info "Cleaning local NuGet cache"
Write-Info "========================================="

foreach ($package in $packagesToClean) {
    $packageCachePath = Join-Path $nugetCachePath $package.Name.ToLower()

    if (Test-Path $packageCachePath) {
        Write-Info "Removing $($package.Name) from cache..."
        try {
            Remove-Item -Path $packageCachePath -Recurse -Force -ErrorAction Stop
            Write-Success " $($package.Name) removed successfully"
        }
        catch {
            Write-Error " Error removing $($package.Name): $_"
        }
    }
    else {
        Write-Info " $($package.Name) is not in cache"
    }
}

Write-Info ""
Write-Success "==================================================="
Write-Success "Cleaning completed!"
Write-Success "==================================================="
Write-Info ""
Write-Warning "Note: You need to restore NuGet packages in your projects."
Write-Info "      Use 'dotnet restore' or 'nuget restore' in your solution."
Write-Info ""

# Local NuGet Packages Cleaning

This folder contains scripts to reinstall local NuGet packages from this directory.

## Usage

### Method 1: Double-click (Windows)

1. Double-click on `Install-LocalPackages.bat`
2. The script will automatically remove existing packages from the NuGet cache (`%USERPROFILE%\.nuget\packages`)


### Method 2: PowerShell

```powershell
# From the Build/PsiPackages folder
.\Install-LocalPackages.ps1
```

### Method 3: From another directory

```powershell
# From any directory
& "Build\PsiPackages\Install-LocalPackages.ps1"
```

## What does the script do?

**NuGet cache cleanup**
   - Scans all `.nupkg` files in the folder
   - Removes corresponding packages from the local cache (`%USERPROFILE%\.nuget\packages`)

## After installation

After running the script, you will need to restore packages in your projects:

```bash
# For a .NET solution
dotnet restore

# Or with NuGet
nuget restore YourSolution.sln
```

## Troubleshooting

### "Execution Policy" Error
If you get an execution policy error, open PowerShell as administrator and run:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Packages still cached
If old packages are still being used, try:
```bash
dotnet nuget locals all --clear
```

## Included packages

This folder contains custom NuGet packages for the Microsoft Psi project:
- Microsoft.Psi.Runtime
- Microsoft.Psi.Audio
- Microsoft.Psi.Media
- Microsoft.Psi.Imaging
- And other packages from the Psi ecosystem

## Notes

- The script requires write permissions on `%USERPROFILE%\.nuget\packages`
- Packages are identified by their filename (format: `PackageName.Version.nupkg`)
- The script may take a few minutes depending on the number of packages

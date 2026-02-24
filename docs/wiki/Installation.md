# Installation Guide

This guide will help you set up the SAAC development environment and build the project.

## Prerequisites

### Required Software

1. **Visual Studio 2022 or 2026**
   - Workload: .NET desktop development
   - Workload: Desktop development with C++
   - Component: .NET Framework 4.8 SDK and targeting pack
   - Component: ATl build tools and librairies

2. **Git**
   - For cloning repositories

3. **Windows 10/11**
   - Required for WPF applications and some components

### Optional Software

- **PsiStudio** - For visualizing and analyzing Psi data stores
- **Unity** - If using Unity integration components

## Step-by-Step Installation

### 1. Clone the Psi Repository

SAAC depends on a modified fork of Microsoft Psi.

```bash
# Navigate to your workspace
cd C:\Dev

# Clone the SAAC fork of Psi
git clone https://github.com/SaacPSI/psi.git
cd psi

# Switch to PsiStudio branch
git checkout PsiStudio
```

### 2. Build Psi

```bash
cd psi

# Build the solution
# Option 1: Using Visual Studio
# Open Sources\Psi\Psi.sln in Visual Studio
# Build the solution (Ctrl+Shift+B)

# Option 2: Using MSBuild from command line
msbuild Sources\Psi\Psi.sln /p:Configuration=Release
```

The build will output NuGet packages to `builds/PsiPackages`.

### 3. Configure NuGet Package Source

Add the Psi package output directory to your NuGet sources:

**Option A: Using Visual Studio**
1. Tools -> NuGet Package Manager -> Package Manager Settings
2. Select "Package Sources"
3. Click the "+" button to add a new source
4. Name: `Psi Local Packages`
5. Source: `C:\Dev\psi\builds\PsiPackages` (adjust path as needed)
6. Click "Update" then "OK"

**Option B: Using NuGet CLI**
```bash
nuget sources add -Name "PsiLocalPackages" -Source "C:\Dev\psi\builds\PsiPackages"
```

**Option C: Using dotnet CLI**
```bash
dotnet nuget add source "C:\Dev\psi\builds\PsiPackages" --name "PsiLocalPackages"
```

### 4. Clone SAAC Repository

```bash
# Navigate to your workspace
cd C:\Dev

# Clone SAAC repository
git clone https://github.com/SaacPSI/saac.git
cd saac
```

### 5. Restore NuGet Packages

```bash
# From the saac directory
nuget restore SAAC.sln

# Or using Visual Studio
# Open SAAC.sln
# Right-click solution in Solution Explorer
# Click "Restore NuGet Packages"
```

### 6. Build SAAC

```bash
# Option 1: Using Visual Studio
# Open SAAC.sln
# Build Solution (Ctrl+Shift+B)

# Option 2: Using MSBuild
msbuild SAAC.sln /p:Configuration=Debug

# Or for Release build
msbuild SAAC.sln /p:Configuration=Release
```

### 7. Verify Installation

Check that key applications build successfully:

1. **ServerApplication**: `build\bin\ServerApplication\Debug\ServerApplication.exe`
2. **VideoRemoteApp**: `build\bin\VideoRemoteApp\Debug\VideoRemoteApp.exe`
3. **CameraRemoteApp**: `build\bin\CameraRemoteApp\Debug\CameraRemoteApp.exe`
4. **WhisperRemoteApp**: `build\bin\WhisperRemoteApp\Debug\WhisperRemoteApp.exe`

## Component-Specific Setup

### For Kinect Azure Support

1. Install Kinect Azure SDK
   - Download from [Microsoft](https://docs.microsoft.com/en-us/azure/kinect-dk/sensor-sdk-download)
   - Install to default location

### For OpenFace Support

1. OpenFace dependencies are in `Dependencies\OpenFace`
2. Native interop is in `Interop\OpenFaceInterop`
3. Ensure DLLs are copied to output directory (handled by build scripts)

### For Nuitrack Support

1. Install Nuitrack SDK from [Nuitrack website](https://nuitrack.com/)
2. Set environment variable: `NUITRACK_HOME`

### For Whisper Support

The Whisper components use pre-built models. No additional setup required unless using custom models.

## Common Installation Issues

### Issue: NuGet Package Not Found

**Error:** `Unable to find package 'Microsoft.Psi.Runtime'`

**Solution:**
1. Verify Psi NuGet source is correctly configured
2. Check that Psi was built successfully
3. Verify packages exist in `psi\builds\PsiPackages`
4. Clear NuGet cache: `nuget locals all -clear`
5. Restore packages again

### Issue: Build Errors in Interop Projects

**Error:** Native library linking errors

**Solution:**
1. Ensure C++ workload is installed in Visual Studio
2. Check that Windows SDK is installed
3. Verify native dependencies are in `Dependencies` folder
4. For specific interops, check the README in the Interop subfolder

### Issue: Missing .NET Framework 4.8

**Error:** `Project targets framework '.NETFramework,Version=v4.8' which is not installed`

**Solution:**
1. Open Visual Studio Installer
2. Modify your Visual Studio installation
3. Under "Individual components", search for ".NET Framework 4.8"
4. Install ".NET Framework 4.8 SDK" and ".NET Framework 4.8 targeting pack"

### Issue: PsiStudio Won't Open Stores

**Error:** Compatibility issues with stores created by SAAC

**Solution:**
1. Ensure you're using PsiStudio from the SAAC Psi fork
2. Build PsiStudio from `psi\Sources\PsiStudio`
3. Use the PsiStudio from `psi\Sources\PsiStudio\bin\Debug` or `bin\Release`
4. Make sure you have the visualizations components lodaded in [PsiStudio](https://github.com/microsoft/psi/wiki/3rd-Party-Visualizers).  

## Updating the Installation

### Update Psi

```bash
cd C:\Dev\psi
git pull origin PsiStudio
msbuild Sources\Psi\Psi.sln /p:Configuration=Release
```

After rebuilding Psi, clean and rebuild SAAC:

```bash
cd C:\Dev\saac
msbuild SAAC.sln /t:Clean
msbuild SAAC.sln /p:Configuration=Debug
```

### Update SAAC

```bash
cd C:\Dev\saac
git pull origin main  # or your working branch

# Restore packages (in case of new dependencies)
nuget restore SAAC.sln

# Rebuild
msbuild SAAC.sln /p:Configuration=Debug
```
 
## Folder Structure After Installation

```
C:\Dev\
├── psi\                          # Microsoft Psi fork
│   ├── Sources\
│   │   ├── Psi\                 # Core Psi
│   │   └── PsiStudio\           # PsiStudio application
│   └── builds\
│       └── PsiPackages\         # NuGet packages output
│
└── saac\                         # SAAC framework
    ├── Applications\
    │   ├── VideoRemoteApp\
    │   ├── CameraRemoteApp\
    │   ├── WhisperRemoteApp\
    │   └── ServerApplication\
    ├── Components\
    │   ├── PipelineServices\
    │   ├── AudioRecording\
    │   └── ...
    ├── Interop\
    │   ├── OpenFaceInterop\
    │   ├── BiopacInterop\
    │   └── ...
    └── Dependencies\             # Third-party native libraries
        ├── OpenFace\
        ├── Nuitrack\
        └── ...
```

## Getting Help

If you encounter issues not covered here:

1. Review component-specific README files
2. Open an issue on the [GitHub repository](https://github.com/SaacPSI/saac/issues)
3. Contact the maintainers (see [Home](Home.md))

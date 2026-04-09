# Setup-DevSymlinks.ps1
# Optional script for developers who want to use symbolic links for shared files
# Other users are not required to run this script

param(
    [switch]$Remove,
    [switch]$Force
)

# Requires Administrator privileges on Windows (or Developer Mode enabled on Windows 10/11)
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
$isDeveloperMode = $false

# Check if Developer Mode is enabled (Windows 10/11)
try {
    $regPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock"
    $devMode = Get-ItemProperty -Path $regPath -Name "AllowDevelopmentWithoutDevLicense" -ErrorAction SilentlyContinue
    $isDeveloperMode = $devMode.AllowDevelopmentWithoutDevLicense -eq 1
} catch {
    # Registry key doesn't exist or can't be read
}

if (-not $isAdmin -and -not $isDeveloperMode) {
    Write-Warning "This script requires either:"
    Write-Warning "  1. Administrator privileges, or"
    Write-Warning "  2. Developer Mode enabled (Windows 10/11: Settings > Update & Security > For Developers > Developer mode)"
    Write-Warning ""
    Write-Warning "To enable Developer Mode:"
    Write-Warning "  - Windows 10/11: Settings > Privacy & security > For developers > Developer mode"
    Write-Warning ""
    
    if (-not $Force) {
        $response = Read-Host "Do you want to continue anyway? (y/n)"
        if ($response -ne 'y') {
            exit 1
        }
    }
}

# Configuration: Define symlinks as hashtable
# Key: Symlink path (relative to repo root)
# Value: Target path (relative to repo root)
$symlinks = @{
    "Applications\VideoRemoteApp\UiGenerator.cs" = "Applications\CameraRemoteApp\UiGenerator.cs"
    "Applications\WhisperRemoteApp\UiGenerator.cs" = "Applications\CameraRemoteApp\UiGenerator.cs"
    "Applications\ServerApplication\UiGenerator.cs" = "Applications\CameraRemoteApp\UiGenerator.cs"
}

# Get repository root (assuming script is in scripts folder)
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptPath

Write-Host "Repository root: $repoRoot" -ForegroundColor Cyan
Write-Host ""

$successCount = 0
$errorCount = 0

if ($Remove) {
    # Remove symlinks
    Write-Host "Removing symbolic links..." -ForegroundColor Yellow
    Write-Host ""
    
    foreach ($symlinkPath in $symlinks.Keys) {
        $fullSymlinkPath = Join-Path $repoRoot $symlinkPath
        
        if (Test-Path $fullSymlinkPath) {
            $item = Get-Item $fullSymlinkPath
            
            # Check if it's a symlink
            if ($item.LinkType -eq "SymbolicLink") {
                try {
                    Remove-Item $fullSymlinkPath -Force
                    Write-Host "[OK] Removed: $symlinkPath" -ForegroundColor Green
                    $successCount++
                } catch {
                    Write-Host "[ERROR] Failed to remove: $symlinkPath" -ForegroundColor Red
                    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
                    $errorCount++
                }
            } else {
                Write-Host "[SKIP] Not a symlink: $symlinkPath" -ForegroundColor Yellow
            }
        } else {
            Write-Host "[SKIP] Does not exist: $symlinkPath" -ForegroundColor Gray
        }
    }
} else {
    # Create symlinks
    Write-Host "Creating symbolic links..." -ForegroundColor Yellow
    Write-Host ""
    
    foreach ($entry in $symlinks.GetEnumerator()) {
        $symlinkPath = $entry.Key
        $targetPath = $entry.Value
        
        $fullSymlinkPath = Join-Path $repoRoot $symlinkPath
        $fullTargetPath = Join-Path $repoRoot $targetPath
        
        # Verify target exists
        if (-not (Test-Path $fullTargetPath)) {
            Write-Host "[ERROR] Target does not exist: $targetPath" -ForegroundColor Red
            $errorCount++
            continue
        }
        
        # Check if symlink already exists
        if (Test-Path $fullSymlinkPath) {
            $item = Get-Item $fullSymlinkPath
            
            if ($item.LinkType -eq "SymbolicLink") {
                $currentTarget = $item.Target
                if ($currentTarget -eq $fullTargetPath) {
                    Write-Host "[SKIP] Symlink already exists: $symlinkPath -> $targetPath" -ForegroundColor Gray
                    continue
                } else {
                    Write-Host "[WARN] Symlink exists but points to different target: $symlinkPath" -ForegroundColor Yellow
                    Write-Host "  Current: $currentTarget" -ForegroundColor Yellow
                    Write-Host "  Expected: $fullTargetPath" -ForegroundColor Yellow
                    
                    if (-not $Force) {
                        $response = Read-Host "  Replace? (y/n)"
                        if ($response -ne 'y') {
                            continue
                        }
                    }
                    
                    Remove-Item $fullSymlinkPath -Force
                }
            } else {
                Write-Host "[WARN] File exists (not a symlink): $symlinkPath" -ForegroundColor Yellow
                
                if (-not $Force) {
                    $response = Read-Host "  Replace with symlink? (y/n)"
                    if ($response -ne 'y') {
                        continue
                    }
                }
                
                # Backup existing file
                $backupPath = "$fullSymlinkPath.backup"
                Move-Item $fullSymlinkPath $backupPath -Force
                Write-Host "  Backed up to: $backupPath" -ForegroundColor Cyan
            }
        }
        
        # Create the symlink
        try {
            New-Item -ItemType SymbolicLink -Path $fullSymlinkPath -Target $fullTargetPath -Force | Out-Null
            Write-Host "[OK] Created: $symlinkPath -> $targetPath" -ForegroundColor Green
            $successCount++
        } catch {
            Write-Host "[ERROR] Failed to create: $symlinkPath" -ForegroundColor Red
            Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
            $errorCount++
        }
    }
}

Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Success: $successCount" -ForegroundColor Green
Write-Host "  Errors: $errorCount" -ForegroundColor $(if ($errorCount -gt 0) { "Red" } else { "Gray" })

if ($errorCount -gt 0) {
    Write-Host ""
    Write-Host "Some operations failed. Common issues:" -ForegroundColor Yellow
    Write-Host "  - Insufficient permissions (try running as Administrator or enable Developer Mode)"
    Write-Host "  - Files are in use (close Visual Studio and other applications)"
    Write-Host "  - Target files don't exist (ensure repo is fully cloned)"
    exit 1
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green

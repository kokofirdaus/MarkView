# MarkView Automated Installer for Windows (x64 and ARM64)
# Run in PowerShell (Standard User or Administrator)

param (
    [switch]$AllUsers = $false
)

$ErrorActionPreference = "Stop"

Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "         MarkView Windows Installer                   " -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan

# 1. Architecture Detection
$arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
Write-Host "Terdeteksi Arsitektur Sistem: $arch" -ForegroundColor Yellow

$sourceExe = ""
if ($arch -eq [System.Runtime.InteropServices.Architecture]::Arm64) {
    $sourceExe = Join-Path $PSScriptRoot "..\dist\MarkView-win-arm64.exe"
    if (-not (Test-Path $sourceExe)) {
        $sourceExe = Join-Path $PSScriptRoot "..\MarkView.exe"
    }
} else {
    $sourceExe = Join-Path $PSScriptRoot "..\dist\MarkView-win-x64.exe"
    if (-not (Test-Path $sourceExe)) {
        $sourceExe = Join-Path $PSScriptRoot "..\MarkView.exe"
    }
}

if (-not (Test-Path $sourceExe)) {
    Write-Error "Berkas executable MarkView tidak ditemukan di: $sourceExe"
    exit 1
}

# 2. Installation Directory
$installDir = ""
if ($AllUsers) {
    $installDir = Join-Path $env:ProgramFiles "MarkView"
} else {
    $installDir = Join-Path $env:LOCALAPPDATA "Programs\MarkView"
}

Write-Host "Menginstal MarkView ke: $installDir" -ForegroundColor Green
New-Item -ItemType Directory -Path $installDir -Force | Out-Null

$targetExe = Join-Path $installDir "MarkView.exe"
Copy-Item -Path $sourceExe -Destination $targetExe -Force
Write-Host "Binary berhasil disalin: $targetExe" -ForegroundColor Green

# 3. Create Shortcuts (Start Menu & Desktop)
$wsh = New-Object -ComObject WScript.Shell

# Start Menu Shortcut
$startMenuDir = if ($AllUsers) {
    [Environment]::GetFolderPath([Environment+SpecialFolder]::CommonStartMenu)
} else {
    [Environment]::GetFolderPath([Environment+SpecialFolder]::StartMenu)
}
$programsDir = Join-Path $startMenuDir "Programs"
$startShortcutPath = Join-Path $programsDir "MarkView.lnk"
$startShortcut = $wsh.CreateShortcut($startShortcutPath)
$startShortcut.TargetPath = $targetExe
$startShortcut.Description = "MarkView - Modern Windows Markdown Viewer & Editor"
$startShortcut.WorkingDirectory = $installDir
$startShortcut.Save()
Write-Host "Pintasan Start Menu dibuat: $startShortcutPath" -ForegroundColor Green

# Desktop Shortcut
$desktopDir = [Environment]::GetFolderPath([Environment+SpecialFolder]::Desktop)
$desktopShortcutPath = Join-Path $desktopDir "MarkView.lnk"
$desktopShortcut = $wsh.CreateShortcut($desktopShortcutPath)
$desktopShortcut.TargetPath = $targetExe
$desktopShortcut.Description = "MarkView - Modern Windows Markdown Viewer & Editor"
$desktopShortcut.WorkingDirectory = $installDir
$desktopShortcut.Save()
Write-Host "Pintasan Desktop dibuat: $desktopShortcutPath" -ForegroundColor Green

# 4. File Associations (.md, .markdown)
try {
    $regRoot = if ($AllUsers) { "HKLM:\Software\Classes" } else { "HKCU:\Software\Classes" }

    # ProgID
    $progId = "MarkView.Document"
    New-Item -Path "$regRoot\$progId" -Force | Out-Null
    Set-ItemProperty -Path "$regRoot\$progId" -Name "(Default)" -Value "Markdown Document" -Force

    # Open Command
    $openCmd = "`"$targetExe`" `"%1`""
    New-Item -Path "$regRoot\$progId\shell\open\command" -Force | Out-Null
    Set-ItemProperty -Path "$regRoot\$progId\shell\open\command" -Name "(Default)" -Value $openCmd -Force

    # Associate .md
    New-Item -Path "$regRoot\.md" -Force | Out-Null
    Set-ItemProperty -Path "$regRoot\.md" -Name "(Default)" -Value $progId -Force

    # Associate .markdown
    New-Item -Path "$regRoot\.markdown" -Force | Out-Null
    Set-ItemProperty -Path "$regRoot\.markdown" -Name "(Default)" -Value $progId -Force

    Write-Host "Asosiasi berkas .md dan .markdown berhasil didaftarkan!" -ForegroundColor Green
} catch {
    Write-Warning "Pendaftaran asosiasi berkas dilewati karena keterbatasan hak akses: $_"
}

Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "    Instalasi MarkView Berhasil Diselesaikan!         " -ForegroundColor Green
Write-Host "======================================================" -ForegroundColor Cyan

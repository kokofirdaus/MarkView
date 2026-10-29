# MarkView Automated Uninstaller for Windows
param (
    [switch]$AllUsers = $false
)

$ErrorActionPreference = "SilentlyContinue"

Write-Host "Menghapus MarkView dari sistem..." -ForegroundColor Yellow

# 1. Paths
$installDir = if ($AllUsers) {
    Join-Path $env:ProgramFiles "MarkView"
} else {
    Join-Path $env:LOCALAPPDATA "Programs\MarkView"
}

# 2. Shortcuts
$startMenuDir = if ($AllUsers) {
    [Environment]::GetFolderPath([Environment+SpecialFolder]::CommonStartMenu)
} else {
    [Environment]::GetFolderPath([Environment+SpecialFolder]::StartMenu)
}
$startShortcutPath = Join-Path $startMenuDir "Programs\MarkView.lnk"
Remove-Item -Path $startShortcutPath -Force

$desktopDir = [Environment]::GetFolderPath([Environment+SpecialFolder]::Desktop)
$desktopShortcutPath = Join-Path $desktopDir "MarkView.lnk"
Remove-Item -Path $desktopShortcutPath -Force

# 3. Registry
$regRoot = if ($AllUsers) { "HKLM:\Software\Classes" } else { "HKCU:\Software\Classes" }
Remove-Item -Path "$regRoot\MarkView.Document" -Recurse -Force

# 4. Remove Files
Remove-Item -Path $installDir -Recurse -Force

Write-Host "MarkView berhasil dihapus dari sistem." -ForegroundColor Green

# Rebuild and reinstall Plant3DCatalogComposer (no Plant 3D restart if APPLOADER reload works)
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "Install-Plant3DCatalogComposer.ps1") -Configuration $Configuration

Write-Host ""
Write-Host "If P3DCOMPOSER still unknown after restart:"
Write-Host "  1. In Plant 3D command line: APPLOADER"
Write-Host "  2. Click Reload (tai load lai bundle)"
Write-Host "  3. Or NETLOAD -> chon DLL trong:"
Write-Host "     $env:APPDATA\Autodesk\ApplicationPlugins\Plant3DCatalogComposer.bundle\Contents\Plant3DCatalogComposer.dll"

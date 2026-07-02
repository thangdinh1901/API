<#
.SYNOPSIS
    Installs Plant3DLineVisibility plugin into the AutoCAD ApplicationPlugins bundle directory.
.DESCRIPTION
    Copies the compiled DLL and PackageContents.xml into
    %AppData%\Autodesk\ApplicationPlugins\Plant3DLineVisibility.bundle
    so that AutoCAD autoloads the plugin on startup.
.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Release.
.PARAMETER SkipBuild
    If set, skips the dotnet build step and only copies files.
#>
param(
    [string]$Configuration = "Release",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$projectDir  = Join-Path $PSScriptRoot "..\Plant3DLineVisibility"
$projectFile = Join-Path $projectDir "Plant3DLineVisibility.csproj"
$bundleDir   = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\Plant3DLineVisibility.bundle"
$contentsDir = Join-Path $bundleDir "Contents"

# ── Build ───────────────────────────────────────────────
if (-not $SkipBuild) {
    Write-Host "Building Plant3DLineVisibility ($Configuration)…" -ForegroundColor Cyan
    dotnet build $projectFile -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }
}

# ── Locate output DLL ──────────────────────────────────
$outDir = Join-Path $projectDir "bin\$Configuration\net8.0-windows"
$dll    = Join-Path $outDir "Plant3DLineVisibility.dll"

if (-not (Test-Path $dll)) {
    throw "DLL not found at $dll — build first."
}

# ── Copy to ApplicationPlugins bundle ──────────────────
Write-Host "Installing to: $bundleDir" -ForegroundColor Yellow

New-Item -ItemType Directory -Path $contentsDir -Force | Out-Null

# Copy PackageContents.xml to bundle root
Copy-Item (Join-Path $projectDir "PackageContents.xml") $bundleDir -Force

# Copy all output files to Contents
Get-ChildItem $outDir -File | ForEach-Object {
    Copy-Item $_.FullName $contentsDir -Force
}

Write-Host "✓ Plant3DLineVisibility installed." -ForegroundColor Green
Write-Host "  Restart AutoCAD Plant 3D, then run P3DLINEVIS." -ForegroundColor Gray

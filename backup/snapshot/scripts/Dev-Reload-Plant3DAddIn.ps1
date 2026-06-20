# Build + copy add-in for quick reload WITHOUT restarting Inventor.
# Prerequisite: unload the add-in in Inventor first (see instructions below).
param(
    [string]$InventorYear = "2026",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$PurgeTemplates
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $root "Plant3DSkeletonManager\Plant3DSkeletonManager.csproj"
$outDir = Join-Path $root "Plant3DSkeletonManager\bin\$Configuration\net8.0-windows"
$addin = Join-Path $root "Plant3DSkeletonManager\Autoload\Plant3DSkeletonManager.addin"
$dllDir = "C:\ProgramData\Autodesk\Inventor $InventorYear\Addins\Plant3DSkeletonManager"

Write-Host "Building $Configuration ..."
dotnet build $proj -c $Configuration -v q -nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

New-Item -ItemType Directory -Force -Path $dllDir | Out-Null

try {
    Copy-Item (Join-Path $outDir "*.dll") $dllDir -Force
    Copy-Item $addin $dllDir -Force
}
catch {
    Write-Host ""
    Write-Host "ERROR: Cannot copy DLL (file locked)." -ForegroundColor Red
    Write-Host "In Inventor, do this first:"
    Write-Host "  1. Close the 'Plant3D Skeleton Manager' dockable panel"
    Write-Host "  2. Tools tab > Add-Ins (or File > Add-Ins)"
    Write-Host "  3. Find 'Plant3D Skeleton Manager' > uncheck Loaded > Apply/OK"
    Write-Host "  4. Run this script again"
    Write-Host ""
    Write-Host "If still locked, restart Inventor once."
    exit 1
}

if ($PurgeTemplates) {
    $templatesDir = Join-Path $dllDir "Templates"
    if (Test-Path $templatesDir) {
        Remove-Item $templatesDir -Recurse -Force
        Write-Host "Purged Templates folder."
    }
}

Write-Host ""
Write-Host "Installed to: $dllDir" -ForegroundColor Green
Write-Host ""
Write-Host "Reload in Inventor (no restart needed):"
Write-Host "  Tools > Add-Ins > Plant3D Skeleton Manager"
Write-Host "  - If unloaded: check Loaded, click OK"
Write-Host "  - If already loaded: uncheck Loaded > OK, then check Loaded > OK"
Write-Host "  Tools > Plant3D > Skeleton Manager to open the panel"
Write-Host ""

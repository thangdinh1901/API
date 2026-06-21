# Export Catalog Builder Excel (.xlsx) from catalog_generator parts (no Plant 3D UI).
param(
    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) "CATA_NUI_export.xlsx"),
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$toolDir = Join-Path $PSScriptRoot "ExportCatalogExcel"
$outFull = [System.IO.Path]::GetFullPath($OutputPath)
$dir = Split-Path $outFull -Parent
if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }

Push-Location $toolDir
dotnet run -c $Configuration --no-launch-profile -- $outFull
$code = $LASTEXITCODE
Pop-Location
exit $code

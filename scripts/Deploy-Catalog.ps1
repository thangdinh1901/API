# Deploy catalog_generator -> Plant 3D CustomScripts (no CAD required).
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$install = Join-Path $PSScriptRoot 'Install-Plant3DCatalogComposer.ps1'
& $install -Configuration $Configuration

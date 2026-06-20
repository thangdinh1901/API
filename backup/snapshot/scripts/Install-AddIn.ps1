# Cài BoxExtrudeAddIn vào Inventor ApplicationPlugins
param(
    [string]$InventorYear = "2026",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $root "BoxExtrudeAddIn\bin\$Configuration\net8.0-windows"
$dll = Join-Path $outDir "BoxExtrudeAddIn.dll"
$addin = Join-Path $root "BoxExtrudeAddIn\Autoload\BoxExtrudeAddIn.addin"

if (-not (Test-Path $dll)) {
    Write-Error "Chua build DLL. Chay: dotnet build -c $Configuration trong thu muc BoxExtrudeAddIn"
}

# Thu muc Addins may-wide cua Inventor (noi PARTsolutions, Navisworks... dang ky)
$addinsDir = "C:\ProgramData\Autodesk\Inventor $InventorYear\Addins"
$dllDir = Join-Path $addinsDir "BoxExtrudeAddIn"
New-Item -ItemType Directory -Force -Path $dllDir | Out-Null

Copy-Item (Join-Path $outDir "*.dll") $dllDir -Force
Copy-Item $addin $dllDir -Force

# Xoa cac ban cai cu khong hoat dong
$oldBundle = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\BoxExtrudeAddIn.bundle"
if (Test-Path $oldBundle) { Remove-Item $oldBundle -Recurse -Force }
$oldUserAddin = Join-Path $env:APPDATA "Autodesk\Inventor $InventorYear\Addins\BoxExtrudeAddIn.addin"
if (Test-Path $oldUserAddin) { Remove-Item $oldUserAddin -Force }
$oldUserDir = Join-Path $env:APPDATA "Autodesk\Inventor $InventorYear\Addins\BoxExtrudeAddIn"
if (Test-Path $oldUserDir) { Remove-Item $oldUserDir -Recurse -Force }

Write-Host "Da cai Add-in vao: $addinsDir"
Write-Host "Khoi dong lai Inventor $InventorYear, vao tab Tools > Box API > Tao Box"

# Cai Plant3DSkeletonManager vao thu muc Addins cua Inventor
param(
    [string]$InventorYear = "2026",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $root "Plant3DSkeletonManager\bin\$Configuration\net8.0-windows"
$dll = Join-Path $outDir "Plant3DSkeletonManager.dll"
$addin = Join-Path $root "Plant3DSkeletonManager\Autoload\Plant3DSkeletonManager.addin"

if (-not (Test-Path $dll)) {
    Write-Error "Chua build DLL. Chay: dotnet build -c $Configuration trong thu muc Plant3DSkeletonManager"
}

$addinsDir = "C:\ProgramData\Autodesk\Inventor $InventorYear\Addins"
$dllDir = Join-Path $addinsDir "Plant3DSkeletonManager"
New-Item -ItemType Directory -Force -Path $dllDir | Out-Null

Copy-Item (Join-Path $outDir "*.dll") $dllDir -Force
Copy-Item $addin $dllDir -Force

# Xoa template cu de add-in tu sinh lai theo geometry moi
$templatesDir = Join-Path $dllDir "Templates"
if (Test-Path $templatesDir) {
    Remove-Item $templatesDir -Recurse -Force
    Write-Host "Da xoa templates cu (se duoc tao lai khi insert primitive)."
}

Write-Host "Da cai Add-in vao: $dllDir"
Write-Host ""
Write-Host "Reload (khong can tat Inventor):"
Write-Host "  1. Tools > Add-Ins > bo tick Loaded o Plant3D Skeleton Manager > OK"
Write-Host "  2. Chay lai script nay (hoac Dev-Reload-Plant3DAddIn.ps1)"
Write-Host "  3. Tools > Add-Ins > tick Loaded > OK"
Write-Host ""
Write-Host "Hoac khoi dong lai Inventor neu DLL bi khoa."

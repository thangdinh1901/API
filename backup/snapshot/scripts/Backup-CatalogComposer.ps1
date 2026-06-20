# Save / restore a full project snapshot (source + catalog_generator, no bin/obj).
# Usage:
#   .\Backup-CatalogComposer.ps1 -Action Save [-Label "before-publish"]
#   .\Backup-CatalogComposer.ps1 -Action Restore
#   .\Backup-CatalogComposer.ps1 -Action Status

param(
    [ValidateSet('Save', 'Restore', 'Status')]
    [string]$Action = 'Save',
    [string]$Label = ''
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path $PSScriptRoot -Parent
$BackupRoot = Join-Path $RepoRoot 'backup'
$SnapshotDir = Join-Path $BackupRoot 'snapshot'
$ManifestPath = Join-Path $BackupRoot 'MANIFEST.json'

$SourceDirs = @(
    'Plant3DCatalogComposer',
    'Plant3DSkeletonManager',
    'catalog_generator',
    'scripts',
    'docs'
)

$SourceFiles = @(
    'README.md',
    'Plant3DCatalogComposer.sln'
)

function Get-RelativeItems {
    param([string]$Root)
    $items = @()
    foreach ($name in $SourceDirs) {
        $p = Join-Path $Root $name
        if (Test-Path $p) { $items += Get-Item $p }
    }
    foreach ($name in $SourceFiles) {
        $p = Join-Path $Root $name
        if (Test-Path $p) { $items += Get-Item $p }
    }
    return $items
}

function Copy-Tree {
    param(
        [string]$From,
        [string]$To
    )
    if (-not (Test-Path $From)) { return }
    $robocopyArgs = @(
        $From, $To,
        '/MIR', '/NFL', '/NDL', '/NJH', '/NJS', '/NC', '/NS', '/NP',
        '/XD', 'bin', 'obj', '__pycache__', '.vs', 'node_modules', 'backup', 'agent-tools',
        '/XF', '*.user', '*.suo', '*.pyc'
    )
    $null = & robocopy @robocopyArgs
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy failed ($LASTEXITCODE): $From -> $To"
    }
}

function Save-Snapshot {
    param([string]$Note)
    if (Test-Path $SnapshotDir) {
        Remove-Item $SnapshotDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $SnapshotDir -Force | Out-Null

    foreach ($name in $SourceDirs) {
        $src = Join-Path $RepoRoot $name
        $dst = Join-Path $SnapshotDir $name
        if (Test-Path $src) {
            Copy-Tree -From $src -To $dst
            Write-Host "Backed up: $name"
        }
    }
    foreach ($name in $SourceFiles) {
        $src = Join-Path $RepoRoot $name
        if (Test-Path $src) {
            $dstDir = Split-Path (Join-Path $SnapshotDir $name) -Parent
            if ($dstDir -and -not (Test-Path $dstDir)) {
                New-Item -ItemType Directory -Path $dstDir -Force | Out-Null
            }
            Copy-Item $src (Join-Path $SnapshotDir $name) -Force
            Write-Host "Backed up: $name"
        }
    }

    $manifest = @{
        createdUtc = (Get-Date).ToUniversalTime().ToString('o')
        createdLocal = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        label = if ($Note) { $Note } else { 'snapshot' }
        repoRoot = $RepoRoot
        sourceDirs = $SourceDirs
    } | ConvertTo-Json -Depth 4

    Set-Content -Path $ManifestPath -Value $manifest -Encoding UTF8
    Write-Host ""
    Write-Host "Snapshot saved -> $SnapshotDir"
    Write-Host "Manifest     -> $ManifestPath"
}

function Restore-Snapshot {
    if (-not (Test-Path $ManifestPath)) {
        throw "No backup found. Run: Backup-CatalogComposer.ps1 -Action Save"
    }
    if (-not (Test-Path $SnapshotDir)) {
        throw "Snapshot folder missing: $SnapshotDir"
    }

    $manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json
    Write-Host "Restoring backup from $($manifest.createdLocal) [$($manifest.label)]"
    Write-Host "Target: $RepoRoot"
    Write-Host ""

    foreach ($name in $SourceDirs) {
        $src = Join-Path $SnapshotDir $name
        $dst = Join-Path $RepoRoot $name
        if (Test-Path $src) {
            Copy-Tree -From $src -To $dst
            Write-Host "Restored: $name"
        }
    }
    foreach ($name in $SourceFiles) {
        $src = Join-Path $SnapshotDir $name
        if (Test-Path $src) {
            Copy-Item $src (Join-Path $RepoRoot $name) -Force
            Write-Host "Restored: $name"
        }
    }

    Write-Host ""
    Write-Host "Restore complete. Rebuild plugin and Deploy Catalog in Plant 3D."
}

function Show-Status {
    if (-not (Test-Path $ManifestPath)) {
        Write-Host "No backup snapshot on disk."
        return
    }
    $manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json
    Write-Host "Backup label : $($manifest.label)"
    Write-Host "Created      : $($manifest.createdLocal)"
    Write-Host "Snapshot dir : $SnapshotDir"
    Write-Host "Exists       : $(Test-Path $SnapshotDir)"
}

switch ($Action) {
    'Save' { Save-Snapshot -Note $Label }
    'Restore' { Restore-Snapshot }
    'Status' { Show-Status }
}

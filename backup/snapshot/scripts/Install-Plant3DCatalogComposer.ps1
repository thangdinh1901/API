# Install Plant3DCatalogComposer into AutoCAD / Plant 3D ApplicationPlugins
param(
    [string]$AcadYear = "2026",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

function Register-Plant3DComposer {
    param(
        [Parameter(Mandatory = $true)][string]$DllPath,
        [string]$Description = "Plant 3D Catalog Composer"
    )

    $productIds = @(
        "ACAD-9117:409",
        "ACAD-9117"
    )

    foreach ($productId in $productIds) {
        $parent = "HKCU:\Software\Autodesk\AutoCAD\R25.1\$productId\Applications"
        if (-not (Test-Path $parent)) { continue }

        $appKey = Join-Path $parent "Plant3DCatalogComposer"
        New-Item -Path $appKey -Force | Out-Null
        New-ItemProperty -Path $appKey -Name "LOADER" -PropertyType ExpandString -Value $DllPath -Force | Out-Null
        New-ItemProperty -Path $appKey -Name "MANAGED" -PropertyType DWord -Value 1 -Force | Out-Null
        New-ItemProperty -Path $appKey -Name "LOADCTRLS" -PropertyType DWord -Value 6 -Force | Out-Null
        New-ItemProperty -Path $appKey -Name "DESCRIPTION" -PropertyType String -Value $Description -Force | Out-Null

        $cmdKey = Join-Path $appKey "Commands"
        New-Item -Path $cmdKey -Force | Out-Null
        Remove-ItemProperty -Path $cmdKey -Name "COMPWRAP" -ErrorAction SilentlyContinue
        New-ItemProperty -Path $cmdKey -Name "P3DCOMPOSER" -PropertyType String -Value "P3DCOMPOSER" -Force | Out-Null
        New-ItemProperty -Path $cmdKey -Name "P3DREBUILD" -PropertyType String -Value "P3DREBUILD" -Force | Out-Null
        New-ItemProperty -Path $cmdKey -Name "P3DCOMPWRAP" -PropertyType String -Value "P3DCOMPWRAP" -Force | Out-Null

        Write-Host "Registry fixed: $appKey"
    }
}

function Get-ComposerHotReloadBlock {
    return @'
    # P3D_COMPOSER_BEGIN
    _composer_flag = os.path.join(_HERE, ".p3d_composer_mode")
    _lib = os.path.join(_HERE, "p3d_composer")
    _scene = os.path.join(_HERE, ".active_scene.json")
    if os.path.isfile(_composer_flag) and os.path.isfile(_scene):
        if _lib not in sys.path:
            sys.path.insert(0, _lib)
        import scene_builder as _sb
        importlib.reload(_sb)
        print("P3D Composer: scene_builder from %s" % _scene)
        try:
            scene = _sb.load_scene(_scene)
            if not (scene.get("parts") or []):
                raise RuntimeError("scene JSON has no parts — insert a catalog part first")
            out = _sb.build_combined_scene(scene, s)
        except Exception as _ex:
            import traceback
            print("P3D Composer: scene build ERROR: %s" % _ex)
            traceback.print_exc()
            raise
        print("P3D Composer: live Python build OK (%d part(s))" % len(scene.get("parts") or []))
        return out.set_color(120)
    raise RuntimeError(
        "P3D Composer: insert a catalog part in P3D Composer first."
    )
    # P3D_COMPOSER_END

'@
}

function Clear-ComposerRuntimeArtifacts {
    param([string]$CustomScripts)

    # Only composer preview cache — do not delete catalog __pycache__ (Spec Editor needs PLANTREGISTERCUSTOMSCRIPTS).
    $composerCache = Join-Path $CustomScripts "p3d_composer\__pycache__"
    if (Test-Path $composerCache) {
        Remove-Item $composerCache -Recurse -Force
        Write-Host "Removed composer preview cache: $composerCache"
    }

    foreach ($f in @(".active_scene.json", ".p3d_composer_mode", ".p3d_composer_scene_path")) {
        $p = Join-Path $CustomScripts $f
        if (Test-Path $p) {
            Remove-Item $p -Force
            Write-Host "Removed runtime flag: $p"
        }
    }

    $composerLib = Join-Path $CustomScripts "p3d_composer"
    foreach ($f in @("composer_live.py", "COMPOSER_LIVE.xml", "composer_catalog.py", "COMPOSER_CATALOG.py")) {
        $p = Join-Path $composerLib $f
        if (Test-Path $p) {
            Remove-Item $p -Force
            Write-Host "Removed stale generated: $p"
        }
    }
}

function Remove-ValveCatalogArtifacts {
    param([string]$CustomScripts)

    foreach ($dir in @('GV_DN100_CL150', 'GV_DN125_CL150')) {
        $p = Join-Path $CustomScripts $dir
        if (Test-Path $p) {
            Remove-Item $p -Recurse -Force
            Write-Host "Removed valve folder: $p"
        }
    }
    foreach ($file in @(
            'CUST_GV_DN100_CL150.py', 'CUST_GV_DN125_CL150.py',
            'CUST_GV_DN100_CL150.xml', 'CUST_GV_DN125_CL150.xml'
        )) {
        $p = Join-Path $CustomScripts $file
        if (Test-Path $p) {
            Remove-Item $p -Force
            Write-Host "Removed valve script: $p"
        }
    }
}

function Deploy-SupportModules {
    param(
        [string]$GenSrc,
        [string]$CustomScripts
    )

    $partsSrc = Join-Path $GenSrc "parts"

    # Windows: Stud_Bolts and STUD_BOLTS are the same path — remove legacy names before copy.
    foreach ($legacy in @('Stud_Bolts', 'Nuts', 'Structural_profiles')) {
        $old = Join-Path $CustomScripts $legacy
        if (Test-Path $old) {
            Remove-Item $old -Recurse -Force
            Write-Host "Removed legacy support folder: $old"
        }
    }

    foreach ($name in @('STUD_BOLTS', 'NUTS', 'STRUCTURAL_PROFILES')) {
        $src = Join-Path $partsSrc $name
        if (-not (Test-Path $src)) {
            Write-Host "WARN: support module not in parts/ - skip $name"
            continue
        }

        $dst = Join-Path $CustomScripts $name
        if (Test-Path $dst) { Remove-Item $dst -Recurse -Force }
        Copy-Item $src $dst -Recurse -Force
        Write-Host "Deployed support module: $name (from parts/$name)"
    }
}

function Deploy-CatalogMetadata {
    param(
        [string]$GenSrc,
        [string]$CustomScripts
    )

    $syncScript = Join-Path $PSScriptRoot "Sync-CatalogMetadata.ps1"
    if (Test-Path $syncScript) {
        & $syncScript -CatalogGeneratorDir $GenSrc
    } else {
        Write-Host "WARN: Sync-CatalogMetadata.ps1 not found - root ScriptGroup/variants may be stale"
    }

    foreach ($name in @('ScriptGroup.xml', 'variants.xml', 'variants.map')) {
        $src = Join-Path $GenSrc $name
        if (Test-Path $src) {
            Copy-Item $src (Join-Path $CustomScripts $name) -Force
            Write-Host "Deployed catalog metadata: $name"
        }
    }
}

function Patch-HotReloadForComposer {
    param([string]$HotReloadPath)

    if (-not (Test-Path $HotReloadPath)) {
        Write-Host "WARN: hot_reload.py not found - skip patch"
        return
    }

    $content = Get-Content $HotReloadPath -Raw
    $block = (Get-ComposerHotReloadBlock).TrimEnd()

    $content = $content -replace '(?m)^_P3D_COMPOSER_LAST\s*=.*\r?\n', ''
    $content = $content -replace '(?s)    global _P3D_COMPOSER_LAST\s*\n    if _P3D_COMPOSER_LAST is not None:.*?_P3D_COMPOSER_LAST = None\s*\n\s*\n', ''

    if ($content -match 'P3D_COMPOSER_BEGIN') {
        $content = $content -replace '(?s)# P3D_COMPOSER_BEGIN.*?# P3D_COMPOSER_END', $block
        $content = $content -replace '(?s)# P3D_COMPOSER_END\r?\n\s*# =+.*?return gv\.build_preview.*?\r?\n\s*# =+.*?\r?\n', "# P3D_COMPOSER_END`n`n"
        Write-Host "Updated P3D Composer block in hot_reload.py"
    }
    elseif ($content -match '_purge_local_modules\(\)') {
        $content = $content -replace '(?s)(_purge_local_modules\(\)\s*\n)', "`$1`n$block`n"
        Write-Host "Patched hot_reload.py for P3D Composer"
    }
    else {
        Write-Host "WARN: hot_reload.py layout changed - patch manually"
        return
    }

    Set-Content -Path $HotReloadPath -Value $content -Encoding UTF8
}

function Patch-WrapperLisp {
    param(
        [string]$WrapperLspPath,
        [string]$TemplatePath
    )
    if (-not (Test-Path $TemplatePath)) {
        Write-Host "WARN: Wrapper.lsp template not found"
        return
    }
    $content = Get-Content $WrapperLspPath -Raw -ErrorAction SilentlyContinue
    if ($content -match 'c:COMPWRAPFRESH' -and $content -notmatch 'REGEN') {
        Write-Host "Wrapper.lsp already up to date"
        return
    }
    Copy-Item $TemplatePath $WrapperLspPath -Force
    Write-Host "Deployed Wrapper.lsp (COMPWRAP without REGEN)"
}

function Merge-CatalogPartPy {
    param(
        [string]$EntryPath,
        [string]$GeometryPath
    )

    $entryLines = Get-Content $EntryPath
    $geom = (Get-Content $GeometryPath -Raw).TrimEnd()

    $varmain = @($entryLines | Where-Object { $_ -match 'varmain\.custom' })
    $body = @($entryLines | Where-Object {
            $_ -notmatch 'varmain\.custom' -and
            $_ -notmatch '^from [A-Z0-9_]+\.CUST_[A-Z0-9_]+ import '
        })
    $activateIdx = 0
    for ($i = 0; $i -lt $body.Count; $i++) {
        if ($body[$i] -match '@activate') {
            $activateIdx = $i
            break
        }
    }
    $body = $body[$activateIdx..($body.Count - 1)]

    $geomLines = @($geom -split "`n" | ForEach-Object { $_.TrimEnd("`r") } | ForEach-Object {
            if ($_ -match 'varmain\.custom' -or $_ -match '^# Port Manager:') { return }
            if ($_ -match '^from ([A-Z0-9_]+)\.CUST_\1 import (.+)$') {
                return "from CUST_$($Matches[1]) import $($Matches[2])"
            }
            $_
        } | Where-Object { $_ -ne $null })
    while ($geomLines.Count -gt 0 -and [string]::IsNullOrWhiteSpace($geomLines[0])) {
        $geomLines = $geomLines[1..($geomLines.Count - 1)]
    }
    $geom = ($geomLines -join "`n").TrimEnd()

    $parts = @()
    if ($varmain.Count -gt 0) { $parts += ($varmain -join "`n") }
    if ($geom) { $parts += $geom }
    if ($body.Count -gt 0) { $parts += ($body -join "`n") }
    return ($parts -join "`n`n").TrimEnd() + "`n"
}

function Remove-OrphanedCatalogParts {
    param(
        [string]$PartsSrc,
        [string]$CustomScripts
    )

    if (-not (Test-Path $CustomScripts)) { return 0 }

    $reserved = @('STUD_BOLTS', 'NUTS', 'STRUCTURAL_PROFILES', 'p3d_composer', '__pycache__', 'Resources')
    $active = @{}
    if (Test-Path $PartsSrc) {
        foreach ($partDir in Get-ChildItem $PartsSrc -Directory) {
            $entry = Join-Path $partDir.FullName "catalog_entry.py"
            if (Test-Path $entry) { $active[$partDir.Name] = $true }
        }
    }

    $removed = 0
    foreach ($py in Get-ChildItem $CustomScripts -Filter "CUST_*.py" -File) {
        $partId = $py.BaseName.Substring(5)
        if ($active.ContainsKey($partId)) { continue }
        if (Remove-CatalogPartArtifacts -CustomScripts $CustomScripts -PartId $partId) {
            $removed++
            Write-Host "Removed orphaned catalog part: CUST_$partId"
        }
    }

    foreach ($dir in Get-ChildItem $CustomScripts -Directory) {
        if ($reserved -contains $dir.Name) { continue }
        if ($active.ContainsKey($dir.Name)) { continue }
        $nested = Join-Path $dir.FullName (Join-Path $dir.Name "CUST_$($dir.Name).py")
        $flatInDir = Join-Path $dir.FullName "CUST_$($dir.Name).py"
        if (-not (Test-Path $nested) -and -not (Test-Path $flatInDir)) { continue }
        Remove-Item $dir.FullName -Recurse -Force
        $removed++
        Write-Host "Removed orphaned part folder: $($dir.FullName)"
    }

    return $removed
}

function Remove-CatalogPartArtifacts {
    param(
        [string]$CustomScripts,
        [string]$PartId
    )

    $changed = $false
    foreach ($name in @("CUST_$PartId.py", "CUST_$PartId.xml")) {
        $p = Join-Path $CustomScripts $name
        if (Test-Path $p) {
            Remove-Item $p -Force
            $changed = $true
        }
    }

    $partFolder = Join-Path $CustomScripts $PartId
    if (Test-Path $partFolder) {
        Remove-Item $partFolder -Recurse -Force
        $changed = $true
    }

    $cacheDir = Join-Path $CustomScripts "__pycache__"
    if (Test-Path $cacheDir) {
        foreach ($pyc in Get-ChildItem $cacheDir -Filter "CUST_$PartId*.pyc" -File) {
            Remove-Item $pyc.FullName -Force
            $changed = $true
        }
    }

    return $changed
}

function Remove-DeployedPartFolders {
    param(
        [string]$PartsSrc,
        [string]$CustomScripts
    )

    if (-not (Test-Path $PartsSrc)) { return }

    foreach ($partDir in Get-ChildItem $PartsSrc -Directory) {
        $partId = $partDir.Name
        $entry = Join-Path $partDir.FullName "catalog_entry.py"
        if (-not (Test-Path $entry)) { continue }

        $libDst = Join-Path $CustomScripts $partId
        if (Test-Path $libDst) {
            Remove-Item $libDst -Recurse -Force
            Write-Host "Removed deployed part folder (flat catalog): $libDst"
        }
    }
}

function Deploy-CustomParts {
    param(
        [string]$PartsSrc,
        [string]$CustomScripts
    )

    if (-not (Test-Path $PartsSrc)) {
        Write-Host "WARN: parts folder not found - skip deploy"
        return
    }

    Remove-DeployedPartFolders -PartsSrc $PartsSrc -CustomScripts $CustomScripts
    $orphans = Remove-OrphanedCatalogParts -PartsSrc $PartsSrc -CustomScripts $CustomScripts
    if ($orphans -gt 0) {
        Write-Host "Removed $orphans orphaned catalog part(s) from CustomScripts (incl. __pycache__)"
    }

    $count = 0
    foreach ($partDir in Get-ChildItem $PartsSrc -Directory) {
        $partId = $partDir.Name
        $entry = Join-Path $partDir.FullName "catalog_entry.py"
        if (-not (Test-Path $entry)) { continue }

        $geometry = Join-Path $partDir.FullName "$partId\CUST_$partId.py"
        $destEntry = Join-Path $CustomScripts "CUST_$partId.py"
        if (Test-Path $geometry) {
            $merged = Merge-CatalogPartPy -EntryPath $entry -GeometryPath $geometry
            Set-Content -Path $destEntry -Value $merged -Encoding UTF8
        }
        else {
            Copy-Item $entry $destEntry -Force
            Write-Host "WARN: no geometry for $partId - deployed entry only"
        }

        $entryXml = Join-Path $partDir.FullName "catalog_entry.xml"
        if (Test-Path $entryXml) {
            Copy-Item $entryXml (Join-Path $CustomScripts "CUST_$partId.xml") -Force
        }

        $count++
    }

    Write-Host "Deployed $count flat catalog part(s) from catalog_generator/parts"
}

function Write-DeploySettings {
    param(
        [string]$Root,
        [string]$GenSrc
    )

    $dir = Join-Path $env:APPDATA "Plant3DCatalogComposer"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $payload = @{
        apiRoot          = $Root
        catalogGenerator = $GenSrc
        primitivesPy     = (Join-Path $Root "Plant3DSkeletonManager\primitives.py")
    } | ConvertTo-Json
    Set-Content -Path (Join-Path $dir "deploy.json") -Value $payload -Encoding UTF8
    Write-Host "Wrote deploy settings: $dir\deploy.json"
}

function Patch-WrapperForComposer {
    param(
        [string]$WrapperPath,
        [string]$TemplatePath
    )

    if (-not (Test-Path $WrapperPath)) {
        Write-Host "WARN: wrapper.py not found - skip patch"
        return
    }
    if (-not (Test-Path $TemplatePath)) {
        Write-Host "WARN: wrapper template not found - skip patch"
        return
    }

    Copy-Item $TemplatePath $WrapperPath -Force
    Write-Host "Deployed composer wrapper.py (preview erase tracking)"
}

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $root "Plant3DCatalogComposer\bin\$Configuration\net8.0-windows"
$dll = Join-Path $outDir "Plant3DCatalogComposer.dll"

if (-not (Test-Path $dll)) {
    Write-Host "Building Plant3DCatalogComposer ($Configuration)..."
    Push-Location (Join-Path $root "Plant3DCatalogComposer")
    dotnet build -c $Configuration
    Pop-Location
    if (-not (Test-Path $dll)) {
        Write-Error "Build failed - DLL not found at $dll"
    }
}

$genSrc = Join-Path $root "catalog_generator"

# Plant 3D preview via wrapper/hot_reload (patch even when Plant 3D has DLL locked)
$customScripts = "C:\AutoCAD Plant 3D 2026 Content\CPak Common\CustomScripts"
$composerLib = Join-Path $customScripts "p3d_composer"
if (Test-Path (Split-Path $customScripts -Parent)) {
    New-Item -ItemType Directory -Force -Path $composerLib | Out-Null
    Copy-Item (Join-Path $genSrc "p3d_composer\*.py") $composerLib -Force
    foreach ($xml in Get-ChildItem $composerLib -Filter "*.xml" -ErrorAction SilentlyContinue) {
        Remove-Item $xml.FullName -Force
        Write-Host "Removed composer XML (not a catalog script): $($xml.Name)"
    }
    $composerCache = Join-Path $composerLib "__pycache__"
    if (Test-Path $composerCache) {
        Remove-Item $composerCache -Recurse -Force
        Write-Host "Cleared composer preview cache: $composerCache"
    }
    Copy-Item (Join-Path $genSrc "p3d_composer_rebuild.py") $customScripts -Force
    Copy-Item (Join-Path $genSrc "P3D_COMPOSER_REBUILD.xml") $customScripts -Force
    foreach ($stale in @("scene_builder.py", "p3d_session_rebuild.py")) {
        $p = Join-Path $customScripts $stale
        if (Test-Path $p) { Remove-Item $p -Force; Write-Host "Removed stale: $p" }
    }
    Patch-HotReloadForComposer -HotReloadPath (Join-Path $customScripts "hot_reload.py")
    Patch-WrapperForComposer `
        -WrapperPath (Join-Path $customScripts "wrapper.py") `
        -TemplatePath (Join-Path $genSrc "p3d_composer\wrapper_patched.py")
    Patch-WrapperLisp `
        -WrapperLspPath (Join-Path $customScripts "Wrapper.lsp") `
        -TemplatePath (Join-Path $genSrc "Wrapper.lsp")
    foreach ($staleLsp in @("P3DCOMPREWRAP.lsp", "P3DComposerRebuild.lsp")) {
        $p = Join-Path $customScripts $staleLsp
        if (Test-Path $p) { Remove-Item $p -Force; Write-Host "Removed stale: $p" }
    }
    Clear-ComposerRuntimeArtifacts -CustomScripts $customScripts
    Remove-ValveCatalogArtifacts -CustomScripts $customScripts
    Deploy-CustomParts -PartsSrc (Join-Path $genSrc "parts") -CustomScripts $customScripts
    Deploy-SupportModules -GenSrc $genSrc -CustomScripts $customScripts
    $pipeSizes = Join-Path $genSrc "pipe_sizes.py"
    if (Test-Path $pipeSizes) {
        Copy-Item $pipeSizes (Join-Path $customScripts "pipe_sizes.py") -Force
        Write-Host "Deployed pipe_sizes.py"
    }
    $swGeom = Join-Path $genSrc "sw_fitting_geom.py"
    if (Test-Path $swGeom) {
        Copy-Item $swGeom (Join-Path $customScripts "sw_fitting_geom.py") -Force
        Write-Host "Deployed sw_fitting_geom.py"
    }
    $primitives = Join-Path $root "Plant3DSkeletonManager\primitives.py"
    if (Test-Path $primitives) {
        Copy-Item $primitives (Join-Path $customScripts "primitives.py") -Force
        Write-Host "Deployed primitives.py"
    }
    $standardSets = Join-Path $genSrc "standard_sets.json"
    if (Test-Path $standardSets) {
        Copy-Item $standardSets (Join-Path $customScripts "standard_sets.json") -Force
        Write-Host "Deployed standard_sets.json"
    }
    Deploy-CatalogMetadata -GenSrc $genSrc -CustomScripts $customScripts
    Write-DeploySettings -Root $root -GenSrc $genSrc
    Write-Host "Composer lib: $composerLib"
    Write-Host "Manual rebuild: P3DCOMPWRAP or Wrapper.lsp COMPWRAP / COMPWRAPFRESH"
} else {
    Write-Host "WARN: CustomScripts folder not found"
}

$bundleRoot = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\Plant3DCatalogComposer.bundle"
$contents = Join-Path $bundleRoot "Contents"
New-Item -ItemType Directory -Force -Path $contents | Out-Null

$builtDll = Join-Path $outDir "Plant3DCatalogComposer.dll"
$netloadDll = Join-Path $customScripts "Plant3DCatalogComposer.dll"
$bundleCopied = $false
try {
    Copy-Item $builtDll $contents -Force
    $bundleCopied = $true
} catch {
    Write-Host "WARN: Could not copy DLL to bundle (Plant 3D may have it locked): $_"
}
$iconSrc = Join-Path $root "Plant3DCatalogComposer\Resources"
$iconDstBundle = Join-Path $contents "Resources"
$iconDstNetload = Join-Path $customScripts "Resources"
if (Test-Path $iconSrc) {
    New-Item -ItemType Directory -Force -Path $iconDstBundle | Out-Null
    Copy-Item (Join-Path $iconSrc "*.png") $iconDstBundle -Force
}
if (Test-Path (Split-Path $customScripts -Parent)) {
    try {
        Copy-Item $builtDll $netloadDll -Force
        if (Test-Path $iconSrc) {
            New-Item -ItemType Directory -Force -Path $iconDstNetload | Out-Null
            Copy-Item (Join-Path $iconSrc "*.png") $iconDstNetload -Force
        }
        Write-Host "NETLOAD while CAD is open: $netloadDll"
    } catch {
        Write-Host "WARN: Could not copy NETLOAD DLL to CustomScripts: $_"
    }
}
Copy-Item (Join-Path $root "Plant3DCatalogComposer\PackageContents.xml") $bundleRoot -Force

$genDst = Join-Path $contents "catalog_generator"
$genDstLib = Join-Path $genDst "p3d_composer"
New-Item -ItemType Directory -Force -Path $genDstLib | Out-Null
Copy-Item (Join-Path $genSrc "p3d_composer\*") $genDstLib -Force
# Part library lives on D: (deploy.json) — do not duplicate parts/ into the ApplicationPlugins bundle.
$partsDst = Join-Path $genDst "parts"
if (Test-Path $partsDst) {
    Remove-Item $partsDst -Recurse -Force
    Write-Host "Removed bundle catalog_generator/parts (use repo source via deploy.json)"
}
Copy-Item (Join-Path $genSrc "Wrapper.lsp") (Join-Path $genDst "Wrapper.lsp") -Force

Copy-Item (Join-Path $root "Plant3DSkeletonManager\primitives.py") $contents -Force

$dllPath = Join-Path $contents "Plant3DCatalogComposer.dll"
Register-Plant3DComposer -DllPath $dllPath -Description "Compose Plant 3D catalog primitives visually"

Write-Host "Installed to: $bundleRoot"
Write-Host "DLL path: $dllPath"
Write-Host ""
if ($bundleCopied) {
    Write-Host "Restart Plant 3D $AcadYear — tab Ribbon 'P3D Composer' loads automatically."
} else {
    Write-Host "Plant 3D was open — restart CAD when you can, OR run once:"
    Write-Host "  NETLOAD"
    Write-Host "  $netloadDll"
    Write-Host "Then use Ribbon tab 'P3D Composer' -> Open Composer."
}
Write-Host "Command line fallback: P3DCOMPOSER"

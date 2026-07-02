# Install Plant3DCatalogComposer into AutoCAD / Plant 3D ApplicationPlugins
param(
    [string]$AcadYear = "2026",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipBuild
)

function Set-PackageContentsVersion {
    param(
        [string]$TemplateXml,
        [string]$DestXml,
        [string]$DllPath
    )

    if (-not (Test-Path $TemplateXml)) { return }
    $version = (Get-Item $DllPath).LastWriteTime.ToString('yyyy.M.d.HHmm')
    $xml = Get-Content $TemplateXml -Raw
    $xml = $xml -replace 'AppVersion="[^"]*"', "AppVersion=`"$version`""
    $xml = $xml -replace '(<ComponentEntry[^>]*\sVersion=")[^"]*(")', "`${1}$version`${2}"
    Set-Content -Path $DestXml -Value $xml -Encoding UTF8
    Write-Host "PackageContents version -> $version"
}

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
        # REG_SZ — ExpandString eats "\C" in "...bundle\Contents\..." and breaks autoload.
        New-ItemProperty -Path $appKey -Name "LOADER" -PropertyType String -Value $DllPath -Force | Out-Null
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

function Clear-AllPythonCache {
    param([string]$CustomScripts)

    if (-not (Test-Path $CustomScripts)) { return 0 }

    $count = 0
    Get-ChildItem $CustomScripts -Directory -Recurse -Filter '__pycache__' -ErrorAction SilentlyContinue |
        ForEach-Object {
            Remove-Item $_.FullName -Recurse -Force
            $count++
            Write-Host "Cleared __pycache__: $($_.FullName)"
        }
    return $count
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
        $src = Join-Path $partsSrc "ARCHIVED\$name"
        if (-not (Test-Path $src)) {
            $src = Join-Path $partsSrc $name
        }
        if (-not (Test-Path $src)) {
            $src = Join-Path $GenSrc "support\$name"
        }
        if (-not (Test-Path $src)) {
            Write-Host "WARN: support module not found (ARCHIVED/, parts/, support/) - skip $name"
            continue
        }

        $dst = Join-Path $CustomScripts $name
        if (Test-Path $dst) { Remove-Item $dst -Recurse -Force }
        New-Item -ItemType Directory -Force -Path $dst | Out-Null
        Get-ChildItem $src -Recurse -File -Filter '*.py' | ForEach-Object {
            $rel = $_.FullName.Substring($src.Length).TrimStart('\', '/')
            $target = Join-Path $dst $rel
            $parent = Split-Path $target -Parent
            if (-not (Test-Path $parent)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
            Copy-Item $_.FullName $target -Force
        }
        $relSrc = $src.Replace($partsSrc, 'parts').Replace($GenSrc, 'catalog_generator')
        Write-Host "Deployed support module: $name (from $relSrc, .py only)"
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
    $entryImports = @($entryLines | Where-Object {
            $_ -match '^(import |from )' -and
            $_ -notmatch 'varmain\.custom' -and
            $_ -notmatch '^from [A-Z0-9_]+\.CUST_[A-Z0-9_]+ import '
        })
    $body = @($entryLines | Where-Object {
            $_ -notmatch 'varmain\.custom' -and
            $_ -notmatch '^(import |from )' -and
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
    if ($entryImports.Count -gt 0) { $parts += ($entryImports -join "`n") }
    if ($geom) { $parts += $geom }
    if ($body.Count -gt 0) { $parts += ($body -join "`n") }
    $merged = ($parts -join "`n`n").TrimEnd() + "`n"
    return (Ensure-SupportImports -Content $merged)
}

function Ensure-SupportImports {
    param([string]$Content)

    if ($Content -notmatch 'catalog_params\.') { return $Content }
    if ($Content -match '(?m)^import catalog_params\s*$|^from catalog_params import ') { return $Content }

    $importLine = "import catalog_params"
    $marker = 'varmain.custom'
    $idx = $Content.IndexOf($marker)
    if ($idx -ge 0) {
        $lineEnd = $Content.IndexOf("`n", $idx)
        $insertAt = if ($lineEnd -lt 0) { $Content.Length } else { $lineEnd + 1 }
        return $Content.Insert($insertAt, "`n$importLine`n")
    }
    return "$importLine`n`n$Content"
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
            if ($partDir.Name -eq 'ARCHIVED' -or $partDir.Name -eq 'CUSTOM' -or $partDir.Name.StartsWith('_')) { continue }
            $entry = Join-Path $partDir.FullName "catalog_entry.py"
            if (Test-Path $entry) { $active[$partDir.Name] = $true }
        }

        # Composite parts under parts/CUSTOM/<name>/ deploy as CUST_<name> too — mark active
        # so they are never pruned as orphans.
        $customRoot = Join-Path $PartsSrc 'CUSTOM'
        if (Test-Path $customRoot) {
            foreach ($partDir in Get-ChildItem $customRoot -Directory) {
                $entry = Join-Path $partDir.FullName "catalog_entry.py"
                if (Test-Path $entry) { $active[$partDir.Name] = $true }
            }
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

function Deploy-PartDirectory {
    param(
        [System.IO.DirectoryInfo]$PartDir,
        [string]$CustomScripts
    )

    $partId = $PartDir.Name
    $entry = Join-Path $PartDir.FullName "catalog_entry.py"
    if (-not (Test-Path $entry)) { return $false }

    $geometry = Join-Path $PartDir.FullName "$partId\CUST_$partId.py"
    $destEntry = Join-Path $CustomScripts "CUST_$partId.py"
    if (Test-Path $geometry) {
        $merged = Merge-CatalogPartPy -EntryPath $entry -GeometryPath $geometry
        Set-Content -Path $destEntry -Value $merged -Encoding UTF8
    }
    else {
        Copy-Item $entry $destEntry -Force
        Write-Host "WARN: no geometry for $partId - deployed entry only"
    }

    $entryXml = Join-Path $PartDir.FullName "catalog_entry.xml"
    if (Test-Path $entryXml) {
        Copy-Item $entryXml (Join-Path $CustomScripts "CUST_$partId.xml") -Force
    }

    return $true
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
        if ($partId -eq 'ARCHIVED' -or $partId -eq 'CUSTOM' -or $partId.StartsWith('_')) { continue }
        if (Deploy-PartDirectory -PartDir $partDir -CustomScripts $CustomScripts) {
            $count++
        }
    }

    # User-authored composite parts under parts/CUSTOM/<name>/ deploy the same way.
    $customRoot = Join-Path $PartsSrc 'CUSTOM'
    if (Test-Path $customRoot) {
        foreach ($partDir in Get-ChildItem $customRoot -Directory) {
            if (Deploy-PartDirectory -PartDir $partDir -CustomScripts $CustomScripts) {
                $count++
            }
        }
    }

    Write-Host "Deployed $count flat catalog part(s) from catalog_generator/parts"
}

function Write-DeployManifest {
    param(
        [string]$CustomScripts,
        [int]$ScriptCount,
        [int]$PycacheCleared
    )

    $keyFiles = @(
        'lj_stud_bolts.py',
        'CUST_GSK_FF_CL150.py',
        'CUST_GSK_RF_CL150.py',
        'CUST_WN_FLRF_CL150.py',
        'CUST_LJ_RING_CL150_RF.py',
        'stubend_geom.py',
        'pipe_sizes.py',
        'catalog_params.py'
    )
    $hashes = @{}
    foreach ($name in $keyFiles) {
        $p = Join-Path $CustomScripts $name
        if (Test-Path $p) {
            $hashes[$name] = (Get-FileHash $p -Algorithm MD5).Hash.ToLowerInvariant()
        }
    }

    $manifest = @{
        deployVersion             = '2026.06.11'
        deployedAtUtc             = (Get-Date).ToUniversalTime().ToString('o')
        scriptCount               = $ScriptCount
        pycacheFoldersCleared     = $PycacheCleared
        registerQueued            = $false
        pluginRestartRecommended  = $false
        keyFileHashes             = $hashes
    } | ConvertTo-Json -Depth 5

    $path = Join-Path $CustomScripts 'deploy_manifest.json'
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($path, $manifest, $utf8NoBom)
    Write-Host "Wrote deploy manifest: $path"
    return $path
}

function Write-DeploySettings {
    param(
        [string]$Root,
        [string]$GenSrc,
        [string]$CustomScripts,
        [string]$Configuration,
        [string]$BundleContents
    )

    $dir = Join-Path $env:APPDATA "Plant3DCatalogComposer"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $payload = @{
        apiRoot          = $Root
        catalogGenerator = $GenSrc
        primitivesPy     = (Join-Path $Root "Plant3DSkeletonManager\primitives.py")
    } | ConvertTo-Json
    $deployJson = Join-Path $dir "deploy.json"
    Set-Content -Path $deployJson -Value $payload -Encoding UTF8
    Write-Host "Wrote deploy settings: $deployJson"

    foreach ($target in @(
            (Join-Path $Root "Plant3DCatalogComposer\bin\$Configuration\net8.0-windows\deploy.json"),
            (Join-Path $CustomScripts "deploy.json"),
            (Join-Path $BundleContents "deploy.json")
        )) {
        if ([string]::IsNullOrWhiteSpace($target)) { continue }
        $parent = Split-Path $target -Parent
        if (Test-Path $parent) {
            Copy-Item $deployJson $target -Force
            Write-Host "Copied deploy.json -> $target"
        }
    }
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

if (-not $SkipBuild) {
    if (-not (Test-Path $dll)) {
        Write-Host "Plant3DCatalogComposer.dll not found - building ($Configuration)..."
    } else {
        Write-Host "Building Plant3DCatalogComposer ($Configuration)..."
    }
    # Template refresh disabled: CatalogBuilderTemplate.xlsx now mirrors CATA_NUI structure and is
    # a static asset. add_static_support_template_sheets.py would restore the legacy 32-sheet
    # valve/pipe template (CATA_NUI has no VALVE sheet -> it triggers a full restore). To update the
    # bundled template, replace the file directly.
    # $templateScript = Join-Path $root "scripts\add_static_support_template_sheets.py"
    # if (Test-Path $templateScript) {
    #     Write-Host "Refreshing CatalogBuilderTemplate.xlsx (pipe + valve sheets)..."
    #     python $templateScript
    # }
    Push-Location (Join-Path $root "Plant3DCatalogComposer")
    dotnet build -c $Configuration -p:SkipComposerDeploy=true
    Pop-Location
    if (-not (Test-Path $dll)) {
        Write-Error "Build failed - DLL not found at $dll"
    }
} else {
    Write-Host "SkipBuild - deploying existing $Configuration output only."
    if (-not (Test-Path $dll)) {
        Write-Error "DLL not found at $dll - run without -SkipBuild or dotnet build -c $Configuration first."
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
    Copy-Item (Join-Path $genSrc "hot_reload.py") $customScripts -Force
    Write-Host "Deployed hot_reload.py (SDK reload + composer + CUST_* catalog)"
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
    $pycacheCleared = Clear-AllPythonCache -CustomScripts $customScripts
    Deploy-CustomParts -PartsSrc (Join-Path $genSrc "parts") -CustomScripts $customScripts
    Deploy-SupportModules -GenSrc $genSrc -CustomScripts $customScripts
    $pipeSizes = Join-Path $genSrc "pipe_sizes.py"
    if (Test-Path $pipeSizes) {
        Copy-Item $pipeSizes (Join-Path $customScripts "pipe_sizes.py") -Force
        Write-Host "Deployed pipe_sizes.py"
    }
    $catalogParams = Join-Path $genSrc "catalog_params.py"
    if (Test-Path $catalogParams) {
        Copy-Item $catalogParams (Join-Path $customScripts "catalog_params.py") -Force
        Write-Host "Deployed catalog_params.py"
    }
    $nativeShapes = Join-Path $genSrc "native_shapes.py"
    if (Test-Path $nativeShapes) {
        Copy-Item $nativeShapes (Join-Path $customScripts "native_shapes.py") -Force
        Write-Host "Deployed native_shapes.py"
    }
    $swGeom = Join-Path $genSrc "sw_fitting_geom.py"
    if (Test-Path $swGeom) {
        Copy-Item $swGeom (Join-Path $customScripts "sw_fitting_geom.py") -Force
        Write-Host "Deployed sw_fitting_geom.py"
    }
    foreach ($supportPy in @("stubend_geom.py", "lj_stud_bolts.py")) {
        $supportSrc = Join-Path $genSrc $supportPy
        if (Test-Path $supportSrc) {
            Copy-Item $supportSrc (Join-Path $customScripts $supportPy) -Force
            Write-Host "Deployed $supportPy"
        }
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
    $pycacheCleared += Clear-AllPythonCache -CustomScripts $customScripts
    Write-Host "Total __pycache__ folders cleared: $pycacheCleared (PLANTREGISTERCUSTOMSCRIPTS rebuilds .pyc in Plant 3D)"
    Write-DeployManifest -CustomScripts $customScripts -ScriptCount 0 -PycacheCleared $pycacheCleared | Out-Null
    Write-Host "Composer lib: $composerLib"
    Write-Host "Manual rebuild: P3DCOMPWRAP or Wrapper.lsp COMPWRAP / COMPWRAPFRESH"
} else {
    Write-Host "WARN: CustomScripts folder not found"
}

$bundleRoot = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\Plant3DCatalogComposer.bundle"
$contents = Join-Path $bundleRoot "Contents"
New-Item -ItemType Directory -Force -Path $contents | Out-Null
Write-DeploySettings -Root $root -GenSrc $genSrc -CustomScripts $customScripts -Configuration $Configuration -BundleContents $contents

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
    $templateXlsx = Join-Path $iconSrc "CatalogBuilderTemplate.xlsx"
    if (Test-Path $templateXlsx) {
        Copy-Item $templateXlsx $iconDstBundle -Force
    }
}
if (Test-Path (Split-Path $customScripts -Parent)) {
    try {
        Copy-Item $builtDll $netloadDll -Force
        if (Test-Path $iconSrc) {
            New-Item -ItemType Directory -Force -Path $iconDstNetload | Out-Null
            Copy-Item (Join-Path $iconSrc "*.png") $iconDstNetload -Force
            $templateXlsx = Join-Path $iconSrc "CatalogBuilderTemplate.xlsx"
            if (Test-Path $templateXlsx) {
                Copy-Item $templateXlsx $iconDstNetload -Force
            }
        }
        Write-Host "NETLOAD while CAD is open: $netloadDll"
    } catch {
        Write-Host "WARN: Could not copy NETLOAD DLL to CustomScripts: $_"
    }
}

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

$packageTemplate = Join-Path $root "Plant3DCatalogComposer\PackageContents.xml"
Set-PackageContentsVersion -TemplateXml $packageTemplate -DestXml (Join-Path $bundleRoot "PackageContents.xml") -DllPath $builtDll

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

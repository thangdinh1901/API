# Rebuild catalog_generator ScriptGroup.xml, variants.xml, variants.map
# from every parts/*/catalog_entry.py (same logic as CatalogMetadataSyncService in the plugin).
param(
    [string]$CatalogGeneratorDir = (Join-Path (Split-Path -Parent $PSScriptRoot) "catalog_generator")
)

function Escape-XmlText([string]$Value) {
    if ([string]::IsNullOrEmpty($Value)) { return "" }
    return $Value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace('"', "&quot;")
}

function Get-AttrValue([string]$Attrs, [string]$Name) {
    if ($Attrs -match "(?m)$Name\s*=\s*`"([^`"]*)`"") { return $Matches[1] }
    return $null
}

$partsDir = Join-Path $CatalogGeneratorDir "parts"
if (-not (Test-Path $partsDir)) {
    Write-Error "parts folder not found: $partsDir"
    exit 1
}

$entries = @()
foreach ($partDir in Get-ChildItem $partsDir -Directory) {
    $entryPy = Join-Path $partDir.FullName "catalog_entry.py"
    if (-not (Test-Path $entryPy)) { continue }

    $text = Get-Content $entryPy -Raw
    $activatePattern = '(?s)@activate\s*\(\s*(?<attrs>.*?)\)\s*def\s+(?<name>CUST_\w+)\s*\('
    if ($text -notmatch $activatePattern) {
        Write-Host ('WARN: skip ' + $partDir.Name + ' — no @activate / CUST_* def in catalog_entry.py')
        continue
    }

    $attrs = $Matches.attrs
    $scriptName = $Matches.name
    $group = Get-AttrValue $attrs "Group"
    if (-not $group) { $group = "Custom" }
    $endtypes = Get-AttrValue $attrs "FirstPortEndtypes"
    if (-not $endtypes) { $endtypes = "FL" }
    $short = Get-AttrValue $attrs "TooltipShort"
    if (-not $short) { $short = $partDir.Name }
    $long = Get-AttrValue $attrs "TooltipLong"
    if (-not $long) { $long = $short }

    $entries += [pscustomobject]@{
        ScriptName = $scriptName
        Group      = $group
        Endtypes   = $endtypes
        Short      = $short
        Long       = $long
    }
}

$entries = $entries | Sort-Object ScriptName

$sg = @("<?xml version=`"1.0`" encoding=`"utf-8`"?>", "<ScriptInfo>")
foreach ($e in $entries) {
    $sg += "`t<ScriptGroup>"
    $sg += "`t`t<ScriptName>$($e.ScriptName)</ScriptName>"
    $sg += "`t`t<Group>$(Escape-XmlText $e.Group)</Group>"
    $sg += "`t`t<FirstPortEndtypes>$(Escape-XmlText $e.Endtypes)</FirstPortEndtypes>"
    $sg += "`t</ScriptGroup>"
}
$sg += "</ScriptInfo>"
$sgPath = Join-Path $CatalogGeneratorDir "ScriptGroup.xml"
Set-Content -Path $sgPath -Value ($sg -join "`n") -Encoding UTF8

$vx = @(
    '<?xml version="1.0" encoding="utf-8"?>',
    '<MsgList xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">',
    "`t<ToolTipLongColl>"
)
foreach ($e in $entries) {
    $vx += "`t`t<Data id=`"$($e.ScriptName)_L`">$(Escape-XmlText $e.Long)</Data>"
}
$vx += "`t</ToolTipLongColl>", "`t<ToolTipShortColl>"
foreach ($e in $entries) {
    $vx += "`t`t<Data id=`"$($e.ScriptName)`">$(Escape-XmlText $e.Short)</Data>"
}
$vx += "`t</ToolTipShortColl>", "`t<ParamGroups>", "`t</ParamGroups>", "`t<EnumColl>", "`t</EnumColl>", "</MsgList>"
$vxPath = Join-Path $CatalogGeneratorDir "variants.xml"
Set-Content -Path $vxPath -Value ($vx -join "`n") -Encoding UTF8

$mapLines = @($entries | ForEach-Object { "$($_.ScriptName)=$($_.ScriptName)" })
$mapLines += @("P3D_COMPOSER_REBUILD=P3D_COMPOSER_REBUILD", "wrapper=WRAPPER")
$mapPath = Join-Path $CatalogGeneratorDir "variants.map"
Set-Content -Path $mapPath -Value ($mapLines -join "`n") -Encoding UTF8

Write-Host "Synced $($entries.Count) catalog part(s) -> ScriptGroup.xml, variants.xml, variants.map"
Write-Host "  $CatalogGeneratorDir"

# Plant 3D Catalog Factory

Custom catalog authoring for **AutoCAD Plant 3D 2026**: scene graph → Python scripts → CustomScripts deploy → Excel → `.pcat` → spec.

## Repository layout

```
├── Plant3DCatalogComposer/   AutoCAD plugin (P3DCOMPOSER palette)
├── Plant3DSkeletonManager/   Core scene model + Inventor add-in + primitives.py
├── catalog_generator/        Python catalog parts, tables, scene_builder
│   ├── parts/                One folder per part (part.json + catalog_entry.py)
│   └── p3d_composer/         Live preview / scene_builder
├── scripts/                  Install, deploy, audits
└── BoxExtrudeAddIn/          Inventor sample add-in
```

Architecture notes: see [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md).

## Requirements

- Windows x64
- AutoCAD / Plant 3D **2026** (default paths in `.csproj`; override with `-p:AcadDir=...`)
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Python 3.x (for audits / table sync scripts)

## Quick start (clone → build → install)

```powershell
git clone https://github.com/thangdinh1901/API.git
cd API

# Optional: point plugin at this repo's catalog_generator (recommended for dev)
Copy-Item deploy.json.example "$env:APPDATA\Plant3DCatalogComposer\deploy.json"
# Edit CatalogGenerator / ApiRoot to your clone path

dotnet build Plant3DCatalogComposer\Plant3DCatalogComposer.csproj -c Release -p:SkipComposerDeploy=true
.\scripts\Install-Plant3DCatalogComposer.ps1
```

Restart Plant 3D (or `NETLOAD`), run **`P3DCOMPOSER`**.

> Use `-p:SkipComposerDeploy=true` when building on CI or before running the install script manually.

## Dev vs runtime

| | Path | Role |
|---|------|------|
| **Source (this repo)** | Your clone | Edit C#, Python, scene in Composer |
| **Plant 3D runtime** | `C:\AutoCAD Plant 3D 2026 Content\CPak Common\CustomScripts` | Plant loads `CUST_*.py` |

**Workflow**

1. Edit scene in **P3D Composer** → **Deploy Catalog** (or `.\scripts\Deploy-Catalog.ps1`)
2. **Test Catalog** in drawing (preview ≠ test — always deploy after scene changes)
3. **Publish** Excel → Catalog Builder → `.pcat` → import spec

`deploy.json` (see `deploy.json.example`) tells the plugin where `catalog_generator` lives so **Generate / Deploy** write back to the repo instead of only the bundle copy.

## Composer commands

| Command | Purpose |
|---------|---------|
| `P3DCOMPOSER` | Open palette |
| `P3DCOMPDEPLOY` | Export + deploy + register scripts |
| `P3DCOMPPUBLISH` | Export Catalog Builder Excel |

## Maintenance scripts

| Script | Purpose |
|--------|---------|
| `Deploy-Catalog.ps1` | Deploy + `PLANTREGISTERCUSTOMSCRIPTS` |
| `Sync-CatalogMetadata.ps1` | Rebuild ScriptGroup / variants from `catalog_entry.py` |
| `sync_lap_joint_cs_tables.py` | Sync C# lap-joint tables from `pipe_sizes.py` |
| `audit_lap_joint.py` | Python ↔ C# table coverage |
| `verify_lj_deploy.py` | Post-deploy CustomScripts check |

Ad-hoc scripts named `scripts/_*.py` are local scratch tools and are not part of the release workflow.

## Inventor

See `BoxExtrudeAddIn/` and `scripts\Install-Plant3DAddIn.ps1`.

## License

Private / internal — add a license file before public release if needed.

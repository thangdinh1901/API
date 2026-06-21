# Plant 3D / Inventor API

Workspace: `D:\02. Engineering\04. Autocad Plant 3D\API`

## Cấu trúc

```
API/
├── Plant3DCatalogComposer/     ← AutoCAD Plant 3D 2026 plugin (P3DCOMPOSER)
├── Plant3DSkeletonManager/     ← Scene graph + primitives.py (+ Inventor add-in)
├── catalog_generator/          ← Python nguồn (deploy → CustomScripts)
│   ├── parts/                  ← catalog parts (part.json + catalog_entry.py)
│   ├── pipe_sizes.py           ← DN/NPS, stub end, LJ ring tables
│   ├── stubend_geom.py         ← lap-joint stub end geometry
│   ├── lj_stud_bolts.py        ← LJ stud OAL / nut placement
│   ├── sw_fitting_geom.py      ← socket-weld fittings
│   ├── standard_sets.json      ← BW / LJ / SW part sets
│   └── ScriptGroup.xml, variants.xml
├── scripts/                    ← install, deploy, audit
└── BoxExtrudeAddIn/            ← Inventor sample add-in
```

## Hai vùng làm việc

| Vùng | Đường dẫn | Vai trò |
|------|-----------|---------|
| **Nguồn / dev** | `D:\02. Engineering\04. Autocad Plant 3D\API` | Sửa C#, Python, scene trong Composer |
| **Runtime Plant 3D** | `C:\AutoCAD Plant 3D 2026 Content\CPak Common\CustomScripts` | Plant đọc catalog script |

**Luồng catalog chuẩn:**

1. **Deploy Catalog** trong Composer (hoặc `.\scripts\Deploy-Catalog.ps1`) → copy Python sang CustomScripts, ghi `deploy_manifest.json`, queue `PLANTREGISTERCUSTOMSCRIPTS`.
2. **Export Excel** → **Publish `.pcat`** → import spec.
3. Lap joint: xóa joint cũ, chèn lại Stub → Ring → GSK_FF → Ring → Stub.

Part mới: folder `catalog_generator/parts/{PART_ID}/` với `part.json` + `catalog_entry.py`, rồi `.\scripts\Sync-CatalogMetadata.ps1`. Install gộp entry + geometry thành `CUST_{PART_ID}.py` trên ổ C.

## Build & cài

```powershell
cd "D:\02. Engineering\04. Autocad Plant 3D\API"
dotnet build Plant3DCatalogComposer\Plant3DCatalogComposer.csproj -c Release
.\scripts\Install-Plant3DCatalogComposer.ps1
```

Restart Plant 3D (hoặc `NETLOAD`), chạy `P3DCOMPOSER`.

## Scripts (QA / bảo trì)

| Script | Mục đích |
|--------|----------|
| `Deploy-Catalog.ps1` | Wrapper deploy + register |
| `Sync-CatalogMetadata.ps1` | Rebuild ScriptGroup / variants từ catalog_entry.py |
| `sync_lap_joint_cs_tables.py` | Sync `CatalogStubEndTable.cs`, `CatalogLjRingCl150Table.cs` từ pipe_sizes.py |
| `patch_gsk_template_dual_ports.py` | Sửa template GSK: port S1/S2 (chạy sau khi đổi template) |
| `Export-CatalogExcel.ps1` | Export `.xlsx` headless (không cần mở Composer) |
| `audit_lap_joint.py` | Python ↔ C# table sync, LJ DN coverage |
| `audit_lj_placement.py` | Kiểm tra axial stack / overlap |
| `audit_lj_stud_lengths.py` | Stud OAL tất cả DN |
| `audit_asme_standard_parts.py` | ASME cho WN/SO/BLD/BW/SW (LJ user-data excluded) |
| `verify_lj_deploy.py` | Kiểm tra CustomScripts + deploy_manifest sau deploy |
| `compare_stub_ring.py` | Stub vs ring vs WN_G |

Sau sửa `pipe_sizes.py`: chạy `sync_lap_joint_cs_tables.py`, rồi audit LJ.

## Inventor

Xem `BoxExtrudeAddIn\` và `scripts\Install-Plant3DAddIn.ps1`.

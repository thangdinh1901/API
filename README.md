# Plant 3D / Inventor API

Workspace gốc: `D:\02. Engineering\04. Autocad Plant 3D\API`

## Cấu trúc

```
API/
├── Plant3DCatalogComposer/     ← AutoCAD Plant 3D 2026 plugin (P3DCOMPOSER)
├── Plant3DSkeletonManager/    ← Shared scene graph + primitives.py (+ Inventor add-in)
├── catalog_generator/        ← Python/Lisp nguồn (deploy → CustomScripts)
│   ├── parts/                ← mọi part + thư viện hỗ trợ
│   │   ├── WN_FLRF_CL150/    ← catalog part (có catalog_entry.py)
│   │   ├── STUD_BOLTS/       ← thư viện (không có catalog_entry.py)
│   │   ├── NUTS/
│   │   └── STRUCTURAL_PROFILES/
│   ├── pipe_sizes.py, sw_fitting_geom.py
│   └── ScriptGroup.xml, variants.xml
├── scripts/                    ← Install / dev reload
├── BoxExtrudeAddIn/            ← Inventor sample add-in
└── BoxExtrudeAddIn.sln
```

## Hai vùng làm việc (ổ D vs ổ C)

| Vùng | Đường dẫn | Vai trò |
|------|-----------|---------|
| **Nguồn / dev** | `D:\02. Engineering\04. Autocad Plant 3D\API` | Sửa code C#, Python parts, thiết kế scene trong Composer |
| **Runtime Plant 3D** | `C:\AutoCAD Plant 3D 2026 Content\CPak Common\CustomScripts` | Plant 3D đọc catalog script tại đây |

**Luồng:**

1. **Thiết kế & preview** — mở `P3DCOMPOSER`, chỉnh part/scene. Scene JSON lưu tại `%AppData%\Plant3DCatalogComposer\scenes\`. Source Python nằm trong `catalog_generator/parts/` trên ổ D.
2. **Part OK** — bấm **Deploy Catalog** trên form Composer, hoặc chạy `.\scripts\Install-Plant3DCatalogComposer.ps1` để copy `.py`/`.xml` từ ổ D sang `CustomScripts` trên ổ C.
3. **Tạo spec / catalog** — sau deploy, Plant 3D chạy **`PLANTREGISTERCUSTOMSCRIPTS`** (nút Deploy Catalog gửi lệnh tự động). Lệnh này compile `.py` → `.pyc` và tạo `__pycache__` — **không tạo folder này thủ công**.

Part mới: tạo folder trong `catalog_generator/parts/{PART_ID}/`, thêm vào `ScriptGroup.xml`, rồi install. Install **gộp** `catalog_entry.py` + geometry thành một file `CUST_{PART_ID}.py` trên ổ C (không copy thêm folder part). Thư viện hỗ trợ (`STUD_BOLTS`, `NUTS`, `STRUCTURAL_PROFILES`) cũng nằm trong `catalog_generator/parts/` (cùng folder với catalog part, **không** có `catalog_entry.py`). Install copy chúng thẳng ra `CustomScripts/STUD_BOLTS/` … trên ổ C — import Python: `from STUD_BOLTS import …`.

**Ổ C (CustomScripts) sau deploy** — chỉ file/folder runtime:

| Loại | Ví dụ |
|------|--------|
| Catalog parts | `CUST_*.py`, `CUST_*.xml` |
| Metadata | `ScriptGroup.xml`, `variants.xml`, `variants.map`, `standard_sets.json` |
| Shared Python | `pipe_sizes.py`, `primitives.py`, `sw_fitting_geom.py` |
| Support modules | `STUD_BOLTS/`, `NUTS/`, `STRUCTURAL_PROFILES/` |
| Composer (preview) | `p3d_composer/`, `hot_reload.py`, `wrapper.py`, `Wrapper.lsp` |

## Plant 3D Catalog Composer

### Build & cài

```powershell
cd "D:\02. Engineering\04. Autocad Plant 3D\API"
dotnet build Plant3DCatalogComposer\Plant3DCatalogComposer.csproj -c Release
.\scripts\Install-Plant3DCatalogComposer.ps1
```

Restart Plant 3D (hoặc `NETLOAD` DLL), rồi chạy `P3DCOMPOSER`.

### Đường dẫn runtime (không nằm trong repo — do install script ghi)

| Mục đích | Đường dẫn |
|----------|-----------|
| Plugin DLL | `%AppData%\Autodesk\ApplicationPlugins\Plant3DCatalogComposer.bundle\` |
| Scene JSON | `%AppData%\Plant3DCatalogComposer\scenes\{drawing}.scene.json` |
| Python deploy | `C:\AutoCAD Plant 3D 2026 Content\CPak Common\CustomScripts\` |
| Composer lib | `...\CustomScripts\p3d_composer\` (`scene_builder.py`, `composer_live.py` sinh khi Save) |
| Wrapper | `...\CustomScripts\wrapper.py` (từ `wrapper_patched.py`) |

### Luồng rebuild

```
Form → scene JSON → Idle → ERASE (lần 2+) → testacpscript wrapper → hot_reload → scene_builder
```

Lệnh: `P3DCOMPOSER`, `P3DREBUILD`, `P3DCOMPWRAP`, hoặc `COMPWRAP` / `COMPWRAPFRESH` trong `Wrapper.lsp`.

---

## Inventor (BoxExtrudeAddIn, Plant3DSkeletonManager)

Xem thư mục `BoxExtrudeAddIn\` và `scripts\Install-Plant3DAddIn.ps1`.

```powershell
cd "D:\02. Engineering\04. Autocad Plant 3D\API\BoxExtrudeAddIn"
dotnet build -c Release
```

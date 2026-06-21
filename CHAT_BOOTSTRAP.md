# CHAT_BOOTSTRAP — Plant 3D Catalog Factory

> Dán toàn bộ nội dung file này vào cuộc hội thoại Cursor mới để bootstrap context.

---

## Bạn đang làm việc với

**Plant 3D Catalog Factory** — nền tảng C# + Python tạo catalog component tùy chỉnh cho **AutoCAD Plant 3D 2026**, thay thế quy trình thủ công Python / Catalog Builder / Spec Editor.

**Workspace**: `D:\02. Engineering\04. Autocad Plant 3D\API`

**Mục tiêu cuối**: ứng dụng duy nhất sinh geometry → Python → deploy → Excel → `.pcat` → spec, không cần thao tác thủ công.

**Chi tiết đầy đủ**: đọc `PROJECT_CONTEXT.md` trong cùng repo.

---

## Cấu trúc repo

```
Plant3DCatalogComposer/   ← AutoCAD plugin (P3DCOMPOSER) — UI chính
Plant3DSkeletonManager/   ← Inventor add-in + Core domain + primitives.py
catalog_generator/        ← Python parts, pipe_sizes.py, p3d_composer/
scripts/                  ← install, deploy, audit
```

**Runtime Plant 3D**: `C:\AutoCAD Plant 3D 2026 Content\CPak Common\CustomScripts`

**Dev config**: `deploy.json` → trỏ `catalog_generator` về repo dev.

---

## Luồng pipeline (đang hoạt động)

```
Composer UI → Scene JSON (ValveProject)
  → Python Generator (CUST_*.py, ScriptGroup.xml)
  → Deploy CustomScripts → PLANTREGISTERCUSTOMSCRIPTS (.pyc)
  → Excel Export (từ part.json library — KHÔNG phải scene)
  → [THỦ CÔNG] Catalog Builder Publish .pcat
```

---

## Kiến trúc tóm tắt

```
UI (ComposerForm: Catalog|Scene|Booleans|Ports|Code)
  ↓
Shared Core (ValveProject, PrimitiveNode, ConnectionPort, BooleanOperation)
  ↓
Geometry (primitives.py, TransformMath, scene_builder.py)
  ↓
Port Engine (PortService, PortTransformMath)
  ↓
Python Generator (CatalogCodeGenerator, PythonCodeGenerator)
  ↓
Deploy (CatalogDeployService → CustomScripts)
  ↓
Excel Exporter (CatalogExcelExportService)
  ↓
Plant 3D Runtime
```

Core được **link-compile** từ `Plant3DSkeletonManager/Core` vào Composer — sửa Core ảnh hưởng cả hai.

---

## Chức năng đã có (không suy đoán)

- **22 primitives** + boolean ops (UNION/SUBTRACT/INTERSECT) + fillet/chamfer cutters
- **Transform**: move jog/align, rotate world/local, reparent
- **Port Manager**: CRUD, pick, jog, rotate direction, 10 end types
- **20 catalog parts** deploy được (flange, gasket, BW Sch40, SW CL3000, LJ stub/ring)
- **Deploy pipeline** + preflight + manifest + test (`testacpscript`)
- **Excel export** (UI + headless CLI) — nguồn: 19 part có `part.json`
- **Catalog Setup**: insert library, metadata, valve skeleton, auto dimensions
- **Scene JSON** Import/Export; Generate Code tách khỏi Deploy
- **Boolean preview** thực thi trong `scene_builder.py` (Inventor: chỉ visual)
- **Valve skeleton UI** (5 types) — catalog valve WIP (`GATEVALVE_DN50_150` thiếu `part.json`)
- **Inventor add-in** cho authoring song song

**Chưa có**: PCAT exporter, PSPX exporter, instrument catalog, ISOGEN PDMS-style.

---

## Đối tượng dữ liệu chính

| Object | Vai trò |
|--------|---------|
| `ValveProject` | Root scene document (JSON) |
| `PrimitiveNode` | Scene node — primitive hoặc catalog reference |
| `ConnectionPort` | Port connection point |
| `SkeletonParameters` | Global DN, BodyOD, pressure class, … |
| `CustomPartDefinition` | Part metadata từ `part.json` |
| `CatalogPackage` | 6-file deploy bundle |
| `BooleanOperation` | Boolean metadata (target + tools) |

---

## Ràng buộc bắt buộc

1. Tương thích **Plant 3D 2026** + `primitives.py` + Python generator hiện tại.
2. **Không phá** pipeline Deploy → Register → Excel đang hoạt động.
3. Units: **mm**.
4. Deploy format: flat merged `CUST_{ID}.py` trong CustomScripts.
5. SSOT kích thước geometry: `catalog_generator/pipe_sizes.py` (C# tables là mirror).

---

## Technical debt cần biết

- Hardcode path Plant 3D 2026 (`ProjectPaths.CustomScriptsDir`)
- Bảng kích thước Python ↔ C# duplicate (chỉ LJ/stub có auto-sync script)
- `standard_sets.json` không được C# parse — lists hardcode riêng
- Hai `PrimitiveCatalog` (Composer vs SkeletonManager) diverge
- `ComposerForm` partial class lớn, UI + logic trộn
- Excel export lấy từ `part.json` library, không phải scene; `ValveProject?` param unused
- Boolean: metadata-only trên Inventor; thực thi trên Composer preview Python
- PCAT vẫn bước thủ công sau Excel

---

## Lộ trình ưu tiên (chưa triển khai)

1. **PCAT Exporter** — tự động hóa `.pcat` từ metadata
2. **Valve Catalog** — hoàn thiện thư viện valve + ports
3. **Instrument Catalog**
4. **PSPX Exporter**
5. **ISOGEN PDMS-style** customization

---

## Commands & build

```powershell
dotnet build Plant3DCatalogComposer\Plant3DCatalogComposer.csproj -c Release
.\scripts\Install-Plant3DCatalogComposer.ps1
# Trong Plant 3D: P3DCOMPOSER
```

| Command | Chức năng |
|---------|-----------|
| `P3DCOMPOSER` | Mở palette |
| `P3DCOMPDEPLOY` | Deploy catalog |
| `P3DCOMPPUBLISH` | Export Excel |
| `P3DCOMPTEST` | Test CUST script |
| `P3DREBUILD` | Rebuild scene preview |

---

## Quy tắc khi code

- **Minimize scope** — diff nhỏ, không refactor không yêu cầu.
- **Match conventions** — đọc code xung quanh trước khi sửa.
- **Không suy đoán** feature chưa có trong source.
- Cập nhật `PROJECT_CONTEXT.md` khi thêm feature lớn.

---

*Bootstrap v2 — 2026-06-21 (review). Tham chiếu: PROJECT_CONTEXT.md mục 11*

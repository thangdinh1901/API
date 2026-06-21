# PROJECT_CONTEXT.md

> Tài liệu kiến trúc lâu dài cho workspace **Plant 3D / Inventor API**.  
> Cập nhật: 2026-06-21 (review lần 2) — mọi mục dưới đây đối chiếu trực tiếp với source code trên disk.

---

## 1. Tổng quan dự án

### Dự án đang làm gì?

Xây dựng **Plant 3D Catalog Factory** — nền tảng C# + Python để tạo, quản lý và triển khai catalog component tùy chỉnh cho **Autodesk AutoCAD Plant 3D 2026**, thay thế quy trình thủ công với Python, Catalog Builder và Spec Editor.

Luồng chính:

1. Thiết kế geometry trong **Composer UI** (scene graph + primitives + booleans + ports).
2. Sinh **Python catalog script** (`CUST_*.py`) và metadata (`ScriptGroup.xml`, `variants.xml`).
3. **Deploy** sang `CustomScripts` của Plant 3D → `PLANTREGISTERCUSTOMSCRIPTS`.
4. **Export Excel** (Catalog Builder format) — xuất **thư viện part chuẩn** từ `part.json`, không phải scene hiện tại → người dùng **Publish `.pcat` thủ công** trong Catalog Builder → import spec.

### Mục tiêu cuối cùng

Một ứng dụng duy nhất cho phép tạo **bất kỳ custom component** nào (fitting, flange, valve, instrument, …) mà **không cần** thao tác trực tiếp với Python thủ công, Catalog Builder GUI, hay Spec Editor — bao gồm cả xuất `.pcat`, `.pspx` và tùy biến ISOGEN trong tương lai.

### Người dùng mục tiêu

| Persona | Vai trò |
|---------|---------|
| **Piping / Plant 3D engineer** | Thiết kế component, chèn vào spec, kiểm tra placement |
| **Catalog administrator** | Deploy, publish Excel, quản lý thư viện part chuẩn |
| **Developer / integrator** | Mở rộng thư viện part, bảng kích thước ASME, pipeline CI |

### Mức độ hoàn thiện hiện tại

| Khu vực | Trạng thái | Ghi chú |
|---------|------------|---------|
| Scene graph + geometry preview | **Hoạt động** | JSON sidecar, rebuild qua `wrapper` / `scene_builder.py` |
| Thư viện part chuẩn (BW/SW/Flange/Gasket/LJ) | **Hoạt động** | 20 `CUST_*` scripts deploy được; 19 có `part.json` |
| Deploy pipeline | **Hoạt động** | Preflight → export → copy CustomScripts → register |
| Excel export | **Hoạt động** | UI + headless CLI; nguồn = `CustomPartCatalog` / `part.json` (19 part), không phải scene |
| PCAT export | **Chưa có trong C#** | Bước thủ công qua Catalog Builder sau Excel |
| Valve catalog | **WIP** | `GATEVALVE_DN50_150` deploy được nhưng thiếu `part.json`; skeleton valve types có trong UI |
| Instrument catalog | **Chưa có** | Chỉ enum `Instrument` trong `CatalogPlantGroups` |
| PSPX / ISOGEN | **Chưa có** | Chỉ metadata ISO cơ bản trong `CatalogExcelIsoMetadata` |
| Inventor add-in (`Plant3DSkeletonManager`) | **Hoạt động** | Authoring valve skeleton song song với Composer |

---

## 2. Mục tiêu cốt lõi

Xây dựng nền tảng tạo **Plant 3D Catalog hoàn chỉnh bằng C#**.

Ứng dụng phải cho phép tạo custom component **không cần thao tác thủ công** với:

- Python (authoring — generation tự động)
- Catalog Builder (GUI — thay bằng Excel export + PCAT exporter tương lai)
- Spec Editor (metadata sync tự động qua deploy)

Ứng dụng sẽ trở thành **Plant 3D Catalog Factory**.

---

## 3. Các chức năng đã hoàn thành

> Chỉ liệt kê chức năng **tồn tại trong source code**.

### 3.1 AutoCAD Plugin — Plant3DCatalogComposer

| Chức năng | Vị trí code | Mô tả |
|-----------|-------------|-------|
| **Plugin load / Ribbon** | `Plugin.cs`, `RibbonService.cs` | Tab "P3D Composer", nút Composer + Rebuild |
| **Palette UI** | `ComposerForm*.cs`, `PaletteManager.cs` | 5 tab: Catalog, Scene, Booleans, Port Manager, Code |
| **Scene tools (UI)** | `ComposerForm.cs` | Generate Code, Deploy, Publish, Test, Rebuild, Import/Export JSON |
| **AutoCAD commands** | `Commands.cs` | `P3DCOMPOSER`, `P3DREBUILD`, `P3DCOMPDEPLOY`, `P3DCOMPPUBLISH`, `P3DCOMPTEST`, `P3DCOMPPICKPOINT`, `P3DCOMPPICKDIST`, `P3DCOMPWRAP` |
| **Plugin autoload** | `PackageContents.xml` | ApplicationPlugins bundle; đăng ký subset commands (không gồm `P3DCOMPDEPLOY`, `P3DCOMPPICKPOINT`, `P3DCOMPPICKDIST`) |

### 3.2 Primitive Library

| Chức năng | Chi tiết |
|-----------|----------|
| **22 loại primitive** | `PrimitiveType` enum trong `Plant3DSkeletonManager/Core/Model.cs` |
| **Catalog định nghĩa** | `Plant3DCatalogComposer/PrimitiveCatalog.cs` — Box, Cylinder, Cone, Torus, Sphere, Half Sphere, Reduced Elbow, Elbow, Segmented Elbow, Ellipsoid Head (×2), Ellipsoid Segment, Pyramid, Round Rectangle, Sphere Segment, Torispheric Head (×3), + 4 cutter types |
| **Insert vào scene** | `PrimitiveService.cs` |

### 3.3 Geometry Engine

| Chức năng | Chi tiết |
|-----------|----------|
| **Python runtime** | `Plant3DSkeletonManager/primitives.py` — wrapper quanh Plant 3D `varmain.primitiv` |
| **Transform math** | `TransformMath.cs`, `PortTransformMath.cs` |
| **Expression evaluator** | `ExpressionEvaluator.cs` — `"BodyOD * 0.5"` |
| **Scene rebuild** | `SceneRebuildService`, `IdleRebuildService`, `CompWrapCommand` → `wrapper` → `scene_builder.py` |
| **Boolean trong preview** | `scene_builder._apply_booleans()` — UNION / SUBTRACT / INTERSECT trên `ShapeObject` (Composer path) |
| **Live preview script** | `ComposerLiveScriptService` → `composer_live.py` |

### 3.4 Transform Operations (Scene tab)

| Thao tác | Implementation |
|----------|----------------|
| **Move (jog)** | ±X/±Y/±Z step → `TransformMath.TranslateWorld` |
| **Move (align)** | `P3DCOMPPICKDIST` → displacement pick |
| **Move (measure step)** | Đặt step từ khoảng cách đo |
| **Rotate quanh tâm (local)** | `RotateLocal`, `RotateLocalInBodyFrame` + `RotationJog` |
| **Rotate quanh gốc WCS (world)** | `RotateWorld` |
| **Reparent / Delete / Duplicate** | `SceneGraphEditor` |
| **Scene tree UI** | Drag-drop reparent; Delete key / nút Delete |
| **Parameter resolve** | `SceneGraphEditor.ResolveExpressions()` — eval expression theo skeleton |

### 3.4.1 Catalog Setup tab

| Chức năng | Chi tiết |
|-----------|----------|
| **Catalog project metadata** | `CatalogProjectService.Apply()` — tên, group, DN/DN2, class, schedule, tooltips |
| **Insert thư viện chuẩn** | `CatalogPartService.Insert()` + `CustomPartCatalog` — chèn `SceneNodeKind.Catalog` |
| **DN / schedule UI** | `PipeSizeCatalog`, `PipeScheduleCatalog`, `CatalogCategories` |
| **Valve skeleton (optional)** | `ParameterService.SuggestDimensions()`, `ApplySkeleton()` — 5 valve types |
| **Auto dimensions từ DN** | `FittingDimensionService.SyncProjectDimensions()` — BodyOD, ElbowCenterToFace, … |

### 3.4.2 Scene JSON Import / Export

| Chức năng | Chi tiết |
|-----------|----------|
| **Export JSON** | `SceneImportExport.SaveJsonFile()` — nút Export trên Scene tab |
| **Import JSON** | `SceneImportExport.LoadJsonFile()` + `ReplaceProject()` — nút Import |
| **Persist tự động** | `DocumentStore.Save()` — `%AppData%/Plant3DCatalogComposer/scenes/{dwg}.scene.json` |
| **Mirror runtime** | `DocumentStore.MirrorToCustomScripts()` — `.active_scene.json`, `.p3d_composer_mode`, `.p3d_composer_scene_path` |

### 3.5 Fillet / Chamfer

Không có lệnh fillet/chamfer trực tiếp trên solid. Thực hiện qua:

- **Cutter primitives**: `FILLET`, `CYLINDER_CHAMFERED`, `BOX_WITH_FILLET`, `CYLINDER_WITH_FILLET`
- **Boolean subtract** trên tab Booleans (`BooleanGraph` — UNION / SUBTRACT / INTERSECT)

### 3.6 Port Manager

| Chức năng | Chi tiết |
|-----------|----------|
| CRUD ports | `PortService` — Add, Delete, Copy, Apply |
| Pick position + direction | `P3DCOMPPICKPOINT` |
| Move port (jog) | ±X/±Y/±Z |
| Rotate port direction | World / local |
| Visual markers | `PortVisualService` — layer `P3D_COMPOSER_PORTS` |
| End types | FL, BV, PL, SW, THDM, THDF, SO, WF, LAP, GRV |

### 3.7 Python Generator

| Output | Generator |
|--------|-----------|
| `composer_live.py` (preview) | `PythonCodeGenerator`, `ComposerLiveScriptService` |
| `CUST_{PART_ID}.py` (catalog) | `CatalogCodeGenerator` |
| Merge entry + geometry | `CatalogDeployService.MergeCatalogPartPy()` |
| Metadata sync | `CatalogMetadataSyncService` → `ScriptGroup.xml`, `variants.xml` |

### 3.8 PYC Compiler

**Không có PYC compiler tùy chỉnh.** Plant 3D biên dịch `.py` → `.pyc` khi `PLANTREGISTERCUSTOMSCRIPTS` chạy.

Quản lý cache: `CatalogDeployService.ClearPythonCache()`, `RemovePycacheForScript()`.

### 3.9 Excel Exporter

> **Quan trọng:** Excel export lấy dữ liệu từ **`catalog_generator/parts/*/part.json`** qua `CatalogExcelPartResolver.DiscoverExportParts()`, **không** serialize scene graph hiện tại. Tham số `ValveProject?` trong `CatalogExcelExportService.Export()` hiện **không được dùng** trong body (dead parameter).

| Thành phần | Chi tiết |
|------------|----------|
| Template | `Resources/CatalogBuilderTemplate.xlsx` — sheet tên `{PART_ID},…` |
| Export service | `CatalogExcelExportService` (ClosedXML) |
| Headless CLI | `CatalogExcelExportCli.ExportAll()` → `scripts/ExportCatalogExcel/` exe + `Export-CatalogExcel.ps1` |
| Part resolver | `CatalogExcelPartResolver` — GUID, ports (regex `catalog_entry.py`), collar aliases |
| Size tables | `CatalogExcelSizeCatalog`, `BwFittingSizeCatalog`, `SwFittingSizeCatalog`, `CatalogStubEndTable`, … |
| Flange bolting | `CatalogFlangeBoltingCatalog` — mirror `STUD_BOLTS/bolting_data.py` |
| SO flange CEL | `CatalogSoFlangeCelTable` |
| ISO metadata | `CatalogExcelIsoMetadata`, `CatalogExcelShortDescription` |
| Lap joint aliases | `CatalogLapJointIds` — stub → collar export rows |
| Group remap warning | `CatalogGroupResolver` — Valve + BV ports → Fitting (Spec Editor) |

### 3.10 Catalog Workflow

```
[Scene] Generate Code → (optional) Export package to parts/
Deploy: Preflight → [Generate if scene non-empty] → Copy CustomScripts → Sync metadata → Register
Publish: Preflight → Excel from part library → [manual .pcat in Catalog Builder]
Preview: Save scene → IdleRebuild / P3DREBUILD → wrapper → scene_builder.py
```

| Bước | Service / Command |
|------|-------------------|
| Generate Code (UI) | `ComposerLiveScriptService.BuildCatalogPackage()` → `CatalogExportService.Export()` |
| Bảo vệ thư viện chuẩn | `StandardCatalogGuard` — không ghi đè part ASME; sandbox `_composer_exports/` |
| Preflight deploy | `CatalogPreflightService.ValidateForDeploy()` |
| Preflight publish | `CatalogPreflightService.ValidateForExcelPublish()` |
| Full deploy | `CatalogDeployFullService.Deploy()` |
| Copy scripts | `CatalogDeployService.DeployToCustomScripts()` |
| Deploy manifest + hướng dẫn | `CatalogDeployManifestWriter`, `CatalogDeployGuidance` |
| Publish Excel | `CatalogPublishService.Publish()` / `P3DCOMPPUBLISH` |
| Test script | `CatalogTestService` → `testacpscript "CUST_..."` |
| Plugin staging | `CatalogPluginDeployService.TryStagePluginDll()` → bundle + CustomScripts NETLOAD |
| Port/import templates | `CatalogPortTemplates.TryBuildFlatCatalogImport()` |

### 3.11 Thư viện Part chuẩn (`catalog_generator/parts/`)

**20 part deploy được** (có `catalog_entry.py` trên disk; `ScriptGroup.xml` root commit có 20 entries):

| Nhóm | Part IDs |
|------|----------|
| Flange CL150 RF | `BLD_FLRF_CL150`, `WN_FLRF_CL150`, `SO_FLRF_CL150`, `LJ_RING_CL150_RF` |
| Gasket CL150 | `GSK_RF_CL150`, `GSK_FF_CL150` |
| BW Sch40 | `ELBOW_45_LR_BW_SCH40`, `ELBOW_90_LR_BW_SCH40`, `ELBOW_90_SR_BW_SCH40`, `REDUCER_CONC_BW_SCH40`, `REDUCER_ECC_BW_SCH40`, `TEE_EQ_BW_SCH40`, `TEE_REDUCE_BW_SCH40`, `STUBEND_LJ_A_BW_SCH40`, `STUBEND_LJ_A_SH_BW_SCH40` |
| SW CL3000 | `ELBOW_45_SW_CL3000`, `ELBOW_90_SW_CL3000`, `TEE_EQ_SW_CL3000`, `TEE_REDUCE_SW_CL3000` |
| Valve (WIP) | `GATEVALVE_DN50_150` (có `catalog_entry.py`, **không** có `part.json`) |

**19 part có `part.json`** — tất cả trên trừ `GATEVALVE_DN50_150`.

**Support modules** (deploy folder, không phải catalog part): `STUD_BOLTS`, `NUTS`, `STRUCTURAL_PROFILES`.

**Shared Python** (deploy root CustomScripts): `pipe_sizes.py`, `catalog_params.py`, `stubend_geom.py`, `sw_fitting_geom.py`, `lj_stud_bolts.py`, `primitives.py`.

**Composer runtime** (`p3d_composer/`): `scene_builder.py`, `catalog_transforms.py`, `wrapper_patched.py` + `p3d_composer_rebuild.py` ở root.

**Metadata deploy**: `ScriptGroup.xml`, `variants.xml`, `variants.map`, `standard_sets.json` — regenerate bởi `CatalogMetadataSyncService.SyncFromParts()` mỗi lần deploy.

**Export aliases** (sinh trong C#, không có folder riêng): `COLLAR_LJ_A_BW_SCH40`, `COLLAR_LJ_A_SH_BW_SCH40`.

### 3.12 Valve Skeleton (UI only)

5 loại trong `ParameterService.ValveTypes`: Gate, Globe, Ball, Butterfly, Check — dùng cho composite scene trên tab Catalog, không phải part library hoàn chỉnh.

### 3.13 Inventor Add-in — Plant3DSkeletonManager

| Chức năng | Chi tiết |
|-----------|----------|
| Dockable UI | `SkeletonForm` — authoring valve skeleton trong `.iam` |
| Inventor templates | `TemplateBuilders*.cs` — 18 parametric `.ipt` (`PrimitiveCatalog.All`) |
| Sync Inventor ↔ graph | `SyncService.Pull()`, `PushService`, `OccurrenceTagger` |
| Boolean **chỉ visual** | `BooleanAppearanceService` — tô màu tool solid; **không** combine geometry trong Inventor |
| JSON import/export | `SceneImportExport` — file JSON; `RebuildFromProject()` |
| Shared Core | Cùng `ValveProject` model với Composer |

### 3.14 Build / Install

| Chức năng | Chi tiết |
|-----------|----------|
| **Auto install sau build** | `Plant3DCatalogComposer.csproj` target `DeployPlant3DCatalogComposer` → `Install-Plant3DCatalogComposer.ps1` |
| **ApplicationPlugins bundle** | `%AppData%/Autodesk/ApplicationPlugins/Plant3DCatalogComposer.bundle` |
| **Wrapper.lsp** | Copy bởi install script → CustomScripts; lệnh `COMPWRAP` / `COMPWRAPFRESH` |
| **NETLOAD fallback** | `Plant3DCatalogComposer.dll` staged vào CustomScripts |

### 3.15 Scripts QA / bảo trì

`Deploy-Catalog.ps1`, `Sync-CatalogMetadata.ps1`, `sync_lap_joint_cs_tables.py`, `audit_*.py`, `verify_lj_deploy.py`, `Install-Plant3DCatalogComposer.ps1`, `Install-Plant3DAddIn.ps1`, …

---

## 4. Kiến trúc hiện tại

### 4.1 Workspace layout

```
API/
├── Plant3DCatalogComposer/     ← AutoCAD Plant 3D 2026 plugin (P3DCOMPOSER)
├── Plant3DSkeletonManager/     ← Inventor add-in + Core domain + primitives.py
├── catalog_generator/          ← Python nguồn (deploy → CustomScripts)
│   ├── parts/                  ← catalog parts (part.json + catalog_entry.py)
│   ├── pipe_sizes.py           ← bảng kích thước ASME (Python SSOT)
│   ├── catalog_params.py       ← resolve DN / preview kwargs
│   ├── stubend_geom.py, sw_fitting_geom.py, lj_stud_bolts.py
│   ├── Wrapper.lsp             ← COMPWRAP helpers (install → CustomScripts)
│   └── p3d_composer/           ← scene_builder, catalog_transforms, wrapper_patched
├── scripts/                    ← install, deploy, audit, sync
│   └── ExportCatalogExcel/     ← headless exe wrapper cho Excel export
└── BoxExtrudeAddIn/            ← Inventor sample (không thuộc pipeline catalog)
```

### 4.2 Sơ đồ kiến trúc (text)

```
┌─────────────────────────────────────────────────────────────────┐
│  UI Layer                                                       │
│  ComposerForm (Catalog | Scene | Booleans | Ports | Code)       │
│  RibbonService / PaletteManager / SkeletonForm (Inventor)       │
└───────────────────────────┬─────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────┐
│  Scene Graph (Shared Core — Plant3DSkeletonManager/Core)        │
│  ValveProject → PrimitiveNode, ConnectionPort, BooleanOperation  │
│  DocumentStore (JSON) / ProjectValidator / SceneGraphEditor     │
└───────────┬─────────────────────────────┬───────────────────────┘
            │                             │
┌───────────▼──────────┐    ┌─────────────▼──────────────────────┐
│  Geometry Engine     │    │  Port Engine                        │
│  TransformMath       │    │  PortService / PortTransformMath    │
│  ExpressionEvaluator │    │  PortVisualService                  │
│  primitives.py       │    └─────────────┬──────────────────────┘
└───────────┬──────────┘                  │
            │                             │
┌───────────▼─────────────────────────────▼──────────────────────┐
│  Python Generator                                                 │
│  PythonCodeGenerator (preview) / CatalogCodeGenerator (catalog)  │
│  ComposerLiveScriptService / CatalogDeployService (merge)         │
└───────────────────────────┬──────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────┐
│  Deploy & Metadata                                                │
│  CatalogDeployFullService → CustomScripts                         │
│  CatalogMetadataSyncService → ScriptGroup.xml, variants.xml       │
│  PLANTREGISTERCUSTOMSCRIPTS → .pyc (Plant 3D native)              │
└───────────────────────────┬─────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────┐
│  Excel Exporter (độc lập — đọc part library, không cần scene)   │
│  CatalogExcelExportService → CatalogBuilderTemplate.xlsx          │
└───────────────────────────┬─────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────┐
│  Plant 3D Runtime                                                 │
│  CustomScripts/CUST_*.py → Catalog Builder → .pcat → Spec Editor  │
└───────────────────────────────────────────────────────────────────┘
```

**Lưu ý kiến trúc:** Deploy và Excel export là **hai nhánh độc lập**. Excel không yêu cầu scene graph; Deploy có thể chạy với scene rỗng (chỉ sync thư viện part).

### 4.3 Hai vùng làm việc

| Vùng | Đường dẫn | Vai trò |
|------|-----------|---------|
| **Nguồn / dev** | `D:\02. Engineering\04. Autocad Plant 3D\API` | Sửa C#, Python, scene |
| **Runtime Plant 3D** | `C:\AutoCAD Plant 3D 2026 Content\CPak Common\CustomScripts` | Plant đọc catalog script |

Cấu hình dev: `deploy.json` (AppData / plugin bundle / CustomScripts).

### 4.4 Mối quan hệ module

| Module | Phụ thuộc | Được dùng bởi |
|--------|-----------|---------------|
| **Core** (`Model.cs`, …) | Không (pure domain) | Composer, SkeletonManager |
| **primitives.py** | Plant 3D `varmain` | scene_builder, CUST_*.py, composer_live |
| **catalog_generator** | pipe_sizes, catalog_params | Deploy, Excel export, CustomPartCatalog |
| **Composer Services** | Core, AutoCAD API, ClosedXML | ComposerForm, Commands |
| **SkeletonManager Adapter** | Core, Inventor API | SkeletonForm |

Core được **link compile** vào Composer (`Plant3DCatalogComposer.csproj` lines 37–49) — cùng source, hai assembly.

### 4.5 Phụ thuộc bên ngoài (runtime / build)

| Phụ thuộc | Vai trò |
|-----------|---------|
| **AutoCAD 2026 .NET** | `AcCoreMgd`, `AcDbMgd`, `AcMgd`, `AdWindows` — plugin host |
| **Inventor 2026 .NET** | SkeletonManager add-in only |
| **ClosedXML 0.104.2** | Ghi `CatalogBuilderTemplate.xlsx` |
| **Plant 3D Python API** | `varmain.primitiv`, `varmain.custom`, `testacpscript`, `PLANTREGISTERCUSTOMSCRIPTS` |
| **deploy.json** | Chain: AppData → plugin bundle → CustomScripts — trỏ dev `catalog_generator` |
| **Application.Idle** | `IdleRebuildService` — rebuild preview khi CAD quiescent |
| **SQLite** (scripts only) | `audit_*_pcat.py` — **không** có trong C# plugin |

---

## 5. Các đối tượng dữ liệu chính

### 5.1 ValveProject (root document)

| Thuộc tính | Mô tả |
|------------|-------|
| `SchemaVersion` | Phiên bản JSON (default `"1.0"`) |
| `ValveName` | Tên catalog / component |
| `CatalogGroup` | Plant 3D `@activate` group |
| `TooltipShort`, `TooltipLong` | Tooltip metadata cho variants.xml |
| `CustomPartId` | ID part library (e.g. `WN_FLRF_CL150`) |
| `Units` | Default `"mm"` |
| `Parameters` | `SkeletonParameters` — DN, BodyOD, … |
| `Parts` | `List<PrimitiveNode>` |
| `Operations` | `List<BooleanOperation>` |
| `Ports` | `List<ConnectionPort>` |
| `ShowPortMarkers` | Vẽ port arrows trên DWG |

**Quan hệ**: Root chứa toàn bộ scene; serialize JSON qua `JsonCodec`; persist bởi `DocumentStore`.

### 5.2 PrimitiveNode

| Thuộc tính | Mô tả |
|------------|-------|
| `Kind` | `Primitive` hoặc `Catalog` |
| `Type` | `PrimitiveType` enum |
| `CatalogPartId` | Khi `Kind=Catalog` |
| `Origin[3]`, `Direction[3]`, `Rotation[9]` | Transform WCS (mm) |
| `RotationJogs` | Lịch sử rotate cho preview replay |
| `Parameters` | `Dictionary<string, ParamValue>` |
| `Parent` | Scene tree parent |

**Quan hệ**: Thuộc `ValveProject.Parts`; có thể có `ConnectionPort` gắn qua `ParentNodeId`; target/tool của `BooleanOperation`.

### 5.3 ConnectionPort

| Thuộc tính | Mô tả |
|------------|-------|
| `Number` | 1-based port index |
| `Type` | `PortConnectionType` (FL, BV, LAP, …) |
| `Id` | GUID |
| `ParentNodeId` | Local vs world coords |
| `Position[3]`, `Direction[3]` | mm, unit vector |
| `Name` | Legacy — ignored on save |

**Quan hệ**: Thuộc `ValveProject.Ports`; sinh `add_ports()` trong Python catalog; xuất Excel qua `CatalogExcelPartResolver`.

### 5.4 SkeletonParameters

| Thuộc tính | Mô tả |
|------------|-------|
| `DN`, `DN2` | Nominal diameter (mm) |
| `PressureClass`, `PipeSchedule` | Class / schedule |
| `BodyOD`, `BodyLength`, `FaceToFace`, `ElbowCenterToFace`, … | Kích thước valve/fitting |

**Quan hệ**: Global params; tham chiếu trong `ParamValue.Expression`; resolve bởi `ExpressionEvaluator`.

### 5.5 BooleanOperation

| Thuộc tính | Mô tả |
|------------|-------|
| `Order`, `Type` | UNION / SUBTRACT / INTERSECT |
| `Target`, `Tools[]` | GUID references |

**Quan hệ**: Metadata cho rebuild Python.

- **Composer / Plant 3D preview:** `scene_builder._apply_booleans()` thực thi UNION/SUBTRACT/INTERSECT trên geometry.
- **Inventor add-in:** comment trong `Model.cs` — *"Inventor solids are never actually combined"*; chỉ `BooleanAppearanceService` tô màu.

### 5.6 CustomPartDefinition

| Thuộc tính | Mô tả |
|------------|-------|
| `Role` | `standard` / `composite` |
| `Id`, `DisplayName`, `Group`, `Category` | Metadata catalog |
| `CatalogParams` | Param spec cho Excel/UI |
| `StandardSet` | e.g. `BW_SCH40` |
| `CatalogFrameRotation` | Baked rotation matrix |

**Quan hệ**: Load từ `part.json` bởi `CustomPartCatalog`; dùng bởi Excel export và Catalog tab insert.

### 5.7 CatalogPackage

Bundle 6 file deployable: `catalog_entry.py/xml`, `CUST_*.py/xml`, `__INIT__.xml` — sinh bởi `CatalogCodeGenerator`.

### 5.8 CatalogExcelPartRow

Row metadata cho Excel: GUID, script path, port layout, size variants — build bởi `CatalogExcelPartResolver`.

### 5.9 ParamValue

`Value` (resolved) + optional `Expression` — per-node parameter.

### 5.10 PrimitiveDefinition

Định nghĩa primitive type: `Type`, `DisplayName`, `Prefix`, parameter specs. Composer version không có `BuildGeometry`; SkeletonManager version có Inventor delegate.

---

## 6. Đánh giá kỹ thuật (Technical Debt)

> Chỉ ghi nhận — **không sửa code**.

### 6.1 Code bị lặp / dữ liệu trùng

| Vấn đề | Chi tiết |
|--------|----------|
| **Bảng kích thước Python ↔ C#** | `pipe_sizes.py` SSOT; C# mirror: `CatalogStubEndTable`, `CatalogLjRingCl150Table`, `BwFittingSizeCatalog`, `CatalogFlangeCl150RfTable`, `CatalogFlangeBoltingCatalog`, `FittingDimensionService`, `CatalogSoFlangeCelTable`. Chỉ LJ/stub auto-sync (`sync_lap_joint_cs_tables.py`). |
| **`standard_sets.json` vs C# catalogs** | JSON có 3 sets (`BW_SCH40`, `LAP_JOINT_CL150`, `SW_CL3000`); C# hardcode trong `BwSch40StandardCatalog`, `SwCl3000StandardCatalog` — **C# không parse JSON**. |
| **Hai `PrimitiveCatalog`** | Composer (22 types, no Inventor) vs SkeletonManager (18 types + BuildGeometry) — diverge khi thêm primitive mới. |
| **`DocumentStore` / `SceneImportExport`** | Tồn tại ở cả Composer và SkeletonManager Adapter với logic tương tự, persistence khác (DWG sidecar vs Inventor attributes). |

### 6.2 Hardcode

| Vị trí | Giá trị hardcode |
|--------|------------------|
| `ProjectPaths.CustomScriptsDir` | `C:\AutoCAD Plant 3D 2026 Content\CPak Common\CustomScripts` |
| `Plant3DCatalogComposer.csproj` | `AcadDir` default `C:\Program Files\Autodesk\AutoCAD 2026` |
| `Install-Plant3DCatalogComposer.ps1` | `AcadYear = "2026"`, CustomScripts path |
| `CatalogDeployManifest.DeployVersion` | `"2026.06.11"` |
| Part ID lists | `BwSch40StandardCatalog`, `SwCl3000StandardCatalog`, `CatalogExcelShortDescription` — family-specific strings |

### 6.3 Coupling quá chặt

| Coupling | Rủi ro |
|----------|--------|
| **Core link-compile** | Sửa `Model.cs` ảnh hưởng cả Composer và SkeletonManager; không có NuGet package riêng. |
| **Composer ↔ CustomScripts runtime** | Deploy phụ thuộc đường dẫn Plant 3D cố định; scene JSON mirror sang CustomScripts. |
| **Excel export ↔ deployed scripts** | `CatalogExcelPartResolver` đọc port metadata từ `catalog_entry.py` bằng regex — fragile nếu format Python thay đổi. |
| **Rebuild pipeline** | Lisp `Wrapper.lsp` → Python `wrapper_patched.py` → `hot_reload` → `scene_builder.py` — nhiều tầng, khó debug. |
| **Excel export API** | `CatalogExcelExportService.Export(..., ValveProject? project)` — tham số `project` **không dùng** (dead parameter). |
| **PackageContents vs Commands.cs** | Autoload bundle không đăng ký `P3DCOMPDEPLOY`, `P3DCOMPPICKPOINT`, `P3DCOMPPICKDIST` — chỉ có trong `Commands.cs`. |

### 6.4 Khó bảo trì

| Phần | Lý do |
|------|-------|
| **`ComposerForm` partial class** | Logic UI + business trộn trong nhiều file partial; file chính lớn. |
| **`CatalogExcel*` family** | Nhiều class nhỏ theo part family (`CatalogExcelIsoMetadata`, `CatalogExcelShortDescription`, …) — thêm family mới = thêm file/logic rải rác. |
| **`CatalogCodeGenerator`** | Sinh Python/XML với TODO placeholders khi thiếu ports/parts. |
| **Audit scripts** | Nhiều script Python one-off trong `scripts/` với đường dẫn project cụ thể (`D:\04. Projects\...`). |

### 6.5 Rủi ro mở rộng

| Rủi ro | Mô tả |
|--------|-------|
| **Không có PCAT exporter** | Excel → Catalog Builder → `.pcat` vẫn thủ công; bottleneck cho automation. |
| **`GATEVALVE_DN50_150` thiếu `part.json`** | Deploy được nhưng invisible cho `CustomPartCatalog` và Excel export. |
| **Valve / Instrument chưa có pipeline hoàn chỉnh** | Chỉ skeleton UI + 1 gate valve WIP. |
| **Plant 3D version lock** | Path và API gắn 2026; nâng version cần sweep toàn repo. |
| **Boolean ops Inventor vs Composer** | Inventor: metadata-only. Composer preview: boolean thật trong Python — hành vi khác nhau nếu không rebuild. |

---

## 7. Lộ trình phát triển tiếp theo

> Chỉ mô tả yêu cầu — **không triển khai**.

### 7.1 PCAT Exporter (ưu tiên 1)

- Xuất trực tiếp file `.pcat` (SQLite catalog database) từ dữ liệu Excel/metadata hiện có.
- Loại bỏ bước thủ công mở Catalog Builder → Publish.
- Phải tương thích schema Plant 3D 2026 `.pcat` (tham chiếu audit scripts hiện có: `audit_*_pcat.py`).

### 7.2 Valve Catalog (ưu tiên 2)

- Hoàn thiện thư viện valve (gate, globe, ball, butterfly, check) với `part.json` đầy đủ.
- Port placement chính xác qua Port Manager (thay TODO placeholders).
- Size variants theo DN range và pressure class.
- Parametric body từ valve skeleton (`ParameterService`).

### 7.3 Instrument Catalog (ưu tiên 3)

- Thêm part library cho instrument (group `Instrument` đã có trong `CatalogPlantGroups`).
- ISO symbol metadata mở rộng.
- Connection types phù hợp instrument (THDM, THDF, …).

### 7.4 PSPX Exporter (ưu tiên 4)

- Xuất spec sheet `.pspx` từ catalog đã publish.
- Đồng bộ với `.pcat` và Excel export.

### 7.5 Tùy biến ISOGEN theo phong cách PDMS (ưu tiên 5)

- Custom ISOGEN symbol mapping và dimension style.
- Metadata ISO trong export pipeline (mở rộng `CatalogExcelIsoMetadata`).
- **Giả định (Assumption)**: PDMS-style ISOGEN = symbol naming, dimension annotation, và material takeoff conventions theo chuẩn nội bộ — cần spec chi tiết khi triển khai.

---

## 8. Các ràng buộc kỹ thuật

| Ràng buộc | Chi tiết |
|-----------|----------|
| **Autodesk Plant 3D 2026** | Target runtime; API qua `varmain.primitiv`, `testacpscript`, `PLANTREGISTERCUSTOMSCRIPTS` |
| **AutoCAD .NET API** | `AcCoreMgd`, `AcDbMgd`, `AcMgd`, `AdWindows` — net8.0-windows x64 |
| **Geometry Engine hiện tại** | `primitives.py` + `TransformMath` — không thay thế bằng engine khác |
| **Python Generator hiện tại** | `CatalogCodeGenerator` / `PythonCodeGenerator` output format — breaking change cần migration |
| **Catalog Builder Excel template** | `CatalogBuilderTemplate.xlsx` — sheet `{PART_ID},*` phải tồn tại; part không có sheet → skipped |
| **Units** | Millimeter (`LengthUnit="mm"`) |
| **Deploy target** | Flat `CUST_*.py` trong CustomScripts (merged entry + geometry) |
| **ClosedXML** | Package NuGet bắt buộc cho Excel export |
| **deploy.json** | Dev workflow: trỏ `CatalogGenerator` về repo; `PrimitivesPy` override tùy chọn |
| **Inventor 2026** (optional path) | SkeletonManager add-in — không bắt buộc cho Composer workflow |

**Nguyên tắc**: Tránh mọi thay đổi phá vỡ pipeline Deploy → Register → Excel → Plant 3D insert đang hoạt động.

---

## 9. Đề xuất cấu trúc dự án lâu dài

> Đề xuất refactor tổ chức — **chưa triển khai**.

```
Plant3D Creator/
├── Core/                          ← ValveProject, transforms, validation (tách NuGet)
│   ├── Model/
│   ├── Math/
│   └── Validation/
├── Geometry/                      ← primitives.py + scene_builder + boolean graph
│   ├── Primitives/
│   ├── SceneBuilder/
│   └── Transforms/
├── Ports/                         ← Port engine, end types, visual
├── Catalog/                       ← Part library, metadata, standard sets
│   ├── Parts/                     ← catalog_generator/parts
│   ├── Tables/                    ← pipe_sizes SSOT + generated C# mirrors
│   └── Metadata/                  ← ScriptGroup, variants, standard_sets
├── Python/                        ← Generators, deploy merge, live preview
│   ├── Generator/
│   ├── Deploy/
│   └── Runtime/                   ← p3d_composer/
├── Publish/                       ← Excel, PCAT (future), PSPX (future)
│   ├── Excel/
│   └── Pcat/                      ← (planned)
├── ISO/                           ← ISOGEN metadata, PDMS-style (future)
├── UI/
│   ├── Composer/                  ← AutoCAD palette + ribbon
│   └── Skeleton/                  ← Inventor add-in
└── Scripts/                       ← install, deploy, audit, sync
```

**Lợi ích**: Core tách package → Composer và SkeletonManager cùng version; Tables có SSOT rõ; Publish mở rộng PCAT/PSPX mà không chạm Geometry.

---

## 10. Tham chiếu nhanh

| Mục | Giá trị |
|-----|---------|
| Plugin command | `P3DCOMPOSER` |
| Build | `dotnet build Plant3DCatalogComposer\Plant3DCatalogComposer.csproj -c Release` |
| Install | `.\scripts\Install-Plant3DCatalogComposer.ps1` |
| Dev config | `%AppData%\Plant3DCatalogComposer\deploy.json` |
| Scene JSON | `%AppData%\Plant3DCatalogComposer\scenes\{dwg}.scene.json` |
| Runtime scripts | `C:\AutoCAD Plant 3D 2026 Content\CPak Common\CustomScripts` |
| Plugin bundle | `%AppData%\Autodesk\ApplicationPlugins\Plant3DCatalogComposer.bundle` |

---

## 11. Kết quả review tài liệu (2026-06-21)

### Đã bổ sung (trước đó thiếu)

- Catalog Setup tab: insert library, metadata, valve skeleton, `FittingDimensionService`
- Generate Code (tách khỏi Deploy), Scene JSON Import/Export, `DocumentStore` mirror flags
- Excel export **library-based** (không phải scene-based)
- `CatalogGroupResolver`, `StandardCatalogGuard`, `CatalogPortTemplates`, `CatalogFlangeBoltingCatalog`
- Shared Python modules + `p3d_composer` runtime + metadata files (`variants.map`)
- Build/install chain, ApplicationPlugins bundle, `Wrapper.lsp`
- Boolean thực thi trong `scene_builder.py` (Composer path)
- Phụ thuộc: ClosedXML, deploy.json, Application.Idle, PackageContents command gap

### Đã sửa (trước đó sai / mơ hồ)

- Boolean ops: **không** phải metadata-only trên toàn hệ thống — chỉ đúng với Inventor add-in
- Excel export không serialize scene; `ValveProject?` trong export là dead parameter
- Kiến trúc: Deploy và Excel là nhánh độc lập
- Part list: liệt kê đầy đủ 20 ID (không rút gọn `/`), phân biệt 19 `part.json`
- `ValveProject` / `ConnectionPort` thiếu thuộc tính

### Không ghi nhận (không tồn tại trên disk)

- `TESTVALVE_FL_CL150` — có trong git index cũ / backup snapshot, **không** có folder trên disk hiện tại
- `ProjectBackupService.cs` — đã xóa khỏi repo
- `CatalogLjRingPlantContentTable.cs` — đã xóa (thay bằng `CatalogLjRingCl150Table.cs`)

### Không suy đoán

- Mục 7 (lộ trình) và mục 9 (cấu trúc lâu dài) giữ nguyên — là yêu cầu/đề xuất, không phải code hiện có
- PDMS-style ISOGEN vẫn đánh dấu **Giả định (Assumption)**

---

*Tài liệu này phản ánh trạng thái source code tại thời điểm phân tích. Khi thêm feature, cập nhật mục 3 (hoàn thành) và mục 6 (debt) tương ứng.*

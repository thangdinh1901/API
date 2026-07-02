using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal static class CatalogExcelTemplateService
    {
        private const int MaxSheetNameLength = 31;

        private static readonly object TemplateCacheLock = new();
        private static string? _cachedTemplatePath;
        private static DateTime _cachedTemplateWriteUtc;
        private static IReadOnlyList<string>? _cachedTemplatePartIds;

        public static string ResolveTemplatePath() => CatalogExcelExportService.ResolveTemplatePath();

        /// <summary>Preload sheet ids (background-friendly) so the palette opens faster.</summary>
        public static void WarmTemplateCache()
        {
            try
            {
                _ = ListTemplatePartIds();
            }
            catch
            {
                // optional warm-up
            }
        }

        public static void InvalidateTemplateCache()
        {
            lock (TemplateCacheLock)
            {
                _cachedTemplatePartIds = null;
                _cachedTemplatePath = null;
            }
        }

        public static IReadOnlyList<string> ListTemplatePartIds()
        {
            string path = ResolveTemplatePath();
            if (!File.Exists(path))
                return Array.Empty<string>();

            DateTime writeUtc = File.GetLastWriteTimeUtc(path);
            lock (TemplateCacheLock)
            {
                if (_cachedTemplatePartIds != null
                    && _cachedTemplatePath != null
                    && _cachedTemplatePath.Equals(path, StringComparison.OrdinalIgnoreCase)
                    && _cachedTemplateWriteUtc == writeUtc)
                {
                    return _cachedTemplatePartIds;
                }
            }

            using var workbook = new XLWorkbook(path);
            var ids = workbook.Worksheets
                .Select(w => DeriveSheetPartId(w.Name))
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            lock (TemplateCacheLock)
            {
                _cachedTemplatePath = path;
                _cachedTemplateWriteUtc = writeUtc;
                _cachedTemplatePartIds = ids;
            }

            return ids;
        }

        public static string? InferCloneSourcePartId(
            string partId,
            string pnpClassName,
            string standardSet,
            string group)
            => InferCloneSourcePartId(partId, null, pnpClassName, standardSet, group, null, null, null);

        public static string InferCloneSourcePartId(
            string partId,
            string? catalogCategory,
            string pnpClassName,
            string standardSet,
            string group,
            string? primaryEndType,
            string? pressureClass = null,
            string? pipeSchedule = null)
            => CatalogExcelCloneSuggestService.InferCloneSourcePartId(
                catalogCategory,
                pnpClassName,
                primaryEndType,
                pressureClass,
                pipeSchedule,
                standardSet,
                group);

        internal static string InferLegacyCloneSourcePartId(
            string partId,
            string pnpClassName,
            string standardSet,
            string group,
            string? primaryEndType = null,
            string? pressureClass = null,
            string? pipeSchedule = null)
        {
            string pnp = pnpClassName.Trim();
            string set = standardSet.Trim();
            string end = Plant3DEndTypes.NormalizeCode(primaryEndType);

            if (set.Equals(BwSch40StandardCatalog.SetId, StringComparison.OrdinalIgnoreCase)
                || IsScheduleEnd(end))
            {
                return pnp switch
                {
                    "Elbow" => "ELBOW_90_LR_BW_SCH40",
                    "Tee" => "TEE_EQ_BW_SCH40",
                    "Reducer" => "REDUCER_CONC_BW_SCH40",
                    "StubEnd" => "STUBEND_LJ_A_BW_SCH40",
                    _ => "ELBOW_90_LR_BW_SCH40",
                };
            }

            if (set.Equals(SwCl3000StandardCatalog.SetId, StringComparison.OrdinalIgnoreCase)
                || IsSwEnd(end))
            {
                return pnp switch
                {
                    "Elbow" => "ELBOW_90_SW_CL3000",
                    "Tee" => "TEE_EQ_SW_CL3000",
                    _ => "ELBOW_90_SW_CL3000",
                };
            }

            if (group.Equals("Flange", StringComparison.OrdinalIgnoreCase))
            {
                if (pnp.Equals("BlindFlange", StringComparison.OrdinalIgnoreCase))
                    return "BLD_FLRF_CL150";
                if (end.Equals("LAP", StringComparison.OrdinalIgnoreCase))
                    return "LJ_RING_CL150_RF";
                if (end.Equals("SO", StringComparison.OrdinalIgnoreCase))
                    return "SO_FLRF_CL150";
                return "WN_FLRF_CL150";
            }

            if (group.Equals("Gasket", StringComparison.OrdinalIgnoreCase))
            {
                return end.Equals("FF", StringComparison.OrdinalIgnoreCase)
                    ? "GSK_FF_CL150"
                    : "GSK_RF_CL150";
            }

            if (group.Equals("Valve", StringComparison.OrdinalIgnoreCase))
            {
                IReadOnlyList<string> valves = CatalogExcelCloneSuggestService.ListForPartFamily(
                    CatalogCategories.Valves,
                    pnp,
                    end,
                    pressureClass,
                    pipeSchedule);
                if (valves.Count > 0)
                    return valves[0];
                return CatalogValveExcelTemplates.FlSimple;
            }

            return "ELBOW_90_LR_BW_SCH40";
        }

        private static bool IsScheduleEnd(string end) =>
            end.Equals("BV", StringComparison.OrdinalIgnoreCase)
            || end.Equals("PL", StringComparison.OrdinalIgnoreCase)
            || end.Equals("PPL", StringComparison.OrdinalIgnoreCase)
            || end.Equals("BW", StringComparison.OrdinalIgnoreCase);

        private static bool IsSwEnd(string end) =>
            end.Equals("SW", StringComparison.OrdinalIgnoreCase)
            || end.Equals("PSW", StringComparison.OrdinalIgnoreCase);

        public static IReadOnlyList<string> ListForPartFamily(
            string? catalogCategory,
            string? pipingComponent,
            string? primaryEndType,
            string? pressureClass,
            string? pipeSchedule = null)
            => CatalogExcelCloneSuggestService.ListForPartFamily(
                catalogCategory,
                pipingComponent,
                primaryEndType,
                pressureClass,
                pipeSchedule);

        public static bool EnsurePartSheet(
            string partId,
            string cloneSourcePartId,
            out string? sheetName,
            out string? error)
        {
            if (TryEnsurePartSheetOnce(partId, cloneSourcePartId, out sheetName, out error))
                return true;

            // A user-configured template (e.g. CATA_NUI.xlsx) is authoritative — never run the
            // bundled-template auto-repair against it.
            if (CatalogTemplateSettings.ResolveConfiguredTemplatePath() != null)
                return false;

            if (!TryRefreshTemplateWorkbook(out string? refreshError))
            {
                if (!string.IsNullOrWhiteSpace(refreshError))
                    error += $"\n\nAuto-repair failed: {refreshError}";
                return false;
            }

            return TryEnsurePartSheetOnce(partId, cloneSourcePartId, out sheetName, out error);
        }

        private static bool TryEnsurePartSheetOnce(
            string partId,
            string cloneSourcePartId,
            out string? sheetName,
            out string? error)
        {
            sheetName = null;
            error = null;
            partId = partId.Trim();
            cloneSourcePartId = cloneSourcePartId.Trim();

            if (string.IsNullOrEmpty(partId))
            {
                error = "Part id is required.";
                return false;
            }

            if (string.IsNullOrEmpty(cloneSourcePartId))
            {
                error = "Select an Excel clone source part.";
                return false;
            }

            string templatePath = ResolveTemplatePath();

            // A user-configured catalog (e.g. CATA_NUI.xlsx) is the user's source of truth and must
            // never be written to. Validate the clone source and compute the sheet name in-memory;
            // the actual sheet is cloned on demand into the export workbook (Export()), not persisted.
            bool readOnlyTemplate = CatalogTemplateSettings.ResolveConfiguredTemplatePath() != null;

            using var workbook = new XLWorkbook(templatePath);

            IXLWorksheet? existing = FindSheetByPartPrefix(workbook, partId);
            if (existing != null)
            {
                sheetName = existing.Name;
                return true;
            }

            IXLWorksheet? source = FindSheetByPartPrefix(workbook, cloneSourcePartId);
            if (source == null && TryImportCloneSourceSheet(workbook, cloneSourcePartId, templatePath))
            {
                if (!readOnlyTemplate)
                    SaveTemplateWorkbook(workbook, templatePath);
                source = FindSheetByPartPrefix(workbook, cloneSourcePartId);
            }

            if (source == null)
            {
                error =
                    $"No template sheet for clone source '{cloneSourcePartId}' in:\n{templatePath}\n" +
                    "Rebuild/redeploy the plugin so Resources/CatalogBuilderTemplate.xlsx includes valve sheets, " +
                    "or set deploy.json CatalogGenerator to your API repo.";
                return false;
            }

            string newName = BuildSheetName(partId, source.Name, cloneSourcePartId);
            if (workbook.Worksheets.Any(w => w.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                sheetName = newName;
                return true;
            }

            // Read-only configured template: return the computed name without modifying the file.
            if (readOnlyTemplate)
            {
                sheetName = newName;
                return true;
            }

            source.CopyTo(newName);
            IXLWorksheet cloned = workbook.Worksheet(newName);
            ReplacePartTokens(cloned, cloneSourcePartId, partId);
            SaveTemplateWorkbook(workbook, templatePath);
            sheetName = newName;
            return true;
        }

        /// <summary>
        /// Restore valve/pipe sheets via scripts/add_static_support_template_sheets.py when the
        /// workbook was truncated (common after a mistaken single-part publish over Resources/).
        /// </summary>
        private static bool TryRefreshTemplateWorkbook(out string? error)
        {
            error = null;
            string? apiRoot = ProjectPaths.TryResolveApiRoot();
            if (apiRoot == null)
            {
                error = "deploy.json CatalogGenerator not set — cannot auto-repair template.";
                return false;
            }

            string scriptPath = Path.Combine(apiRoot, "scripts", "add_static_support_template_sheets.py");
            if (!File.Exists(scriptPath))
            {
                error = $"Repair script not found: {scriptPath}";
                return false;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\"",
                    WorkingDirectory = apiRoot,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                using Process? proc = Process.Start(psi);
                if (proc == null)
                {
                    error = "Could not start python.";
                    return false;
                }

                if (!proc.WaitForExit(120_000))
                {
                    try { proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
                    error = "Template repair timed out.";
                    return false;
                }

                string stderr = proc.StandardError.ReadToEnd();
                if (proc.ExitCode != 0)
                {
                    error = string.IsNullOrWhiteSpace(stderr)
                        ? $"Template repair exited with code {proc.ExitCode}."
                        : stderr.Trim();
                    return false;
                }

                InvalidateTemplateCache();
                SyncRepairedTemplateToPlugin(apiRoot);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static void SyncRepairedTemplateToPlugin(string apiRoot)
        {
            string devPath = Path.Combine(
                apiRoot,
                "Plant3DCatalogComposer",
                "Resources",
                "CatalogBuilderTemplate.xlsx");
            if (!File.Exists(devPath))
                return;

            string pluginPath = CatalogExcelExportService.PluginTemplatePath();
            if (pluginPath.Equals(devPath, StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(pluginPath)!);
                File.Copy(devPath, pluginPath, overwrite: true);
            }
            catch
            {
                // optional — dev template is enough when deploy.json points at API repo
            }
        }

        /// <summary>
        /// Clone a part sheet inside an export workbook (does not persist to CatalogBuilderTemplate.xlsx).
        /// </summary>
        public static IXLWorksheet? TryClonePartSheetInWorkbook(
            XLWorkbook workbook,
            string partId,
            string cloneSourcePartId)
        {
            partId = partId.Trim();
            cloneSourcePartId = cloneSourcePartId.Trim();
            if (string.IsNullOrEmpty(partId) || string.IsNullOrEmpty(cloneSourcePartId))
                return null;

            IXLWorksheet? existing = FindSheetByPartPrefix(workbook, partId);
            if (existing != null)
                return existing;

            IXLWorksheet? source = FindSheetByPartPrefix(workbook, cloneSourcePartId);
            if (source == null)
                return null;

            string newName = BuildSheetName(partId, source.Name, cloneSourcePartId);
            if (workbook.Worksheets.Any(w => w.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                return workbook.Worksheet(newName);

            source.CopyTo(newName);
            IXLWorksheet cloned = workbook.Worksheet(newName);
            ReplacePartTokens(cloned, cloneSourcePartId, partId);
            return cloned;
        }

        private static void SaveTemplateWorkbook(XLWorkbook workbook, string templatePath)
        {
            // Hard guard: never overwrite a user-configured read-only catalog template.
            string? configured = CatalogTemplateSettings.ResolveConfiguredTemplatePath();
            if (configured != null
                && Path.GetFullPath(configured).Equals(Path.GetFullPath(templatePath), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            workbook.SaveAs(templatePath);
            InvalidateTemplateCache();
        }

        /// <summary>Remove template sheets for parts no longer present under catalog_generator/parts.</summary>
        public static int RemoveOrphanedPartSheets(IReadOnlySet<string> activePartIds)
        {
            // Never prune a user-configured catalog (e.g. CATA_NUI.xlsx) — it is read-only and its
            // sheets are not 1:1 with catalog_generator/parts, so pruning would delete the user's data.
            if (CatalogTemplateSettings.ResolveConfiguredTemplatePath() != null)
                return 0;

            string templatePath = ResolveTemplatePath();
            using var workbook = new XLWorkbook(templatePath);
            var toRemove = new List<IXLWorksheet>();

            foreach (IXLWorksheet sheet in workbook.Worksheets)
            {
                if (!TryParseSheetPartId(sheet.Name, out string partId))
                    continue;

                if (!activePartIds.Contains(partId))
                    toRemove.Add(sheet);
            }

            if (toRemove.Count == 0)
                return 0;

            foreach (IXLWorksheet sheet in toRemove)
                sheet.Delete();

            SaveTemplateWorkbook(workbook, templatePath);
            return toRemove.Count;
        }

        /// <summary>
        /// Non-part / metadata sheets that should never be treated as a clone source or part sheet
        /// (e.g. the "Catalog Data Flag" marker sheet in a published Catalog Builder workbook).
        /// </summary>
        private static bool IsMetaSheet(string sheetName)
        {
            string n = sheetName.Trim();
            if (string.IsNullOrEmpty(n))
                return true;

            return n.Equals("Catalog Data Flag", StringComparison.OrdinalIgnoreCase)
                || n.Equals("Catalog Data", StringComparison.OrdinalIgnoreCase)
                || n.Equals("Settings", StringComparison.OrdinalIgnoreCase)
                || n.StartsWith("Sheet", StringComparison.OrdinalIgnoreCase) && n.Length <= 7;
        }

        /// <summary>
        /// Derive a clone-source part id from a sheet name. Supports both the legacy comma
        /// convention ("{PartId},{End},{Class}") and plain sheet names used by real published
        /// catalogs (e.g. "ELBOW 90_BV_SCH40", "GSK_RF_CL150"). Returns null for meta sheets.
        /// </summary>
        internal static string? DeriveSheetPartId(string sheetName)
        {
            if (IsMetaSheet(sheetName))
                return null;

            int comma = sheetName.IndexOf(',');
            string id = comma > 0 ? sheetName[..comma].Trim() : sheetName.Trim();
            return string.IsNullOrEmpty(id) ? null : id;
        }

        private static bool TryParseSheetPartId(string sheetName, out string partId)
        {
            partId = DeriveSheetPartId(sheetName) ?? "";
            return !string.IsNullOrEmpty(partId);
        }

        internal static IXLWorksheet? FindSheetByPartPrefix(XLWorkbook workbook, string partId)
        {
            string trimmed = partId.Trim();
            string prefix = trimmed + ",";

            // Comma convention: "{PartId},..." — and exact match for plain catalog sheet names.
            return workbook.Worksheets.FirstOrDefault(w =>
                       w.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                   ?? workbook.Worksheets.FirstOrDefault(w =>
                       w.Name.Trim().Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Same match as <see cref="FindSheetByPartPrefix"/>, plus a normalised-key fallback
        /// (letters/digits only, case-insensitive) for native library ids whose underscores don't
        /// line up with the template's space-separated sheet names, e.g. id "ELBOW_90_SCH40_BW" vs.
        /// sheet "ELBOW 90_SCH40_BW".</summary>
        internal static IXLWorksheet? FindSheetByPartPrefixOrNormalizedKey(XLWorkbook workbook, string partId)
        {
            IXLWorksheet? exact = FindSheetByPartPrefix(workbook, partId);
            if (exact != null)
                return exact;

            string target = NormalizeSheetKey(partId);
            if (target.Length == 0)
                return null;

            return workbook.Worksheets.FirstOrDefault(w =>
                NormalizeSheetKey(SheetNameKey(w.Name)).Equals(target, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Sheet name up to the first comma (drops the legacy ",FL,150" style suffix).</summary>
        private static string SheetNameKey(string sheetName)
        {
            int comma = sheetName.IndexOf(',');
            return comma >= 0 ? sheetName[..comma] : sheetName;
        }

        /// <summary>Uppercase, alphanumerics only — folds spaces/underscores so ids and sheet names match.</summary>
        private static string NormalizeSheetKey(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToUpperInvariant(c));
            }

            return sb.ToString();
        }

        private static bool TryImportCloneSourceSheet(
            XLWorkbook targetWorkbook,
            string cloneSourcePartId,
            string activeTemplatePath)
        {
            foreach (string path in EnumerateTemplateSearchPaths(activeTemplatePath))
            {
                using var altWorkbook = new XLWorkbook(path);
                IXLWorksheet? source = FindSheetByPartPrefix(altWorkbook, cloneSourcePartId);
                if (source == null)
                    continue;

                string importName = source.Name;
                if (FindSheetByPartPrefix(targetWorkbook, cloneSourcePartId) != null)
                    return true;

                if (targetWorkbook.Worksheets.Any(w =>
                        w.Name.Equals(importName, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                source.CopyTo(targetWorkbook, importName);
                return true;
            }

            return false;
        }

        private static IEnumerable<string> EnumerateTemplateSearchPaths(string activeTemplatePath)
        {
            var paths = new List<string>();
            void Add(string? path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    return;
                if (!File.Exists(path))
                    return;
                if (paths.Any(p => p.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    return;
                paths.Add(path);
            }

            Add(CatalogExcelExportService.DevTemplatePath());
            Add(CatalogExcelExportService.PluginTemplatePath());
            Add(activeTemplatePath);
            return paths;
        }

        private static string BuildSheetName(string partId, string sourceSheetName, string cloneSourcePartId)
        {
            string suffix = sourceSheetName.Length > cloneSourcePartId.Length
                ? sourceSheetName.Substring(cloneSourcePartId.Length)
                : ",FL,40";

            string candidate = partId + suffix;
            if (candidate.Length <= MaxSheetNameLength)
                return candidate;

            int keepSuffix = Math.Max(0, MaxSheetNameLength - partId.Length);
            if (keepSuffix == 0)
                return partId.Length <= MaxSheetNameLength ? partId : partId[..MaxSheetNameLength];

            return partId + suffix[..Math.Min(suffix.Length, keepSuffix)];
        }

        private static void ReplacePartTokens(IXLWorksheet sheet, string oldId, string newId)
        {
            string oldCust = "CUST_" + oldId;
            string newCust = "CUST_" + newId;

            foreach (IXLCell cell in sheet.CellsUsed())
            {
                if (cell.Value.IsText)
                {
                    string text = cell.GetString();
                    if (text.Contains(oldId, StringComparison.Ordinal) ||
                        text.Contains(oldCust, StringComparison.Ordinal))
                    {
                        cell.Value = text
                            .Replace(oldCust, newCust, StringComparison.Ordinal)
                            .Replace(oldId, newId, StringComparison.Ordinal);
                    }
                }
            }
        }
    }
}

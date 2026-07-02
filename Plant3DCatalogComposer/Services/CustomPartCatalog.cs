using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    public static class CustomPartCatalog
    {
        private static IReadOnlyList<CustomPartDefinition>? _cache;
        private static IReadOnlyList<CustomPartDefinition>? _customCache;

        public static IReadOnlyList<CustomPartDefinition> Parts => _cache ??= DiscoverActive();

        /// <summary>User-authored composite parts under parts/CUSTOM/ (NOT shown in the insert list).</summary>
        public static IReadOnlyList<CustomPartDefinition> CustomParts => _customCache ??= DiscoverCustom();

        public static void Reload()
        {
            _cache = null;
            _customCache = null;
        }

        /// <summary>Flanges, gaskets, pre-built catalog valves — insert directly into the scene.</summary>
        public static IReadOnlyList<CustomPartDefinition> InsertableParts =>
            Parts.Where(p => p.IsStandardCatalog).ToList();

        /// <summary>All parts that Publish/Deploy should export: native library + user CUSTOM parts.</summary>
        public static IReadOnlyList<CustomPartDefinition> ExportableParts =>
            Parts.Where(p => p.IsStandardCatalog).Concat(CustomParts).ToList();

        /// <summary>Valve dimension templates for Setup / Create Skeleton.</summary>
        public static IReadOnlyList<CustomPartDefinition> CompositeTemplates =>
            Parts.Where(p => p.IsCompositeTemplate).ToList();

        public static CustomPartDefinition? FindById(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return Parts.Concat(CustomParts)
                .FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<CustomPartDefinition> DiscoverActive() =>
            DiscoverFromDirectories(CatalogPartsDiscovery.EnumerateActivePartDirectories);

        private static IReadOnlyList<CustomPartDefinition> DiscoverCustom() =>
            DiscoverFromDirectories(CatalogPartsDiscovery.EnumerateCustomPartDirectories);

        private static IReadOnlyList<CustomPartDefinition> DiscoverFromDirectories(
            Func<string, IEnumerable<string>> enumerate)
        {
            string partsDir = ProjectPaths.ResolvePartsDir();
            if (!Directory.Exists(partsDir))
                return Array.Empty<CustomPartDefinition>();

            var list = new List<CustomPartDefinition>();
            foreach (string dir in enumerate(partsDir))
            {
                if (!File.Exists(Path.Combine(dir, "catalog_entry.py")))
                    continue;

                if (TryLoadPartFromDirectory(dir) is CustomPartDefinition def)
                    list.Add(def);
            }

            return list
                .OrderBy(p => p.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static CustomPartDefinition? TryLoadPartFromDirectory(string dir)
        {
            string id = Path.GetFileName(dir);
            string metaPath = Path.Combine(dir, "part.json");
            if (!File.Exists(metaPath))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(metaPath));
                JsonElement root = doc.RootElement;
                string role = root.TryGetProperty("role", out JsonElement roleEl)
                    ? roleEl.GetString() ?? "standard"
                    : "standard";
                string displayName = root.GetProperty("displayName").GetString() ?? id;

                string group = root.GetProperty("group").GetString() ?? "Part";
                string category = root.TryGetProperty("category", out JsonElement catEl)
                    ? CatalogCategories.NormalizeCategoryId(catEl.GetString())
                    : ResolveCategoryFromGroup(group);
                double defaultDn = root.TryGetProperty("defaultDN", out JsonElement dnEl) && dnEl.TryGetDouble(out double dn)
                    ? dn
                    : 100;
                string pressureClass = root.TryGetProperty("pressureClass", out JsonElement pcEl)
                    ? pcEl.GetString() ?? "150"
                    : "150";
                bool parametricDn = !root.TryGetProperty("parametricDN", out JsonElement pdEl) || pdEl.GetBoolean();
                string pipeSchedule = ResolvePipeSchedule(root, id);
                string standardSet = root.TryGetProperty("standardSet", out JsonElement ssEl)
                    ? ssEl.GetString() ?? ""
                    : InferStandardSetFromId(id);
                string shortDescription = root.TryGetProperty("shortDescription", out JsonElement sdEl)
                    ? sdEl.GetString() ?? ""
                    : "";
                string pnpClassName = root.TryGetProperty("pnpClassName", out JsonElement pnpEl)
                    ? pnpEl.GetString() ?? ""
                    : "";
                string primaryEndType = root.TryGetProperty("primaryEndType", out JsonElement petEl)
                    ? petEl.GetString() ?? ""
                    : "";
                string excelCloneSourcePartId = root.TryGetProperty("excelCloneSourcePartId", out JsonElement cloneEl)
                    ? cloneEl.GetString() ?? ""
                    : "";
                string flangeFacing = root.TryGetProperty("flangeFacing", out JsonElement ffEl)
                    ? ffEl.GetString() ?? ""
                    : "";

                Dictionary<string, double>? skeleton = null;
                if (root.TryGetProperty("skeleton", out JsonElement skEl) && skEl.ValueKind == JsonValueKind.Object)
                {
                    skeleton = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    foreach (JsonProperty prop in skEl.EnumerateObject())
                    {
                        if (prop.Value.TryGetDouble(out double v))
                            skeleton[prop.Name] = v;
                    }
                }

                var catalogParams = new List<CatalogPartParam>();
                if (root.TryGetProperty("catalogParams", out JsonElement cpEl) && cpEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in cpEl.EnumerateArray())
                    {
                        string name = item.GetProperty("name").GetString() ?? "DN";
                        string label = item.TryGetProperty("label", out JsonElement lblEl)
                            ? lblEl.GetString() ?? name
                            : name;
                        double def = item.TryGetProperty("default", out JsonElement defEl) && defEl.TryGetDouble(out double dv)
                            ? dv
                            : 0;
                        bool useSkel = item.TryGetProperty("useSkeletonDN", out JsonElement skEl2) && skEl2.GetBoolean();
                        bool useSkel2 = item.TryGetProperty("useSkeletonDN2", out JsonElement sk2El) && sk2El.GetBoolean();
                        catalogParams.Add(new CatalogPartParam
                        {
                            Name = name,
                            Label = label,
                            Default = def,
                            UseSkeletonDN = useSkel,
                            UseSkeletonDN2 = useSkel2,
                        });
                    }
                }
                else
                {
                    catalogParams.Add(new CatalogPartParam
                    {
                        Name = "DN",
                        Label = "DN",
                        UseSkeletonDN = true,
                        Default = defaultDn,
                    });
                }

                var sizes = new List<int>();
                if (root.TryGetProperty("sizes", out JsonElement szEl) && szEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in szEl.EnumerateArray())
                    {
                        if (item.TryGetInt32(out int sz))
                            sizes.Add(sz);
                    }
                }

                return new CustomPartDefinition
                {
                    Role = role,
                    Id = id,
                    DisplayName = displayName,
                    Group = group,
                    Category = category,
                    DefaultDN = defaultDn,
                    PressureClass = pressureClass,
                    PipeSchedule = pipeSchedule,
                    StandardSet = standardSet,
                    ShortDescription = shortDescription,
                    PnpClassName = pnpClassName,
                    PrimaryEndType = primaryEndType,
                    ExcelCloneSourcePartId = excelCloneSourcePartId.Trim(),
                    FlangeFacing = CatalogFlangeFacing.Normalize(
                        string.IsNullOrWhiteSpace(flangeFacing) ? "RF" : flangeFacing),
                    ParametricDN = parametricDn,
                    Sizes = sizes,
                    Skeleton = skeleton,
                    CatalogParams = catalogParams,
                    CatalogFrameRotation = ParseCatalogFrameRotation(root),
                };
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveCategoryFromGroup(string group) =>
            CatalogCategories.FromActivateGroup(group);

        private static string ResolvePipeSchedule(JsonElement root, string partId)
        {
            if (root.TryGetProperty("pipeSchedule", out JsonElement psEl))
            {
                string? explicitSchedule = psEl.GetString();
                if (!string.IsNullOrWhiteSpace(explicitSchedule))
                    return PipeScheduleCatalog.Normalize(explicitSchedule);
            }

            return InferPipeScheduleFromId(partId);
        }

        private static string InferStandardSetFromId(string partId)
        {
            if (partId.Contains("_BW_SCH40", StringComparison.OrdinalIgnoreCase))
                return BwSch40StandardCatalog.SetId;
            if (partId.Contains("_SW_CL3000", StringComparison.OrdinalIgnoreCase))
                return SwCl3000StandardCatalog.SetId;
            return "";
        }

        private static string InferPipeScheduleFromId(string partId)
        {
            string id = partId.ToUpperInvariant();
            if (id.Contains("SCH10S", StringComparison.Ordinal))
                return "10S";
            if (id.Contains("SCH10", StringComparison.Ordinal))
                return "10";
            if (id.Contains("SCH80S", StringComparison.Ordinal))
                return "80S";
            if (id.Contains("SCH80", StringComparison.Ordinal))
                return "80";
            if (id.Contains("SCH40S", StringComparison.Ordinal))
                return "40S";
            if (id.Contains("SCH40", StringComparison.Ordinal))
                return "40";
            return "";
        }

        private static double[]? ParseCatalogFrameRotation(JsonElement root)
        {
            if (!root.TryGetProperty("catalogFrameRotation", out JsonElement cfEl))
                return null;

            if (cfEl.ValueKind == JsonValueKind.Array && cfEl.GetArrayLength() >= 9)
            {
                var arr = new double[9];
                for (int i = 0; i < 9; i++)
                    arr[i] = cfEl[i].GetDouble();
                return arr;
            }

            if (cfEl.ValueKind != JsonValueKind.Object)
                return null;

            string axis = cfEl.TryGetProperty("axis", out JsonElement axEl)
                ? axEl.GetString() ?? "Y"
                : "Y";
            double degrees = cfEl.TryGetProperty("degrees", out JsonElement degEl) && degEl.TryGetDouble(out double d)
                ? d
                : 90.0;
            if (axis.Length == 0)
                return null;

            return TransformMath.FrameRotationFromAxisDegrees(axis[0], degrees);
        }
    }
}

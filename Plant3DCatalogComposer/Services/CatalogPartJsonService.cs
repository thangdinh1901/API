using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class CatalogPartJsonDocument
    {
        public required string Role { get; init; }
        public required string Id { get; init; }
        public required string DisplayName { get; init; }
        public required string Category { get; init; }
        public required string Group { get; init; }
        public required string StandardSet { get; init; }
        public required string PrimaryEndType { get; init; }
        public required string PipeSchedule { get; init; }
        public required string PressureClass { get; init; }
        public required string PnpClassName { get; init; }
        public required string ShortDescription { get; init; }
        public required string ExcelCloneSourcePartId { get; init; }
        public double DefaultDn { get; init; }
        public bool ParametricDn { get; init; } = true;
    }

    internal static class CatalogPartJsonService
    {
        public static CatalogPartJsonDocument BuildFromProject(ValveProject project)
        {
            string partId = CatalogProjectService.SanitizeCatalogName(project.ValveName);
            if (string.IsNullOrEmpty(partId))
                partId = "COMPOSER_PART";

            string category = ResolveCategory(project);
            string group = ResolveGroup(project, category);
            string primaryEndType = CatalogStandardSetInference.ResolvePrimaryEndType(project);
            string standardSet = CatalogStandardSetInference.InferStandardSet(project, primaryEndType);
            string schedule = ResolvePipeSchedule(project, standardSet);
            string pressureClass = string.IsNullOrWhiteSpace(project.Parameters.PressureClass)
                ? "150"
                : project.Parameters.PressureClass.Trim();

            string displayName = FirstNonEmpty(
                project.TooltipShort,
                project.ShortDescription,
                partId);

            string shortDesc = FirstNonEmpty(
                project.ShortDescription,
                project.TooltipShort,
                partId);

            string pnp = FirstNonEmpty(
                project.PnpClassName,
                InferPnpClass(partId));

            string cloneSource = FirstNonEmpty(
                project.ExcelCloneSourcePartId,
                CatalogExcelTemplateService.InferCloneSourcePartId(partId, pnp, standardSet, group));

            return new CatalogPartJsonDocument
            {
                Role = "standard",
                Id = partId,
                DisplayName = displayName,
                Category = category,
                Group = group,
                StandardSet = standardSet,
                PrimaryEndType = primaryEndType,
                PipeSchedule = schedule,
                PressureClass = pressureClass,
                PnpClassName = pnp,
                ShortDescription = shortDesc,
                ExcelCloneSourcePartId = cloneSource,
                DefaultDn = project.Parameters.DN > 0 ? project.Parameters.DN : 100,
                ParametricDn = true,
            };
        }

        public static string WritePartJson(string partDir, CatalogPartJsonDocument doc)
        {
            Directory.CreateDirectory(partDir);
            string path = Path.Combine(partDir, "part.json");
            var root = new Dictionary<string, object?>
            {
                ["role"] = doc.Role,
                ["id"] = doc.Id,
                ["displayName"] = doc.DisplayName,
                ["category"] = doc.Category,
                ["group"] = doc.Group,
                ["defaultDN"] = doc.DefaultDn,
                ["pressureClass"] = doc.PressureClass,
                ["parametricDN"] = doc.ParametricDn,
                ["catalogParams"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["name"] = "DN",
                        ["label"] = "DN",
                        ["useSkeletonDN"] = true,
                        ["default"] = doc.DefaultDn,
                    },
                },
            };

            if (!string.IsNullOrWhiteSpace(doc.StandardSet))
                root["standardSet"] = doc.StandardSet;
            if (!string.IsNullOrWhiteSpace(doc.PrimaryEndType)
                && !doc.PrimaryEndType.Equals("Undefined_ET", StringComparison.OrdinalIgnoreCase))
                root["primaryEndType"] = doc.PrimaryEndType;
            if (!string.IsNullOrWhiteSpace(doc.PipeSchedule))
                root["pipeSchedule"] = doc.PipeSchedule;
            if (!string.IsNullOrWhiteSpace(doc.PnpClassName))
                root["pnpClassName"] = doc.PnpClassName;
            if (!string.IsNullOrWhiteSpace(doc.ShortDescription))
                root["shortDescription"] = doc.ShortDescription;
            if (!string.IsNullOrWhiteSpace(doc.ExcelCloneSourcePartId))
                root["excelCloneSourcePartId"] = doc.ExcelCloneSourcePartId;

            string json = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return path;
        }

        public static string? TryWriteDraft(string partDir, ValveProject project)
        {
            if (string.IsNullOrWhiteSpace(partDir) || !Directory.Exists(partDir))
                return null;

            if (!File.Exists(Path.Combine(partDir, "catalog_entry.py")))
                return null;

            CatalogPartJsonDocument doc = BuildFromProject(project);
            return WritePartJson(partDir, doc);
        }

        private static string ResolveCategory(ValveProject project)
        {
            if (!string.IsNullOrWhiteSpace(project.CatalogCategory))
                return CatalogCategories.NormalizeCategoryId(project.CatalogCategory);

            return CatalogCategories.FromActivateGroup(project.CatalogGroup);
        }

        private static string ResolveGroup(ValveProject project, string category)
        {
            string pnp = project.PnpClassName?.Trim() ?? "";
            return CatalogPartFamilyOptions.ResolveActivateGroup(category, pnp);
        }

        private static string ResolvePipeSchedule(ValveProject project, string standardSet)
        {
            if (!string.IsNullOrWhiteSpace(project.Parameters.PipeSchedule))
                return PipeScheduleCatalog.Normalize(project.Parameters.PipeSchedule);

            if (standardSet.Equals(BwSch40StandardCatalog.SetId, StringComparison.OrdinalIgnoreCase))
                return BwSch40StandardCatalog.PipeSchedule;

            return "";
        }

        private static string InferPnpClass(string partId)
        {
            string id = partId.ToUpperInvariant();
            if (id.Contains("ELBOW", StringComparison.Ordinal))
                return "Elbow";
            if (id.Contains("TEE", StringComparison.Ordinal))
                return "Tee";
            if (id.Contains("REDUCER", StringComparison.Ordinal))
                return "Reducer";
            if (id.StartsWith("WN_", StringComparison.Ordinal) ||
                id.StartsWith("SO_", StringComparison.Ordinal) ||
                id.StartsWith("LJ_RING_", StringComparison.Ordinal))
                return "Flange";
            if (id.StartsWith("BLD_", StringComparison.Ordinal))
                return "BlindFlange";
            if (id.StartsWith("GSK_", StringComparison.Ordinal))
                return "Gasket";
            if (id.StartsWith("STUBEND_", StringComparison.Ordinal))
                return "StubEnd";
            if (id.StartsWith("COLLAR_", StringComparison.Ordinal))
                return "Collar";
            return "Cap";
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (string? value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return "";
        }
    }
}

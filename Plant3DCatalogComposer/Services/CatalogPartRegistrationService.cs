using System;
using System.IO;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class CatalogPartRegistrationResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? PartJsonPath { get; init; }
        public string? ExcelSheetName { get; init; }
    }

    internal static class CatalogPartRegistrationService
    {
        public static CatalogPartRegistrationResult Register(ValveProject project)
        {
            string partId = CatalogProjectService.SanitizeCatalogName(project.ValveName);
            if (string.IsNullOrEmpty(partId))
            {
                return new CatalogPartRegistrationResult
                {
                    Success = false,
                    Message = "Set a script name in Catalog Project first.",
                };
            }

            string partsRoot = ProjectPaths.ResolvePartsDir();
            // Custom composite parts live under parts/CUSTOM/<id>; native under parts/<id>.
            string? partDir = CatalogPartsDiscovery.ResolveCatalogPartDirectory(partsRoot, partId);
            if (partDir == null)
            {
                return new CatalogPartRegistrationResult
                {
                    Success = false,
                    Message = $"Generate Code first — missing catalog_generator/parts/{partId}/catalog_entry.py",
                };
            }

            CatalogPartJsonDocument doc = CatalogPartJsonService.BuildFromProject(project);
            string jsonPath = CatalogPartJsonService.WritePartJson(partDir, doc);

            string cloneSource = doc.ExcelCloneSourcePartId;
            if (!CatalogExcelTemplateService.EnsurePartSheet(
                    partId,
                    cloneSource,
                    out string? sheetName,
                    out string? sheetError))
            {
                return new CatalogPartRegistrationResult
                {
                    Success = false,
                    Message = sheetError ?? "Could not create Excel template sheet.",
                    PartJsonPath = jsonPath,
                };
            }

            CustomPartCatalog.Reload();

            return new CatalogPartRegistrationResult
            {
                Success = true,
                PartJsonPath = jsonPath,
                ExcelSheetName = sheetName,
                Message = $"Registered {partId} — part.json + Excel sheet '{sheetName}'. Publish Catalog to export sizes.",
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class CatalogPublishResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public string OutputPath { get; init; } = string.Empty;
        public int SheetCount { get; init; }
        public int SizeRowCount { get; init; }
        public IReadOnlyList<string> SkippedPartIds { get; init; } = [];
        public IReadOnlyList<string> Warnings { get; init; } = [];
        public ValidationResult Preflight { get; init; } = new();
    }

    /// <summary>Export Catalog Builder Excel workbook for .pcat generation.</summary>
    internal static class CatalogPublishService
    {
        public static CatalogPublishResult Publish(
            string dwgPath,
            ValveProject project,
            string outputPath,
            bool allowExportWithWarnings)
        {
            ValidationResult preflight = CatalogPreflightService.ValidateForExcelPublish(project);
            if (!preflight.IsValid)
            {
                return new CatalogPublishResult
                {
                    Success = false,
                    Preflight = preflight,
                    Message = "Publish blocked:\n" + string.Join("\n", preflight.Errors),
                };
            }

            if (!allowExportWithWarnings && preflight.Issues.Any(i => !i.IsError))
            {
                return new CatalogPublishResult
                {
                    Success = false,
                    Preflight = preflight,
                    Message = "Publish cancelled — resolve warnings or confirm to continue.",
                };
            }

            try
            {
                DocumentStore.Save(dwgPath, project);
                CatalogExcelExportResult export = CatalogExcelExportService.Export(outputPath, project);
                if (!export.Success)
                {
                    return new CatalogPublishResult
                    {
                        Success = false,
                        Preflight = preflight,
                        Message = export.Message,
                        SkippedPartIds = export.SkippedPartIds,
                        Warnings = export.Warnings,
                    };
                }

                var warnings = preflight.Issues
                    .Where(i => !i.IsError)
                    .Select(i => i.Message)
                    .Concat(export.Warnings)
                    .ToList();

                return new CatalogPublishResult
                {
                    Success = true,
                    Preflight = preflight,
                    OutputPath = export.OutputPath,
                    SheetCount = export.SheetCount,
                    SizeRowCount = export.SizeRowCount,
                    SkippedPartIds = export.SkippedPartIds,
                    Warnings = warnings,
                    Message = export.Message,
                };
            }
            catch (Exception ex)
            {
                return new CatalogPublishResult
                {
                    Success = false,
                    Preflight = preflight,
                    Message = ex.Message,
                };
            }
        }
    }
}

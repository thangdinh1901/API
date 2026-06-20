using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class CatalogPublishResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public int ExportedFileCount { get; init; }
        public int DeployedScriptCount { get; init; }
        public bool RegisterQueued { get; init; }
        public string PartFolder { get; init; } = string.Empty;
        public ValidationResult Preflight { get; init; } = new();
    }

    internal static class CatalogPublishService
    {
        public static CatalogPublishResult Publish(
            string dwgPath,
            ValveProject project,
            Func<string, string> resolveExportRoot,
            Document? doc,
            bool allowExportWithWarnings)
        {
            ValidationResult preflight = CatalogPreflightService.ValidateForPublish(project);
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
                CatalogPackage package = ComposerLiveScriptService.BuildCatalogPackage(project);
                string exportRoot = resolveExportRoot(dwgPath);
                IReadOnlyList<string> exported = CatalogExportService.Export(package, exportRoot);
                string catalogCode = package.ToDisplayText();
                ComposerLiveScriptService.WriteCatalogPackage(project, catalogCode);

                CatalogDeployResult deploy = CatalogDeployService.DeployToCustomScripts();
                if (!deploy.Success)
                {
                    return new CatalogPublishResult
                    {
                        Success = false,
                        Preflight = preflight,
                        ExportedFileCount = exported.Count,
                        PartFolder = Path.Combine(exportRoot, package.FolderName),
                        Message = "Generate OK, deploy failed: " + deploy.Message,
                    };
                }

                bool registered = CatalogDeployService.TryQueueRegisterCustomScripts(doc, deploy.ScriptCount);
                string partFolder = Path.Combine(exportRoot, package.FolderName);
                string summary =
                    $"Published {package.FolderName}: {exported.Count} file(s) exported, " +
                    $"{deploy.ScriptCount} script(s) deployed." +
                    (registered ? " PLANTREGISTERCUSTOMSCRIPTS queued." : " Run PLANTREGISTERCUSTOMSCRIPTS.");

                return new CatalogPublishResult
                {
                    Success = true,
                    Preflight = preflight,
                    ExportedFileCount = exported.Count,
                    DeployedScriptCount = deploy.ScriptCount,
                    RegisterQueued = registered,
                    PartFolder = partFolder,
                    Message = summary,
                };
            }
            catch (OperationCanceledException)
            {
                return new CatalogPublishResult
                {
                    Success = false,
                    Preflight = preflight,
                    Message = "Publish cancelled (export folder not chosen).",
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

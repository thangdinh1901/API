using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class CatalogDeployFullResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public int ExportedFileCount { get; init; }
        public int DeployedScriptCount { get; init; }
        public bool RegisterQueued { get; init; }
        public string PartFolder { get; init; } = string.Empty;
        public ValidationResult Preflight { get; init; } = new();
    }

    /// <summary>Generate Code + deploy to CustomScripts + PLANTREGISTERCUSTOMSCRIPTS.</summary>
    internal static class CatalogDeployFullService
    {
        public static CatalogDeployFullResult Deploy(
            string dwgPath,
            ValveProject project,
            Func<string, string> resolveExportRoot,
            Document? doc,
            bool allowExportWithWarnings)
        {
            ValidationResult preflight = CatalogPreflightService.ValidateForDeploy(project);
            if (!preflight.IsValid)
            {
                return new CatalogDeployFullResult
                {
                    Success = false,
                    Preflight = preflight,
                    Message = "Deploy blocked:\n" + string.Join("\n", preflight.Errors),
                };
            }

            if (!allowExportWithWarnings && preflight.Issues.Any(i => !i.IsError))
            {
                return new CatalogDeployFullResult
                {
                    Success = false,
                    Preflight = preflight,
                    Message = "Deploy cancelled — resolve warnings or confirm to continue.",
                };
            }

            try
            {
                DocumentStore.Save(dwgPath, project);

                int exportedFileCount = 0;
                string partFolder = string.Empty;
                string? catalogCode = null;

                if (project.Parts.Count > 0)
                {
                    if (!StandardCatalogGuard.ShouldSkipSceneExport(project))
                    {
                        CatalogPackage package = ComposerLiveScriptService.BuildCatalogPackage(project);
                        string exportRoot = resolveExportRoot(dwgPath);
                        IReadOnlyList<string> exported = CatalogExportService.Export(package, exportRoot);
                        exportedFileCount = exported.Count;
                        partFolder = Path.Combine(exportRoot, package.ExportFolderName);
                        catalogCode = package.ToDisplayText();
                        ComposerLiveScriptService.WriteCatalogPackage(project, catalogCode);
                    }
                }

                CatalogDeployResult deploy = CatalogDeployService.DeployToCustomScripts();
                if (!deploy.Success)
                {
                    return new CatalogDeployFullResult
                    {
                        Success = false,
                        Preflight = preflight,
                        ExportedFileCount = exportedFileCount,
                        PartFolder = partFolder,
                        Message = project.Parts.Count > 0
                            ? "Generate OK, deploy failed: " + deploy.Message
                            : "Deploy failed: " + deploy.Message,
                    };
                }

                bool registered = CatalogDeployService.TryQueueRegisterCustomScripts(doc, deploy.ScriptCount);
                string summary = project.Parts.Count > 0
                    ? $"Deployed {Path.GetFileName(partFolder)}: {exportedFileCount} file(s) exported, " +
                      $"{deploy.ScriptCount} script(s) copied." +
                      (registered ? " PLANTREGISTERCUSTOMSCRIPTS queued." : " Run PLANTREGISTERCUSTOMSCRIPTS.")
                    : deploy.Message +
                      (registered ? " PLANTREGISTERCUSTOMSCRIPTS queued." : " Run PLANTREGISTERCUSTOMSCRIPTS.");

                return new CatalogDeployFullResult
                {
                    Success = true,
                    Preflight = preflight,
                    ExportedFileCount = exportedFileCount,
                    DeployedScriptCount = deploy.ScriptCount,
                    RegisterQueued = registered,
                    PartFolder = partFolder,
                    Message = summary,
                };
            }
            catch (OperationCanceledException)
            {
                return new CatalogDeployFullResult
                {
                    Success = false,
                    Preflight = preflight,
                    Message = "Deploy cancelled (export folder not chosen).",
                };
            }
            catch (Exception ex)
            {
                return new CatalogDeployFullResult
                {
                    Success = false,
                    Preflight = preflight,
                    Message = ex.Message,
                };
            }
        }
    }
}

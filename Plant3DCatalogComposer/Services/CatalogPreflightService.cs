using System;
using System.Collections.Generic;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal static class CatalogPreflightService
    {
        /// <summary>Library deploy — errors only (no scene/port warnings).</summary>
        public static ValidationResult ValidateForDeploy(ValveProject project)
        {
            var result = new ValidationResult();

            if (!System.IO.Directory.Exists(ProjectPaths.CustomScriptsDir))
            {
                result.AddError($"CustomScripts not found: {ProjectPaths.CustomScriptsDir}");
            }

            if (project.Parts.Count > 0)
            {
                ValidationResult scene = ProjectValidator.Validate(project);
                foreach (ValidationIssue issue in scene.Issues.Where(i => i.IsError))
                    result.AddError(issue.Message);
            }

            return result;
        }

        public static ValidationResult ValidateForExcelPublish(ValveProject project)
        {
            var result = new ValidationResult();
            AddPublishWarnings(result, project);

            IReadOnlyList<CatalogExcelPartRow> exportable = CatalogExcelPartResolver.DiscoverExportParts();
            if (exportable.Count == 0)
            {
                result.AddError(
                    "No flange / gasket / fitting parts found in catalog_generator/parts for Excel export.");
            }

            if (!System.IO.Directory.Exists(ProjectPaths.CustomScriptsDir))
            {
                result.AddError($"CustomScripts not found: {ProjectPaths.CustomScriptsDir}");
            }

            return result;
        }

        private static void AddPublishWarnings(ValidationResult result, ValveProject project)
        {
            string catalogName = CatalogProjectService.SanitizeCatalogName(project.ValveName ?? "");
            if (string.IsNullOrEmpty(catalogName) ||
                catalogName.Equals("COMPOSER_PART", StringComparison.OrdinalIgnoreCase))
            {
                result.AddWarning(
                    "Catalog name is default (COMPOSER_PART). Set a unique name in Catalog Project → Apply.");
            }

            if (project.Ports.Count == 0)
            {
                result.AddWarning(
                    "No ports in Port Manager — published catalog uses library defaults or TODO placeholders.");
            }

            if (CatalogGroupResolver.WouldRemapValveToFitting(project.CatalogGroup, project.Ports))
            {
                result.AddWarning(
                    "Group Valve with BV/SW ports will export as Fitting (Plant 3D Spec Editor FL quirk).");
            }

            foreach (PrimitiveNode node in project.Parts.Where(p =>
                         p.Kind == SceneNodeKind.Catalog && !string.IsNullOrEmpty(p.CatalogPartId)))
            {
                string partId = node.CatalogPartId!;
                if (CatalogPortTemplates.TryBuildFlatCatalogImport(partId) == null &&
                    CatalogPortTemplates.TryResolveLibraryClassName(partId) == null)
                {
                    result.AddWarning(
                        $"Nested catalog '{partId}' — could not resolve library class from catalog_entry.py " +
                        $"(check deploy.json parts folder).");
                }
            }

            if (ProjectPaths.TryResolveDevPartsDir() == null)
            {
                result.AddWarning(
                    "deploy.json not configured — Generate will ask for an export folder each time.");
            }
        }
    }
}

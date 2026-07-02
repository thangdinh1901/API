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

        public static ValidationResult ValidateForGenerate(ValveProject project)
        {
            var result = new ValidationResult();
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
            else
            {
                string partId = CatalogProjectService.SanitizeCatalogName(project.ValveName ?? "");
                if (!string.IsNullOrWhiteSpace(partId)
                    && !partId.Equals("COMPOSER_PART", StringComparison.OrdinalIgnoreCase))
                {
                    string script = System.IO.Path.Combine(
                        ProjectPaths.CustomScriptsDir,
                        $"CUST_{partId}.py");
                    if (!System.IO.File.Exists(script))
                    {
                        result.AddWarning(
                            $"CUST_{partId}.py is not in CustomScripts — run Deploy Catalog before Build Catalog "
                            + "or Catalog Builder will report Invalid Custom Script path.");
                    }
                }
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

            if (project.CatalogGroup.Equals("Valve", StringComparison.OrdinalIgnoreCase)
                || CatalogCategories.NormalizeCategoryId(project.CatalogCategory)
                    .Equals(CatalogCategories.Valves, StringComparison.OrdinalIgnoreCase))
            {
                string cgp = CatalogExcelGeometryParams.ResolveParamDefinition(project);
                if (CatalogExcelGeometryParams.ParseParamNames(cgp)
                        .Any(n => n.Equals("L", StringComparison.OrdinalIgnoreCase))
                    && project.Parameters.FaceToFace <= 0)
                {
                    result.AddWarning(
                        "Face-to-face (L) not set — export will use placeholder L per DN. "
                        + "Set Dimensions → FaceToFace before Publish for accurate catalog.");
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

using System;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal static class CatalogProjectService
    {
        public static void Apply(
            ValveProject project,
            string catalogName,
            double dn,
            double dn2,
            string pressureClass,
            string pipeSchedule,
            string tooltipShort,
            string tooltipLong,
            string catalogCategory,
            string pipingComponent,
            string primaryEndType,
            string shortDescription,
            string excelCloneSourcePartId,
            string flangeFacing,
            bool refreshDesignDimensions = true)
        {
            project.ValveName = SanitizeCatalogName(catalogName);
            project.TooltipShort = tooltipShort.Trim();
            project.TooltipLong = tooltipLong.Trim();
            project.CatalogCategory = CatalogCategories.NormalizeCategoryId(catalogCategory);
            project.PnpClassName = pipingComponent.Trim();
            project.PrimaryEndType = Plant3DEndTypes.NormalizeCode(primaryEndType);
            project.CatalogGroup = CatalogPartFamilyOptions.ResolveActivateGroup(
                project.CatalogCategory,
                project.PnpClassName);
            project.StandardSet = CatalogStandardSetInference.InferStandardSet(project, project.PrimaryEndType);
            project.ShortDescription = shortDescription.Trim();
            project.ExcelCloneSourcePartId = excelCloneSourcePartId.Trim();
            project.FlangeFacing = CatalogFlangeFacing.Normalize(flangeFacing);
            project.Parameters.DN = dn;
            if (CatalogPartFamilyOptions.UsesDnSmall(catalogCategory, pipingComponent))
            {
                project.Parameters.DN2 = BwFittingSizeCatalog.NormalizeReducerSmallDn(
                    (int)Math.Round(dn),
                    dn2 > 0 ? (int)Math.Round(dn2) : BwFittingSizeCatalog.DefaultReducerSmallDn((int)Math.Round(dn)));
            }
            else
            {
                project.Parameters.DN2 = 0;
            }
            project.Parameters.PressureClass = pressureClass.Trim();
            project.Parameters.PipeSchedule = string.IsNullOrWhiteSpace(pipeSchedule)
                ? ""
                : PipeScheduleCatalog.Normalize(pipeSchedule);

            if (!refreshDesignDimensions)
                return;

            RefreshDesignDimensions(project);
        }

        /// <summary>Seed from DN / Excel clone only when the user has not set design dimensions yet.</summary>
        public static void SeedDesignDimensionsIfEmpty(ValveProject project)
        {
            if (HasUserDesignDimensions(project))
                return;

            RefreshDesignDimensions(project);
        }

        public static void RefreshDesignDimensions(ValveProject project)
        {
            FittingDimensionService.SyncProjectDimensions(project);
            CatalogDimensionSuggestService.ApplySuggestions(project);
        }

        /// <summary>Pick / Apply / Scene bindings or saved dimension rows — do not overwrite on Generate.</summary>
        public static bool HasUserDesignDimensions(ValveProject project)
        {
            if (project.DimensionBindings.Count > 0)
                return true;

            return ProjectDimensionService.LoadRows(project.Parameters).Count > 0;
        }

        public static bool HasDimensionBinding(ValveProject project, string name) =>
            project.DimensionBindings.ContainsKey(name);

        public static string PreviewScriptName(ValveProject project)
        {
            if (project.Parts.Count == 1 &&
                project.Parts[0].Kind == SceneNodeKind.Catalog &&
                !string.IsNullOrWhiteSpace(project.Parts[0].CatalogPartId))
            {
                return $"CUST_{project.Parts[0].CatalogPartId}";
            }

            string baseName = string.IsNullOrWhiteSpace(project.ValveName)
                ? "COMPOSER_PART"
                : SanitizeCatalogName(project.ValveName);
            return $"CUST_{baseName}";
        }

        public static string SanitizeCatalogName(string name)
        {
            var chars = name.ToUpperInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                .ToArray();
            string n = new string(chars).Trim('_');
            return string.IsNullOrEmpty(n) ? "COMPOSER_PART" : n;
        }
    }
}

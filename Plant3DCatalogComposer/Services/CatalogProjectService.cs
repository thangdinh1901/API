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
            string excelCloneSourcePartId)
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
            FittingDimensionService.SyncProjectDimensions(project);
        }

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

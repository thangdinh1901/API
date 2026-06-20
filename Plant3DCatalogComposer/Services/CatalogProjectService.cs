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
            string plantGroup,
            double dn,
            double dn2,
            string pressureClass,
            string pipeSchedule,
            string tooltipShort,
            string tooltipLong)
        {
            project.ValveName = SanitizeCatalogName(catalogName);
            project.CatalogGroup = string.IsNullOrWhiteSpace(plantGroup) ? "Custom" : plantGroup.Trim();
            project.TooltipShort = tooltipShort.Trim();
            project.TooltipLong = tooltipLong.Trim();
            project.Parameters.DN = dn;
            project.Parameters.DN2 = BwFittingSizeCatalog.NormalizeReducerSmallDn(
                (int)Math.Round(dn),
                dn2 > 0 ? (int)Math.Round(dn2) : BwFittingSizeCatalog.DefaultReducerSmallDn((int)Math.Round(dn)));
            project.Parameters.PressureClass = pressureClass.Trim();
            project.Parameters.PipeSchedule = PipeScheduleCatalog.Normalize(pipeSchedule);
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

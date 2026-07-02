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
            // Envelope dimensions (FaceToFace, BodyOD, ElbowCenterToFace, …) are now declared by the
            // user with the +/- buttons and Pick, not auto-seeded from the Excel clone. Only the
            // per-node native dimension rows (ELBO_001.R, FLAN_001.L, …) are seeded automatically.
            CatalogNativeDimensionSeedService.Sync(project);
        }

        /// <summary>One-time migration for scenes saved while envelope dimensions (FaceToFace,
        /// BodyOD, ElbowCenterToFace, BodyLength, BonnetHeight, StemDia, HandwheelOD, plus L/CEL/T)
        /// were still auto-seeded from the Excel clone / port sync. That path also stamped a
        /// "manual" DimensionBinding on each row, so we can't tell an auto-seeded row from a
        /// hand-declared one by binding alone — hence a one-shot prune gated by
        /// <see cref="ValveProject.LegacyEnvelopeDimsPruned"/>. After it runs once, anything the
        /// user adds with + is preserved. Returns true if the project changed (caller should save).</summary>
        public static bool PruneStaleAutoSuggestedDimensions(ValveProject project)
        {
            if (project.LegacyEnvelopeDimsPruned)
                return false;

            SkeletonParameters p = project.Parameters;

            void Clear(string name, Action clear)
            {
                if (ProjectDimensionService.GetValue(project, name) > 0)
                    clear();

                project.DimensionBindings.Remove(name);
            }

            Clear("FaceToFace", () => p.FaceToFace = 0);
            Clear("BodyOD", () => p.BodyOD = 0);
            Clear("ElbowCenterToFace", () => p.ElbowCenterToFace = 0);
            Clear("BodyLength", () => p.BodyLength = 0);
            Clear("BonnetHeight", () => p.BonnetHeight = 0);
            Clear("StemDia", () => p.StemDia = 0);
            Clear("HandwheelOD", () => p.HandwheelOD = 0);

            foreach (string name in new[] { "L", "CEL", "T" })
            {
                p.CustomDimensions.Remove(name);
                project.DimensionBindings.Remove(name);
            }

            project.LegacyEnvelopeDimsPruned = true;
            return true;
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Resolve scene expressions and sync JSON before catalog export / deploy / test.</summary>
    internal static class CatalogExportPrepareService
    {
        public static void PrepareSceneForExport(ValveProject project)
        {
            project.PruneOperations();
            SceneGraphCatalogService.EnsureUnreferencedPartsUnioned(project);
            SceneParamBindingService.SanitizeManualParameterOverrides(project);
            FittingDimensionService.NormalizeElbowDimensionBindings(project);
        }

        public static IReadOnlyList<(string Name, double Value)> CollectExportDimensionParams(ValveProject project)
        {
            var list = new List<(string, double)>();
            foreach ((string name, double value) in ProjectDimensionService.LoadRows(project.Parameters))
            {
                if (value <= 0)
                    continue;
                if (name.Equals("DN", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("DN2", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Native-seed rows (e.g. "ELBO_001.R") are per-node reference values, not part
                // parameters. Their names contain '.', which is illegal in a Python kwarg — letting
                // them reach the generated def __init__(... ) produces a SyntaxError and Plant 3D
                // falls back to a cube. They are consumed only via primitive expressions, so exclude
                // them from the exported parameter signature.
                if (CatalogNativeDimensionSeedService.IsNativeSeedName(name))
                    continue;

                list.Add((name, value));
            }

            if (FittingDimensionService.TryGetSceneBendRadiusMm(project, out double bendRadius))
            {
                int idx = list.FindIndex(p =>
                    p.Item1.Equals("BendRadius", StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                    list[idx] = ("BendRadius", bendRadius);
                else
                    list.Add(("BendRadius", bendRadius));
            }

            return list
                .OrderBy(p => p.Item1, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}

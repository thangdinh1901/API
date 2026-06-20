using System;
using System.Collections.Generic;
using System.IO;
using Inventor;
using Plant3DSkeletonManager.Core;

namespace Plant3DSkeletonManager.Adapter
{
    /// <summary>Visual feedback for boolean metadata (solids are never actually combined).</summary>
    public static class BooleanAppearanceService
    {
        public static void ApplyAll(AssemblyDocument asmDoc, ValveProject project)
        {
            ResetAll(asmDoc);

            foreach (BooleanOperation op in project.Operations)
            {
                foreach (Guid toolId in op.Tools)
                {
                    ComponentOccurrence? occ = OccurrenceLookup.FindByNodeId(asmDoc, toolId);
                    if (occ == null)
                        continue;

                    switch (op.Type)
                    {
                        case BooleanOpType.SUBTRACT:
                            ApplyToolAppearance(occ, 255, 80, 80, 0.45);
                            break;
                        case BooleanOpType.INTERSECT:
                            ApplyToolAppearance(occ, 80, 120, 255, 0.45);
                            break;
                        // UNION: keep default appearance
                    }
                }
            }

            asmDoc.Update();
        }

        public static void ResetAll(AssemblyDocument asmDoc)
        {
            foreach (ComponentOccurrence occ in asmDoc.ComponentDefinition.Occurrences)
            {
                if (OccurrenceTagger.GetNodeId(occ) == null)
                    continue;

                try
                {
                    occ.AppearanceSourceType = AppearanceSourceTypeEnum.kComponentOccurrenceAppearance;
                    occ.OverrideOpacity = 1.0;
                }
                catch
                {
                    // Appearance reset is best-effort
                }
            }
        }

        private static void ApplyToolAppearance(
            ComponentOccurrence occ, int r, int g, int b, double opacity)
        {
            try
            {
                Inventor.Application app = (Inventor.Application)occ.Application;
                Asset? asset = FindOrCreateColorAsset(app, r, g, b);
                if (asset == null)
                    return;

                occ.Appearance = asset;
                occ.AppearanceSourceType = AppearanceSourceTypeEnum.kOverrideAppearance;
                occ.OverrideOpacity = opacity;
            }
            catch
            {
                // Library may differ between Inventor versions
            }
        }

        private static Asset? FindOrCreateColorAsset(Inventor.Application app, int r, int g, int b)
        {
            AssetLibrary lib = app.ActiveAppearanceLibrary;
            string[] preferred = r > 200 ? new[] { "Red", "Red - Light" } : new[] { "Blue", "Blue - Light" };

            foreach (string name in preferred)
            {
                try
                {
                    return lib.AppearanceAssets[name];
                }
                catch
                {
                    // try next name
                }
            }

            // Fallback: first asset in library
            try
            {
                if (lib.AppearanceAssets.Count > 0)
                    return lib.AppearanceAssets[1];
            }
            catch
            {
            }

            return null;
        }
    }
}

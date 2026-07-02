using System;
using System.Collections.Generic;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// Auto-seeds one read-only Design Dimension row per (native Catalog-kind scene node,
    /// original Excel geometry column) so a declared envelope dimension can reference the
    /// native's own DN-resolved geometry in an expression, e.g.
    /// "FaceToFace - ELBO_001.R - CYL_002.OF". Rows are named "&lt;NodeName&gt;.&lt;Symbol&gt;" —
    /// the dot is deliberate: ProjectDimensionService.TryValidateRow forbids '.' in user-typed
    /// names, so any dimension name containing '.' is unambiguously an auto-seeded row.
    /// </summary>
    internal static class CatalogNativeDimensionSeedService
    {
        public static bool IsNativeSeedName(string? name) =>
            !string.IsNullOrEmpty(name) && name.Contains('.');

        /// <summary>Recompute every native-seed dimension from the current scene graph. Values are
        /// always overwritten (never user-edited); entries for deleted/renamed nodes or symbols
        /// no longer suggested are removed. Returns true if any seed key/value changed.</summary>
        public static bool Sync(ValveProject project)
        {
            var desired = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (PrimitiveNode node in project.Parts)
            {
                if (node.Kind != SceneNodeKind.Catalog
                    || string.IsNullOrEmpty(node.CatalogPartId)
                    || string.IsNullOrWhiteSpace(node.Name))
                {
                    continue;
                }

                foreach ((string symbol, double valueMm) in
                         CatalogDimensionSuggestService.SuggestForNode(project, node))
                {
                    desired[$"{node.Name}.{symbol}"] = valueMm;
                }
            }

            Dictionary<string, double> custom = project.Parameters.CustomDimensions;
            bool changed = false;

            foreach (string existingKey in custom.Keys.Where(IsNativeSeedName).ToList())
            {
                if (!desired.ContainsKey(existingKey))
                {
                    custom.Remove(existingKey);
                    changed = true;
                }
            }

            foreach (KeyValuePair<string, double> pair in desired)
            {
                if (!custom.TryGetValue(pair.Key, out double current)
                    || Math.Abs(current - pair.Value) > 1e-9)
                {
                    custom[pair.Key] = pair.Value;
                    changed = true;
                }
            }

            return changed;
        }
    }
}

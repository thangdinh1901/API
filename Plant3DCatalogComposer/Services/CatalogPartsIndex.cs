using System;
using System.Collections.Generic;
using System.IO;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Part folders under catalog_generator/parts (aligned with CustomPartCatalog discovery).</summary>
    internal static class CatalogPartsIndex
    {
        public static HashSet<string> CollectCatalogPartIds(string? partsRoot = null)
        {
            partsRoot ??= ProjectPaths.ResolvePartsDir();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(partsRoot))
                return ids;

            foreach (string partDir in CatalogPartsDiscovery.EnumerateActivePartDirectories(partsRoot))
            {
                string partId = Path.GetFileName(partDir);
                if (File.Exists(Path.Combine(partDir, "part.json")))
                    ids.Add(partId);
            }

            return ids;
        }

        /// <summary>Parts with exportable catalog_entry.py (deploy / CustomScripts sync).</summary>
        public static HashSet<string> CollectDeployablePartIds(string partsRoot)
        {
            var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(partsRoot))
                return active;

            foreach (string partDir in CatalogPartsDiscovery.EnumerateActivePartDirectories(partsRoot))
            {
                string partId = Path.GetFileName(partDir);
                if (File.Exists(Path.Combine(partDir, "catalog_entry.py")))
                    active.Add(partId);
            }

            // Composite parts under parts/CUSTOM/<name>/ deploy as CUST_<name> too — count them
            // as active so RemoveOrphanedCatalogParts never prunes freshly deployed custom parts.
            foreach (string partDir in CatalogPartsDiscovery.EnumerateCustomPartDirectories(partsRoot))
                active.Add(Path.GetFileName(partDir));

            return active;
        }
    }
}

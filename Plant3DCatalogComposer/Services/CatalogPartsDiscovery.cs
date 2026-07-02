using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// Discovers active catalog parts under catalog_generator/parts/. Support modules
    /// (STUD_BOLTS, …) live under parts/ and are copied on Deploy Catalog, not registered as spec.
    /// </summary>
    internal static class CatalogPartsDiscovery
    {
        /// <summary>Shared geometry libs — not catalog parts; still deployed via CatalogDeployService.</summary>
        public static readonly string[] SupportModuleFolderNames = { "STUD_BOLTS", "NUTS", "STRUCTURAL_PROFILES" };

        /// <summary>Container folder for user-authored composite parts (parts/CUSTOM/&lt;name&gt;/).</summary>
        public const string CustomFolderName = "CUSTOM";

        public static bool IsSupportModuleDirectory(string? directoryName) =>
            !string.IsNullOrWhiteSpace(directoryName)
            && SupportModuleFolderNames.Any(n =>
                n.Equals(directoryName, System.StringComparison.OrdinalIgnoreCase));

        public static bool IsCustomContainerDirectory(string? directoryName) =>
            !string.IsNullOrWhiteSpace(directoryName)
            && CustomFolderName.Equals(directoryName, System.StringComparison.OrdinalIgnoreCase);

        public static bool ShouldSkipPartDirectory(string? directoryName)
        {
            if (string.IsNullOrWhiteSpace(directoryName))
                return true;

            if (StandardCatalogGuard.IsSandboxDirectory(directoryName))
                return true;

            if (IsCustomContainerDirectory(directoryName))
                return true;

            return IsSupportModuleDirectory(directoryName);
        }

        /// <summary>Active part folders directly under parts/ (excludes support modules, CUSTOM and _*).</summary>
        public static IEnumerable<string> EnumerateActivePartDirectories(string partsRoot)
        {
            if (!Directory.Exists(partsRoot))
                yield break;

            foreach (string partDir in Directory.EnumerateDirectories(partsRoot))
            {
                string partId = Path.GetFileName(partDir);
                if (ShouldSkipPartDirectory(partId))
                    continue;

                yield return partDir;
            }
        }

        /// <summary>User-authored composite part folders under parts/CUSTOM/ that carry a catalog_entry.py.</summary>
        public static IEnumerable<string> EnumerateCustomPartDirectories(string partsRoot)
        {
            string customRoot = Path.Combine(partsRoot, CustomFolderName);
            if (!Directory.Exists(customRoot))
                yield break;

            foreach (string partDir in Directory.EnumerateDirectories(customRoot))
            {
                if (File.Exists(Path.Combine(partDir, "catalog_entry.py")))
                    yield return partDir;
            }
        }

        public static string? ResolveCatalogPartDirectory(string partsRoot, string partId)
        {
            if (string.IsNullOrWhiteSpace(partId))
                return null;

            string activeDir = Path.Combine(partsRoot, partId);
            if (Directory.Exists(activeDir)
                && File.Exists(Path.Combine(activeDir, "catalog_entry.py")))
                return activeDir;

            string customDir = Path.Combine(partsRoot, CustomFolderName, partId);
            if (Directory.Exists(customDir)
                && File.Exists(Path.Combine(customDir, "catalog_entry.py")))
                return customDir;

            return null;
        }
    }
}

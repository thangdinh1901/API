using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Plant3DCatalogComposer.Services;

namespace Plant3DCatalogComposer
{
    /// <summary>Plugin and Plant 3D CustomScripts paths.</summary>
    internal static class ProjectPaths
    {
        private static readonly Lazy<string> PluginDir = new(() =>
        {
            string? loc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return string.IsNullOrEmpty(loc) ? AppDomain.CurrentDomain.BaseDirectory : loc;
        });

        public static string PluginDirectory => PluginDir.Value;

        public static string CatalogGeneratorDir => Path.Combine(PluginDirectory, "catalog_generator");

        /// <summary>Custom fitting/flange libraries (one subfolder per part).</summary>
        public static string PartsDir => Path.Combine(CatalogGeneratorDir, "parts");

        /// <summary>Dev repo parts/ from deploy.json, or null if not configured.</summary>
        public static string? TryResolveDevPartsDir()
        {
            DeploySettings? settings = DeploySettings.TryLoad();
            if (string.IsNullOrWhiteSpace(settings?.CatalogGenerator))
                return null;

            if (!Directory.Exists(settings.CatalogGenerator))
                return null;

            string parts = Path.Combine(settings.CatalogGenerator, "parts");
            Directory.CreateDirectory(parts);
            return parts;
        }

        /// <summary>API repo root when deploy.json points at dev catalog_generator.</summary>
        public static string? TryResolveApiRoot()
        {
            DeploySettings? settings = DeploySettings.TryLoad();
            if (string.IsNullOrWhiteSpace(settings?.CatalogGenerator))
                return null;

            string? genDir = Path.GetDirectoryName(settings.CatalogGenerator.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return genDir != null && Directory.Exists(genDir) ? genDir : null;
        }

        /// <summary>parts/ folder from deploy.json dev repo when available, else bundle copy.</summary>
        public static string ResolvePartsDir()
        {
            string? devParts = TryResolveDevPartsDir();
            if (devParts != null)
                return devParts;

            return PartsDir;
        }

        /// <summary>catalog_generator source for deploy (dev repo via deploy.json, else bundle copy).</summary>
        public static string ResolveCatalogGeneratorSource()
        {
            DeploySettings? settings = DeploySettings.TryLoad();
            if (!string.IsNullOrWhiteSpace(settings?.CatalogGenerator) &&
                Directory.Exists(settings.CatalogGenerator))
            {
                return settings.CatalogGenerator;
            }

            if (Directory.Exists(CatalogGeneratorDir))
                return CatalogGeneratorDir;

            throw new DirectoryNotFoundException(
                "catalog_generator not found. Run Install-Plant3DCatalogComposer.ps1 once from the API repo.");
        }

        public static string ActiveSceneJson => Path.Combine(CatalogGeneratorDir, ".active_scene.json");

        /// <summary>Plant 3D CustomScripts folder (TESTACPSCRIPT entry point).</summary>
        public static string CustomScriptsDir =>
            @"C:\AutoCAD Plant 3D 2026 Content\CPak Common\CustomScripts";

        public static string CustomScriptsActiveSceneJson =>
            Path.Combine(CustomScriptsDir, ".active_scene.json");

        public static string CustomScriptsModeFlag =>
            Path.Combine(CustomScriptsDir, ".p3d_composer_mode");

        public static string CustomScriptsScenePointer =>
            Path.Combine(CustomScriptsDir, ".p3d_composer_scene_path");

        public static string CustomScriptsLibDir =>
            Path.Combine(CustomScriptsDir, "p3d_composer");

        /// <summary>Per-drawing scene store under %AppData%.</summary>
        public static string SceneStoreDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Plant3DCatalogComposer",
                "scenes");

        public static string GetDrawingSceneKey(string? dwgPath)
        {
            if (string.IsNullOrWhiteSpace(dwgPath))
                return "unsaved";

            string full = Path.GetFullPath(dwgPath);
            string name = SanitizeFileName(Path.GetFileNameWithoutExtension(full));
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(full.ToLowerInvariant()));
            string id = Convert.ToHexString(hash)[..12];
            return $"{name}_{id}";
        }

        private static string LegacyDrawingSceneKey(string? dwgPath)
        {
            if (string.IsNullOrWhiteSpace(dwgPath))
                return "unsaved";

            return SanitizeFileName(Path.GetFileNameWithoutExtension(dwgPath));
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "unsaved" : name;
        }

        public static string GetSceneStorePath(string? dwgPath)
        {
            if (string.IsNullOrWhiteSpace(dwgPath))
                return Path.Combine(SceneStoreDirectory, "unsaved.scene.json");

            string full = Path.GetFullPath(dwgPath);
            string keyed = Path.Combine(SceneStoreDirectory, GetDrawingSceneKey(full) + ".scene.json");
            if (File.Exists(keyed))
                return keyed;

            // Saved drawing: use keyed path so different folders with the same file name do not share one JSON.
            if (Path.IsPathRooted(full) && File.Exists(full))
                return keyed;

            string legacy = Path.Combine(
                SceneStoreDirectory,
                LegacyDrawingSceneKey(full) + ".scene.json");
            return File.Exists(legacy) ? legacy : keyed;
        }

        public static string LegacySceneStorePath(string? dwgPath)
        {
            if (string.IsNullOrWhiteSpace(dwgPath))
                return Path.Combine(SceneStoreDirectory, "unsaved.scene.json");

            return Path.Combine(
                SceneStoreDirectory,
                LegacyDrawingSceneKey(Path.GetFullPath(dwgPath)) + ".scene.json");
        }
    }
}

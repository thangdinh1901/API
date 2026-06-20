using System.IO;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Writes composer_live.py (preview rebuild). Catalog package preview is UI-only (.txt reference).</summary>
    internal static class ComposerLiveScriptService
    {
        public const string LiveFileName = "composer_live.py";
        /// <summary>Human-readable catalog bundle for the Code tab — not valid Python (contains XML).</summary>
        public const string CatalogFileName = "composer_catalog.txt";

        public static string GeneratePreview(ValveProject project) =>
            PythonCodeGenerator.Generate(project);

        public static CatalogPackage BuildCatalogPackage(ValveProject project) =>
            CatalogCodeGenerator.BuildPackage(project);

        public static string GenerateCatalogPackage(ValveProject project) =>
            BuildCatalogPackage(project).ToDisplayText();

        public static string Write(ValveProject project)
        {
            string code = GeneratePreview(project);
            WriteToDeployPaths(LiveFileName, code);
            return Path.Combine(ProjectPaths.CustomScriptsLibDir, LiveFileName);
        }

        public static string WriteCatalogPackage(ValveProject project, string? code = null)
        {
            code ??= GenerateCatalogPackage(project);
            // Reference only for the user — must not be .py (PLANTREGISTERCUSTOMSCRIPTS compiles all scripts).
            WriteIfDirExists(Path.Combine(ProjectPaths.CustomScriptsLibDir, CatalogFileName), code);
            return Path.Combine(ProjectPaths.CustomScriptsLibDir, CatalogFileName);
        }

        private static void WriteToDeployPaths(string fileName, string code)
        {
            string customPath = Path.Combine(ProjectPaths.CustomScriptsLibDir, fileName);
            WriteIfDirExists(customPath, code);
        }

        private static void WriteIfDirExists(string path, string code)
        {
            string? dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir))
                return;

            Directory.CreateDirectory(dir);
            File.WriteAllText(path, code);
        }
    }
}

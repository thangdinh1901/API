using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.ApplicationServices;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class CatalogDeployResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        /// <summary>Catalog CUST_*.py parts deployed.</summary>
        public int PartCount { get; init; }
        /// <summary>All .py files copied to CustomScripts (parts, libraries, shared).</summary>
        public int ScriptCount { get; init; }
        public int PycacheFoldersCleared { get; init; }
        public bool RegisterCommandQueued { get; init; }
        public string? ManifestPath { get; init; }
        public PluginDeployResult? PluginDeploy { get; init; }

        public static string StatusLine(int scriptCount) =>
            $"{scriptCount} Python scripts deployed.";
    }

    /// <summary>Deploy catalog_generator from dev repo (or bundle) to Plant 3D CustomScripts.</summary>
    internal static class CatalogDeployService
    {
        private static readonly string[] SupportModules = { "STUD_BOLTS", "NUTS", "STRUCTURAL_PROFILES" };
        private static readonly string[] LegacySupportModules = { "Stud_Bolts", "Nuts", "Structural_profiles" };
        private static readonly string[] MetadataFiles = { "ScriptGroup.xml", "variants.xml", "variants.map" };
        private static readonly Regex SubfolderImport = new(
            @"^from [A-Z0-9_]+\.CUST_[A-Z0-9_]+ import ",
            RegexOptions.Compiled);

        private static readonly HashSet<string> ReservedCustomScriptsDirs =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "STUD_BOLTS", "NUTS", "STRUCTURAL_PROFILES",
                "p3d_composer", "__pycache__", "Resources",
            };

        public static CatalogDeployResult DeployToCustomScripts()
        {
            if (!Directory.Exists(ProjectPaths.CustomScriptsDir))
            {
                return Fail($"CustomScripts not found: {ProjectPaths.CustomScriptsDir}");
            }

            string genSrc = ProjectPaths.ResolveCatalogGeneratorSource();
            string partsSrc = Path.Combine(genSrc, "parts");
            if (!Directory.Exists(partsSrc))
                return Fail($"Parts folder not found: {partsSrc}");

            try
            {
                // Drop stale bytecode before and after copy so PLANTREGISTERCUSTOMSCRIPTS rebuilds .pyc.
                int pycacheCleared = ClearPythonCache(ProjectPaths.CustomScriptsDir);

                RemoveLegacySupportFolders(ProjectPaths.CustomScriptsDir);
                int removed = RemoveOrphanedCatalogParts(partsSrc, ProjectPaths.CustomScriptsDir);

                int partCount = DeployCustomParts(partsSrc, ProjectPaths.CustomScriptsDir);
                int scriptCount = partCount;
                scriptCount += DeploySupportModules(partsSrc, ProjectPaths.CustomScriptsDir);
                scriptCount += DeploySharedFiles(genSrc, ProjectPaths.CustomScriptsDir);
                scriptCount += DeployComposerLib(genSrc, ProjectPaths.CustomScriptsDir);
                DeployMetadata(genSrc, ProjectPaths.CustomScriptsDir);

                pycacheCleared += ClearPythonCache(ProjectPaths.CustomScriptsDir);

                PluginDeployResult plugin = CatalogPluginDeployService.TryStagePluginDll();
                string manifestPath = CatalogDeployManifestWriter.Write(
                    ProjectPaths.CustomScriptsDir,
                    scriptCount,
                    pycacheCleared,
                    registerQueued: false,
                    plugin);

                string message = CatalogDeployGuidance.BuildSummary(scriptCount, registerQueued: false);
                if (removed > 0)
                    message = $"Removed {removed} orphaned catalog part(s).{Environment.NewLine}{message}";

                return new CatalogDeployResult
                {
                    Success = true,
                    PartCount = partCount,
                    ScriptCount = scriptCount,
                    PycacheFoldersCleared = pycacheCleared,
                    ManifestPath = manifestPath,
                    PluginDeploy = plugin,
                    Message = message,
                };
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        private static CatalogDeployResult Fail(string message) =>
            new() { Success = false, Message = message };

        /// <summary>
        /// Plant 3D compiles .py → .pyc and updates variant paths for Spec Editor.
        /// Creates CustomScripts/__pycache__ automatically — do not copy or create it manually.
        /// </summary>
        public static bool TryQueueRegisterCustomScripts(
            Document? doc,
            CatalogDeployResult deploy)
        {
            if (doc == null)
                return false;

            if (!string.IsNullOrEmpty(deploy.ManifestPath))
                CatalogDeployManifestWriter.MarkRegisterQueued(deploy.ManifestPath);

            doc.Editor.WriteMessage(
                $"\nP3D Composer: deployed {deploy.ScriptCount} script(s).");
            doc.SendStringToExecute("PLANTREGISTERCUSTOMSCRIPTS\n", true, false, false);
            return true;
        }

        private static void RemoveLegacySupportFolders(string customScripts)
        {
            foreach (string name in LegacySupportModules)
            {
                string path = Path.Combine(customScripts, name);
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
        }

        /// <summary>
        /// Remove CUST_* runtime files on CustomScripts when the part was deleted from catalog_generator/parts on D:.
        /// Includes flat CUST_*.py/.xml, legacy part subfolders, and __pycache__/CUST_*.pyc.
        /// </summary>
        private static int RemoveOrphanedCatalogParts(string partsSrc, string customScripts)
        {
            var activePartIds = CollectActivePartIds(partsSrc);
            int removed = 0;

            foreach (string pyPath in Directory.EnumerateFiles(customScripts, "CUST_*.py"))
            {
                string scriptName = Path.GetFileNameWithoutExtension(pyPath);
                if (!scriptName.StartsWith("CUST_", StringComparison.Ordinal))
                    continue;

                string partId = scriptName[5..];
                if (activePartIds.Contains(partId))
                    continue;

                if (RemoveCatalogPartArtifacts(customScripts, partId))
                    removed++;
            }

            foreach (string dir in Directory.EnumerateDirectories(customScripts))
            {
                string partId = Path.GetFileName(dir);
                if (ReservedCustomScriptsDirs.Contains(partId) || activePartIds.Contains(partId))
                    continue;

                if (!LooksLikeCatalogPartFolder(dir, partId))
                    continue;

                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                    removed++;
                }
            }

            return removed;
        }

        private static HashSet<string> CollectActivePartIds(string partsSrc)
        {
            var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(partsSrc))
                return active;

            foreach (string partDir in Directory.EnumerateDirectories(partsSrc))
            {
                string partId = Path.GetFileName(partDir);
                if (StandardCatalogGuard.IsSandboxDirectory(partId))
                    continue;

                if (File.Exists(Path.Combine(partDir, "catalog_entry.py")))
                    active.Add(partId);
            }

            return active;
        }

        private static bool LooksLikeCatalogPartFolder(string dir, string partId)
        {
            if (File.Exists(Path.Combine(dir, $"CUST_{partId}.py")))
                return true;

            string nested = Path.Combine(dir, partId, $"CUST_{partId}.py");
            return File.Exists(nested);
        }

        private static bool RemoveCatalogPartArtifacts(string customScripts, string partId)
        {
            bool removed = false;

            string py = Path.Combine(customScripts, $"CUST_{partId}.py");
            if (File.Exists(py))
            {
                File.Delete(py);
                removed = true;
            }

            string xml = Path.Combine(customScripts, $"CUST_{partId}.xml");
            if (File.Exists(xml))
                File.Delete(xml);

            string partFolder = Path.Combine(customScripts, partId);
            if (Directory.Exists(partFolder))
            {
                Directory.Delete(partFolder, recursive: true);
                removed = true;
            }

            if (RemovePycacheForScript(customScripts, $"CUST_{partId}"))
                removed = true;

            return removed;
        }

        private static bool RemovePycacheForScript(string customScripts, string scriptName)
        {
            string cacheDir = Path.Combine(customScripts, "__pycache__");
            if (!Directory.Exists(cacheDir))
                return false;

            bool removed = false;
            foreach (string pyc in Directory.EnumerateFiles(cacheDir, $"{scriptName}*.pyc"))
            {
                File.Delete(pyc);
                removed = true;
            }

            return removed;
        }

        /// <summary>Remove all __pycache__ folders under CustomScripts before PLANTREGISTERCUSTOMSCRIPTS.</summary>
        internal static int ClearPythonCache(string customScripts)
        {
            if (!Directory.Exists(customScripts))
                return 0;

            int cleared = 0;
            foreach (string dir in Directory.EnumerateDirectories(customScripts, "__pycache__", SearchOption.AllDirectories))
            {
                Directory.Delete(dir, recursive: true);
                cleared++;
            }

            return cleared;
        }

        private static int DeployCustomParts(string partsSrc, string customScripts)
        {
            int count = 0;
            foreach (string partDir in Directory.EnumerateDirectories(partsSrc))
            {
                string partId = Path.GetFileName(partDir);
                string entry = Path.Combine(partDir, "catalog_entry.py");
                if (!File.Exists(entry))
                    continue;

                string legacyFolder = Path.Combine(customScripts, partId);
                if (Directory.Exists(legacyFolder))
                    Directory.Delete(legacyFolder, recursive: true);

                string geometry = Path.Combine(partDir, partId, $"CUST_{partId}.py");
                string destPy = Path.Combine(customScripts, $"CUST_{partId}.py");
                string content = File.Exists(geometry)
                    ? EnsureSupportImports(MergeCatalogPartPy(entry, geometry))
                    : EnsureSupportImports(File.ReadAllText(entry, Encoding.UTF8));
                File.WriteAllText(destPy, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                string entryXml = Path.Combine(partDir, "catalog_entry.xml");
                if (File.Exists(entryXml))
                    File.Copy(entryXml, Path.Combine(customScripts, $"CUST_{partId}.xml"), overwrite: true);

                count++;
            }

            return count;
        }

        internal static string MergeCatalogPartPy(string entryPath, string geometryPath)
        {
            string[] entryLines = File.ReadAllLines(entryPath);
            string geom = SanitizeGeometryForMerge(File.ReadAllText(geometryPath, Encoding.UTF8).TrimEnd());

            IEnumerable<string> varmain = entryLines.Where(l =>
                l.Contains("varmain.custom", StringComparison.Ordinal));
            IEnumerable<string> entryImports = entryLines.Where(l =>
                (l.StartsWith("import ", StringComparison.Ordinal) ||
                 l.StartsWith("from ", StringComparison.Ordinal)) &&
                !l.Contains("varmain.custom", StringComparison.Ordinal) &&
                !SubfolderImport.IsMatch(l));
            IEnumerable<string> body = entryLines.Where(l =>
                !l.Contains("varmain.custom", StringComparison.Ordinal) &&
                !SubfolderImport.IsMatch(l) &&
                !(l.StartsWith("import ", StringComparison.Ordinal) ||
                  l.StartsWith("from ", StringComparison.Ordinal)));

            var bodyLines = body.ToList();
            int activateIdx = bodyLines.FindIndex(l => l.Contains("@activate", StringComparison.Ordinal));
            if (activateIdx >= 0)
                bodyLines = bodyLines.Skip(activateIdx).ToList();

            var chunks = new List<string>();
            string varmainText = string.Join(Environment.NewLine, varmain);
            if (!string.IsNullOrWhiteSpace(varmainText))
                chunks.Add(varmainText);
            string entryImportText = string.Join(Environment.NewLine, entryImports);
            if (!string.IsNullOrWhiteSpace(entryImportText))
                chunks.Add(entryImportText);
            if (!string.IsNullOrWhiteSpace(geom))
                chunks.Add(geom);
            string bodyText = string.Join(Environment.NewLine, bodyLines).Trim();
            if (!string.IsNullOrWhiteSpace(bodyText))
                chunks.Add(bodyText);

            return EnsureSupportImports(string.Join(Environment.NewLine + Environment.NewLine, chunks).TrimEnd()
                + Environment.NewLine);
        }

        private static readonly Regex ModuleLevelCatalogParamsImport = new(
            @"^import catalog_params\s*$|^from catalog_params import ",
            RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>Hoist shared modules referenced by merged entry points (e.g. catalog_params).</summary>
        internal static string EnsureSupportImports(string content)
        {
            if (content.Contains("catalog_params.", StringComparison.Ordinal)
                && !ModuleLevelCatalogParamsImport.IsMatch(content))
            {
                const string importLine = "import catalog_params";
                int varmainEnd = content.IndexOf("varmain.custom", StringComparison.Ordinal);
                if (varmainEnd >= 0)
                {
                    int lineEnd = content.IndexOf('\n', varmainEnd);
                    int insertAt = lineEnd >= 0 ? lineEnd + 1 : content.Length;
                    content = content.Insert(insertAt, Environment.NewLine + importLine + Environment.NewLine);
                }
                else
                {
                    content = importLine + Environment.NewLine + Environment.NewLine + content;
                }
            }

            return content;
        }

        private static readonly Regex PackageCatalogImport = new(
            @"^from (?<part>[A-Z0-9_]+)\.CUST_\k<part> import (?<symbols>.+)$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static string SanitizeGeometryForMerge(string geometry)
        {
            geometry = PackageCatalogImport.Replace(
                geometry,
                "from CUST_${part} import ${symbols}");
            geometry = CatalogPortTemplates.SanitizeNestedCatalogGeometry(geometry);

            var lines = geometry.Split('\n')
                .Select(l => l.TrimEnd('\r'))
                .Where(l =>
                    !l.Contains("varmain.custom", StringComparison.Ordinal) &&
                    !l.StartsWith("# Port Manager:", StringComparison.Ordinal))
                .ToList();

            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
                lines.RemoveAt(0);

            return string.Join(Environment.NewLine, lines).TrimEnd();
        }

        private static int DeploySupportModules(string partsSrc, string customScripts)
        {
            int count = 0;
            foreach (string name in SupportModules)
            {
                string src = Path.Combine(partsSrc, name);
                if (!Directory.Exists(src))
                    continue;

                string dst = Path.Combine(customScripts, name);
                if (Directory.Exists(dst))
                    Directory.Delete(dst, recursive: true);
                count += CopyDirectoryPyFiles(src, dst);
            }

            return count;
        }

        private static void DeployMetadata(string genSrc, string customScripts)
        {
            CatalogMetadataSyncService.SyncFromParts(genSrc);
            foreach (string name in MetadataFiles)
            {
                string src = Path.Combine(genSrc, name);
                if (File.Exists(src))
                    File.Copy(src, Path.Combine(customScripts, name), overwrite: true);
            }

            string standardSets = Path.Combine(genSrc, "standard_sets.json");
            if (File.Exists(standardSets))
                File.Copy(standardSets, Path.Combine(customScripts, "standard_sets.json"), overwrite: true);
        }

        private static int DeployComposerLib(string genSrc, string customScripts)
        {
            string libSrc = Path.Combine(genSrc, "p3d_composer");
            string libDst = Path.Combine(customScripts, "p3d_composer");
            if (!Directory.Exists(libSrc))
                return 0;

            Directory.CreateDirectory(libDst);
            int count = 0;
            foreach (string file in Directory.EnumerateFiles(libSrc, "*.py"))
            {
                string name = Path.GetFileName(file);
                if (IsComposerReferencePy(name))
                    continue;

                File.Copy(file, Path.Combine(libDst, name), overwrite: true);
                count++;
            }

            // Legacy reference bundle (XML inside .py) breaks PLANTREGISTERCUSTOMSCRIPTS.
            foreach (string stale in new[] { "composer_catalog.py", "COMPOSER_CATALOG.py" })
            {
                string path = Path.Combine(libDst, stale);
                if (File.Exists(path))
                    File.Delete(path);
            }

            string bundleStale = Path.Combine(libSrc, "composer_catalog.py");
            if (File.Exists(bundleStale))
                File.Delete(bundleStale);

            // Composer preview modules are not catalog scripts — drop stray XML/pyc from failed registers.
            foreach (string xml in Directory.EnumerateFiles(libDst, "*.xml"))
                File.Delete(xml);

            string composerCache = Path.Combine(libDst, "__pycache__");
            if (Directory.Exists(composerCache))
                Directory.Delete(composerCache, recursive: true);

            foreach (string name in new[] { "p3d_composer_rebuild.py", "P3D_COMPOSER_REBUILD.xml" })
            {
                string src = Path.Combine(genSrc, name);
                if (File.Exists(src))
                    File.Copy(src, Path.Combine(customScripts, name), overwrite: true);
            }

            return count;
        }

        private static bool IsComposerReferencePy(string fileName) =>
            fileName.Equals("composer_catalog.py", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("COMPOSER_CATALOG.py", StringComparison.OrdinalIgnoreCase);

        private static int DeploySharedFiles(string genSrc, string customScripts)
        {
            int count = 0;
            foreach (string name in new[] { "pipe_sizes.py", "catalog_params.py", "sw_fitting_geom.py", "stubend_geom.py", "lj_stud_bolts.py" })
            {
                string src = Path.Combine(genSrc, name);
                if (!File.Exists(src))
                    continue;
                File.Copy(src, Path.Combine(customScripts, name), overwrite: true);
                count++;
            }

            string? primitives = ResolvePrimitivesPath();
            if (primitives != null && File.Exists(primitives))
            {
                File.Copy(primitives, Path.Combine(customScripts, "primitives.py"), overwrite: true);
                count++;
            }

            return count;
        }

        private static string? ResolvePrimitivesPath()
        {
            DeploySettings? settings = DeploySettings.TryLoad();
            if (!string.IsNullOrWhiteSpace(settings?.PrimitivesPy) && File.Exists(settings.PrimitivesPy))
                return settings.PrimitivesPy;

            string sibling = Path.Combine(ProjectPaths.PluginDirectory, "primitives.py");
            if (File.Exists(sibling))
                return sibling;

            if (!string.IsNullOrWhiteSpace(settings?.ApiRoot))
            {
                string fromApi = Path.Combine(settings.ApiRoot, "Plant3DSkeletonManager", "primitives.py");
                if (File.Exists(fromApi))
                    return fromApi;
            }

            return null;
        }

        private static int CopyDirectoryPyFiles(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            int count = 0;
            foreach (string file in Directory.EnumerateFiles(sourceDir, "*.py", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(sourceDir, file);
                string target = Path.Combine(destDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: true);
                count++;
            }

            return count;
        }
    }
}

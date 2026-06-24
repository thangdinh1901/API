using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class CatalogTestResult
    {
        public bool CanRun { get; init; }
        public string ScriptName { get; init; } = string.Empty;
        public string CommandLine { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    internal static class CatalogTestService
    {
        private static int _testSequence;

        public static CatalogTestResult BuildTestCommand(string dwgPath)
        {
            ValveProject project = SceneGraphCatalogService.ReloadScene(dwgPath);
            CatalogExportPrepareService.PrepareSceneForExport(project);

            string scriptName = CatalogProjectService.PreviewScriptName(project);
            string pyPath = Path.Combine(ProjectPaths.CustomScriptsDir, scriptName + ".py");
            if (!File.Exists(pyPath))
            {
                return new CatalogTestResult
                {
                    CanRun = false,
                    ScriptName = scriptName,
                    Message =
                        $"Script not deployed: {pyPath}\n\n"
                        + "Run Generate Code, then Deploy Catalog (scene must match Catalog Project name).\n"
                        + "To preview the Scene tree while editing, use Rebuild Scene — Test Catalog runs "
                        + "wrapper → hot_reload → CUST_*.py (reloads from disk, no CAD restart).",
                };
            }

            string partId = scriptName.StartsWith("CUST_", StringComparison.Ordinal)
                ? scriptName[5..]
                : scriptName;
            IReadOnlyList<(string Name, string Value)> args = ResolveTestArguments(project, partId);

            if (!CatalogSceneManifest.TryReadFromScript(pyPath, out _))
            {
                return new CatalogTestResult
                {
                    CanRun = true,
                    ScriptName = scriptName,
                    CommandLine = BuildInvoke(scriptName, args, out _),
                    Message =
                        $"Testing {scriptName} — deployed script has no scene manifest (re-run Deploy Catalog after plugin update).{Environment.NewLine}"
                        + $"Current scene: {CatalogSceneManifest.Build(project)}",
                };
            }

            if (!CatalogSceneManifest.MatchesProject(project, pyPath, out string manifestMessage))
            {
                if (project.Parts.Count == 0)
                {
                    string blankDrawingCommand = BuildInvoke(scriptName, args, out _);
                    return new CatalogTestResult
                    {
                        CanRun = true,
                        ScriptName = scriptName,
                        CommandLine = blankDrawingCommand,
                        Message =
                            $"Testing deployed {scriptName} (blank drawing — catalog script only, scene parity skipped).{Environment.NewLine}"
                            + $"Deployed: {manifestMessage}{Environment.NewLine}"
                            + "Preview geometry will be erased before test.",
                    };
                }

                return BuildMismatchDeployedTest(scriptName, args, project, manifestMessage);
            }

            string command = BuildInvoke(scriptName, args, out _);
            return new CatalogTestResult
            {
                CanRun = true,
                ScriptName = scriptName,
                CommandLine = command,
                Message =
                    $"Testing deployed {scriptName} — {manifestMessage}{Environment.NewLine}"
                    + "Preview geometry will be erased before test so only catalog script geometry remains.",
            };
        }

        private static CatalogTestResult BuildMismatchDeployedTest(
            string scriptName,
            IReadOnlyList<(string Name, string Value)> args,
            ValveProject project,
            string manifestMessage)
        {
            string command = BuildInvoke(scriptName, args, out _);
            return new CatalogTestResult
            {
                CanRun = true,
                ScriptName = scriptName,
                CommandLine = command,
                Message =
                    $"Testing deployed {scriptName} (this drawing's scene differs from deploy — geometry from CustomScripts).{Environment.NewLine}"
                    + $"Scene JSON:  {CatalogSceneManifest.Build(project)}{Environment.NewLine}"
                    + $"{manifestMessage}{Environment.NewLine}"
                    + "Run Generate + Deploy on this drawing to sync scene JSON with deploy.",
            };
        }

        private static string BuildInvoke(
            string scriptName,
            IReadOnlyList<(string Name, string Value)> args,
            out int seq)
        {
            // Route via wrapper → hot_reload → reload(CUST_*) — same SDK pattern as SPDS sample.
            var parts = new List<string> { "(testacpscript \"wrapper\" \"S\" \"" + scriptName + "\"" };
            foreach ((string name, string value) in args)
            {
                parts.Add($"\"{name}\"");
                parts.Add($"\"{value}\"");
            }

            parts.Add(")");
            string invoke = string.Join(" ", parts);
            seq = Interlocked.Increment(ref _testSequence);
            return WrapperScript.WrapTestCatalogInvoke(seq, invoke, registerScripts: false);
        }

        public static bool TryQueueTest(Document? doc, string dwgPath)
        {
            CatalogTestResult test = BuildTestCommand(dwgPath);
            if (!test.CanRun || doc == null)
                return false;

            IdleRebuildService.CancelPending();
            CatalogDeployService.InvalidateBeforeCatalogTest(test.ScriptName);
            doc.Editor.WriteMessage($"\nP3D Composer: {test.Message}");
            // No trailing "\n": the wrapped LISP already ends with a space that executes it. An extra
            // newline lands at the Command prompt as a second Enter, repeating TESTACPSCRIPT with no
            // args, which pops up Autodesk Help. (Rebuild sends the same way without a newline.)
            doc.SendStringToExecute(test.CommandLine, true, false, false);
            return true;
        }

        /// <summary>Preview a standard library part only — does not rebuild the custom Part Family scene.</summary>
        public static CatalogTestResult BuildLibraryPartPreview(
            CustomPartDefinition part,
            double dn,
            ValveProject? project = null)
        {
            string scriptName = $"CUST_{part.Id}";
            string pyPath = Path.Combine(ProjectPaths.CustomScriptsDir, scriptName + ".py");
            if (!File.Exists(pyPath))
            {
                return new CatalogTestResult
                {
                    CanRun = false,
                    ScriptName = scriptName,
                    Message = $"Script not deployed: {pyPath}\nRun Deploy Catalog first.",
                };
            }

            IReadOnlyList<(string Name, string Value)> args =
                ResolveLibraryPreviewArguments(part, dn, project);
            string command = BuildInvoke(scriptName, args, out _);
            return new CatalogTestResult
            {
                CanRun = true,
                ScriptName = scriptName,
                CommandLine = command,
                Message =
                    $"Preview {part.DisplayName} ({scriptName}) — previous preview geometry will be erased.",
            };
        }

        public static bool TryQueueLibraryPartPreview(
            Document? doc,
            CustomPartDefinition part,
            double dn,
            ValveProject? project = null)
        {
            CatalogTestResult preview = BuildLibraryPartPreview(part, dn, project);
            if (!preview.CanRun || doc == null)
                return false;

            doc.Editor.WriteMessage($"\nP3D Composer: {preview.Message}");
            // See TryQueueTest: no trailing "\n" (avoids repeating TESTACPSCRIPT → Autodesk Help).
            doc.SendStringToExecute(preview.CommandLine, true, false, false);
            return true;
        }

        private static IReadOnlyList<(string Name, string Value)> ResolveLibraryPreviewArguments(
            CustomPartDefinition part,
            double dn,
            ValveProject? project)
        {
            var list = new List<(string, string)>();
            int dnMm = dn > 0 ? (int)Math.Round(dn) : (int)Math.Round(part.DefaultDN > 0 ? part.DefaultDN : 100);
            list.Add(("DN", dnMm.ToString(CultureInfo.InvariantCulture)));

            Dictionary<string, string>? xmlDefaults = TryLoadParamDefaults(part.Id);
            if (xmlDefaults != null)
            {
                foreach (KeyValuePair<string, string> kv in xmlDefaults)
                {
                    if (kv.Key.Equals("DN", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (list.Any(a => a.Item1.Equals(kv.Key, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    string value = ResolveLibraryPreviewParamValue(part, project, dnMm, kv.Key, kv.Value);
                    list.Add((kv.Key, value));
                }
            }

            return list;
        }

        private static string ResolveLibraryPreviewParamValue(
            CustomPartDefinition part,
            ValveProject? project,
            int dnMm,
            string name,
            string defaultValue)
        {
            if (name.Equals("DN2", StringComparison.OrdinalIgnoreCase))
            {
                if (project?.Parameters.DN2 > 0)
                {
                    return ((int)Math.Round(project.Parameters.DN2)).ToString(CultureInfo.InvariantCulture);
                }

                foreach (CatalogPartParam spec in part.CatalogParams)
                {
                    if (spec.UseSkeletonDN2 || spec.Name.Equals("DN2", StringComparison.OrdinalIgnoreCase))
                    {
                        int small = BwFittingSizeCatalog.DefaultReducerSmallDn(dnMm);
                        return small.ToString(CultureInfo.InvariantCulture);
                    }
                }
            }

            if (name.Equals("CEL", StringComparison.OrdinalIgnoreCase))
                return "0";

            return defaultValue;
        }

        private static IReadOnlyList<(string Name, string Value)> ResolveTestArguments(
            ValveProject project,
            string partId)
        {
            var list = new List<(string, string)>();
            int dn = project.Parameters.DN > 0
                ? (int)Math.Round(project.Parameters.DN)
                : 100;

            Dictionary<string, string>? xmlDefaults = TryLoadParamDefaults(partId);
            if (xmlDefaults != null && xmlDefaults.TryGetValue("DN", out string? dnDefault))
            {
                if (project.Parameters.DN <= 0 && int.TryParse(dnDefault, out int parsed))
                    dn = parsed;
            }

            list.Add(("DN", dn.ToString(CultureInfo.InvariantCulture)));

            foreach ((string name, double value) in CatalogExportPrepareService.CollectExportDimensionParams(project))
            {
                list.Add((name, value.ToString("0.###", CultureInfo.InvariantCulture)));
            }

            if (xmlDefaults != null)
            {
                foreach (KeyValuePair<string, string> kv in xmlDefaults)
                {
                    if (kv.Key.Equals("DN", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (list.Any(a => a.Item1.Equals(kv.Key, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    string value = ResolveParamValue(project, kv.Key, kv.Value);
                    list.Add((kv.Key, value));
                }
            }

            return list;
        }

        private static string ResolveParamValue(ValveProject project, string name, string defaultValue)
        {
            if (name.Equals("DN2", StringComparison.OrdinalIgnoreCase))
            {
                int dn2 = project.Parameters.DN2 > 0
                    ? (int)Math.Round(project.Parameters.DN2)
                    : BwFittingSizeCatalog.DefaultReducerSmallDn((int)Math.Round(project.Parameters.DN));
                return dn2.ToString(CultureInfo.InvariantCulture);
            }

            double fromProject = ProjectDimensionService.GetValue(project, name);
            if (fromProject > 0)
                return fromProject.ToString("0.###", CultureInfo.InvariantCulture);

            if (name.Equals("CEL", StringComparison.OrdinalIgnoreCase))
                return "0";

            return defaultValue;
        }

        private static Dictionary<string, string>? TryLoadParamDefaults(string partId)
        {
            string? partsDir = ProjectPaths.TryResolveDevPartsDir() ?? ProjectPaths.PartsDir;
            string xmlPath = Path.Combine(partsDir, partId, "catalog_entry.xml");
            if (!File.Exists(xmlPath))
                return null;

            try
            {
                XDocument doc = XDocument.Load(xmlPath);
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (XElement param in doc.Descendants("Param"))
                {
                    string? name = param.Attribute("Name")?.Value;
                    string? value = param.Attribute("Value")?.Value;
                    if (!string.IsNullOrEmpty(name) && value != null)
                        dict[name] = value;
                }

                return dict.Count > 0 ? dict : null;
            }
            catch
            {
                return null;
            }
        }
    }
}

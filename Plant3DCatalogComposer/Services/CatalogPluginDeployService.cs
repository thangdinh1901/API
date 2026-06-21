using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class PluginDeployResult
    {
        public bool Attempted { get; init; }
        public bool CopiedToBundle { get; init; }
        public bool CopiedToNetload { get; init; }
        public bool BuiltDllIsNewerThanLoaded { get; init; }
        public bool RestartRecommended { get; init; }
        public string? BuiltDllPath { get; init; }
        public string NetloadDllPath { get; init; } =
            Path.Combine(ProjectPaths.CustomScriptsDir, "Plant3DCatalogComposer.dll");
        public string BundleRoot { get; init; } =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Autodesk\ApplicationPlugins\Plant3DCatalogComposer.bundle");
    }

    /// <summary>
    /// Stage the newest built plugin DLL for NETLOAD / next CAD session.
    /// Cannot replace the assembly that is currently loaded in-process.
    /// </summary>
    internal static class CatalogPluginDeployService
    {
        private static readonly string BundleContentsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Autodesk\ApplicationPlugins\Plant3DCatalogComposer.bundle\Contents");

        public static PluginDeployResult TryStagePluginDll()
        {
            string? builtDll = ResolveNewestBuiltDll();
            if (builtDll == null)
            {
                return new PluginDeployResult { Attempted = false };
            }

            string loadedDll = Assembly.GetExecutingAssembly().Location;
            bool builtIsNewer = File.GetLastWriteTimeUtc(builtDll) >
                                File.GetLastWriteTimeUtc(loadedDll);

            bool copiedBundle = TryCopy(builtDll, Path.Combine(BundleContentsDir, "Plant3DCatalogComposer.dll"));
            bool copiedNetload = TryCopy(builtDll, Path.Combine(
                ProjectPaths.CustomScriptsDir,
                "Plant3DCatalogComposer.dll"));

            if (copiedBundle)
                TryRefreshPackageContentsVersion(builtDll);

            bool restart = builtIsNewer;

            return new PluginDeployResult
            {
                Attempted = true,
                BuiltDllPath = builtDll,
                CopiedToBundle = copiedBundle,
                CopiedToNetload = copiedNetload,
                BuiltDllIsNewerThanLoaded = builtIsNewer,
                RestartRecommended = restart,
            };
        }

        internal static void TryRefreshPackageContentsVersion(string dllPath)
        {
            try
            {
                string? apiRoot = ProjectPaths.TryResolveApiRoot();
                if (string.IsNullOrWhiteSpace(apiRoot))
                    return;

                string template = Path.Combine(apiRoot, "Plant3DCatalogComposer", "PackageContents.xml");
                string bundleRoot = Path.GetDirectoryName(BundleContentsDir)!;
                string dest = Path.Combine(bundleRoot, "PackageContents.xml");
                if (!File.Exists(template))
                    return;

                string version = File.GetLastWriteTimeUtc(dllPath).ToString("yyyy.M.d.HHmm");
                string xml = File.ReadAllText(template);
                xml = Regex.Replace(
                    xml,
                    @"AppVersion=""[^""]*""",
                    $@"AppVersion=""{version}""",
                    RegexOptions.CultureInvariant);
                xml = Regex.Replace(
                    xml,
                    @"(<ComponentEntry[^>]*\sVersion="")[^""]*("")",
                    $"$1{version}$2",
                    RegexOptions.CultureInvariant);

                Directory.CreateDirectory(bundleRoot);
                File.WriteAllText(dest, xml);

                // Validate — corrupt XML breaks ApplicationPlugins autoload.
                var doc = new XmlDocument();
                doc.Load(dest);
            }
            catch
            {
                // best-effort; Install script also refreshes on build
            }
        }

        private static string? ResolveNewestBuiltDll()
        {
            string? apiRoot = ProjectPaths.TryResolveApiRoot();
            if (string.IsNullOrWhiteSpace(apiRoot))
                return null;

            string baseDir = Path.Combine(apiRoot, "Plant3DCatalogComposer", "bin");
            if (!Directory.Exists(baseDir))
                return null;

            string? newest = null;
            DateTime newestTime = DateTime.MinValue;
            foreach (string config in new[] { "Release", "Debug" })
            {
                string candidate = Path.Combine(
                    baseDir,
                    config,
                    "net8.0-windows",
                    "Plant3DCatalogComposer.dll");
                if (!File.Exists(candidate))
                    continue;

                DateTime write = File.GetLastWriteTimeUtc(candidate);
                if (write > newestTime)
                {
                    newestTime = write;
                    newest = candidate;
                }
            }

            return newest;
        }

        private static bool TryCopy(string source, string dest)
        {
            try
            {
                string? dir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.Copy(source, dest, overwrite: true);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}

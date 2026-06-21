using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class CatalogDeployManifest
    {
        public string DeployVersion { get; init; } = "2026.06.11";
        public DateTime DeployedAtUtc { get; init; }
        public int ScriptCount { get; init; }
        public int PycacheFoldersCleared { get; init; }
        public bool RegisterQueued { get; init; }
        public string? PluginDllPath { get; init; }
        public bool PluginRestartRecommended { get; init; }
        public Dictionary<string, string> KeyFileHashes { get; init; } = new();
    }

    internal static class CatalogDeployManifestWriter
    {
        private static readonly string[] KeyFiles =
        {
            "lj_stud_bolts.py",
            "CUST_GSK_FF_CL150.py",
            "CUST_LJ_RING_CL150_RF.py",
            "stubend_geom.py",
            "pipe_sizes.py",
            "catalog_params.py",
        };

        public static string Write(
            string customScripts,
            int scriptCount,
            int pycacheCleared,
            bool registerQueued,
            PluginDeployResult? plugin)
        {
            var manifest = new CatalogDeployManifest
            {
                DeployedAtUtc = DateTime.UtcNow,
                ScriptCount = scriptCount,
                PycacheFoldersCleared = pycacheCleared,
                RegisterQueued = registerQueued,
                PluginDllPath = plugin?.BuiltDllPath,
                PluginRestartRecommended = plugin?.RestartRecommended ?? false,
                KeyFileHashes = CollectKeyHashes(customScripts),
            };

            string path = Path.Combine(customScripts, "deploy_manifest.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(manifest, options), Encoding.UTF8);
            return path;
        }

        public static int CountPycacheFolders(string customScripts)
        {
            if (!Directory.Exists(customScripts))
                return 0;

            return Directory.EnumerateDirectories(customScripts, "__pycache__", SearchOption.AllDirectories).Count();
        }

        private static Dictionary<string, string> CollectKeyHashes(string customScripts)
        {
            var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in KeyFiles)
            {
                string path = Path.Combine(customScripts, name);
                if (!File.Exists(path))
                    continue;

                hashes[name] = ComputeMd5Hex(path);
            }

            return hashes;
        }

        public static void MarkRegisterQueued(string manifestPath)
        {
            if (!File.Exists(manifestPath))
                return;

            try
            {
                string json = File.ReadAllText(manifestPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                CatalogDeployManifest? manifest = JsonSerializer.Deserialize<CatalogDeployManifest>(json, options);
                if (manifest == null)
                    return;

                manifest = new CatalogDeployManifest
                {
                    DeployVersion = manifest.DeployVersion,
                    DeployedAtUtc = manifest.DeployedAtUtc,
                    ScriptCount = manifest.ScriptCount,
                    PycacheFoldersCleared = manifest.PycacheFoldersCleared,
                    RegisterQueued = true,
                    PluginDllPath = manifest.PluginDllPath,
                    PluginRestartRecommended = manifest.PluginRestartRecommended,
                    KeyFileHashes = manifest.KeyFileHashes,
                };

                File.WriteAllText(
                    manifestPath,
                    JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }),
                    Encoding.UTF8);
            }
            catch
            {
                // best-effort
            }
        }

        private static string ComputeMd5Hex(string path)
        {
            using var md5 = MD5.Create();
            using FileStream stream = File.OpenRead(path);
            byte[] hash = md5.ComputeHash(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }

    internal static class CatalogDeployGuidance
    {
        public static string BuildSummary(int scriptCount, bool registerQueued) =>
            registerQueued
                ? $"Deployed {scriptCount} script(s)."
                : $"Deployed {scriptCount} script(s). Run PLANTREGISTERCUSTOMSCRIPTS.";
    }
}

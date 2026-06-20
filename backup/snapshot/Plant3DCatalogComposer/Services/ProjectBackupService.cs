using System;
using System.IO;

namespace Plant3DCatalogComposer.Services
{
    internal static class ProjectBackupService
    {
        public static (bool Success, string Message) SaveSnapshot(string? label = null)
        {
            string? script = ResolveBackupScriptPath();
            if (script == null)
                return (false, "Backup script not found. Set deploy.json CatalogGenerator to the dev API repo.");

            return RunScript(script, "Save", label);
        }

        public static (bool Success, string Message) RestoreSnapshot()
        {
            string? script = ResolveBackupScriptPath();
            if (script == null)
                return (false, "Backup script not found.");

            return RunScript(script, "Restore");
        }

        public static (bool Success, string Message) GetStatus()
        {
            string? script = ResolveBackupScriptPath();
            if (script == null)
                return (false, "Backup script not found.");

            return RunScript(script, "Status");
        }

        private static string? ResolveBackupScriptPath()
        {
            string? apiRoot = ProjectPaths.TryResolveApiRoot();
            if (apiRoot != null)
            {
                string path = Path.Combine(apiRoot, "scripts", "Backup-CatalogComposer.ps1");
                if (File.Exists(path))
                    return path;
            }

            string sibling = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup-CatalogComposer.ps1");
            return File.Exists(sibling) ? sibling : null;
        }

        private static (bool Success, string Message) RunScript(string script, string action, string? label = null)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = BuildArgs(script, action, label),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using System.Diagnostics.Process? proc = System.Diagnostics.Process.Start(psi);
            if (proc == null)
                return (false, "Failed to start PowerShell.");

            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit(600_000);
            string output = (stdout + stderr).Trim();
            if (proc.ExitCode != 0)
                return (false, string.IsNullOrEmpty(output) ? $"Exit code {proc.ExitCode}" : output);

            return (true, string.IsNullOrEmpty(output) ? "OK" : output);
        }

        private static string BuildArgs(string script, string action, string? label)
        {
            string args = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -Action {action}";
            if (!string.IsNullOrWhiteSpace(label))
                args += $" -Label \"{label.Replace("\"", "")}\"";
            return args;
        }
    }
}

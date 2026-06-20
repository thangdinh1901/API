using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Plant3DCatalogComposer
{
    internal static class PluginVersion
    {
        public static string BuildStamp
        {
            get
            {
                try
                {
                    string? path = Assembly.GetExecutingAssembly().Location;
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        return File.GetLastWriteTime(path)
                            .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                    }
                }
                catch
                {
                    // ignore
                }

                return "unknown";
            }
        }

        public static string StatusSuffix => $"build {BuildStamp}";
    }
}

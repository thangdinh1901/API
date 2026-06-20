using System.IO;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal static class RebuildDnResolver
    {
        public static ValveProject? LoadActiveScene()
        {
            try
            {
                string path = ProjectPaths.CustomScriptsActiveSceneJson;
                if (!File.Exists(path))
                    return null;

                return JsonCodec.Deserialize(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        public static double ReadActiveSceneOrDefault(double fallback = 80)
        {
            ValveProject? project = LoadActiveScene();
            return project == null ? fallback : Resolve(project);
        }

        /// <summary>Nominal DN for testacpscript wrapper D= parameter (Plant 3D preview validation).</summary>
        public static double Resolve(ValveProject project)
        {
            foreach (PrimitiveNode part in project.Parts)
            {
                if (part.Parameters.TryGetValue("DN", out ParamValue? pv) && pv.Value > 0)
                    return pv.Value;
            }

            if (project.Parameters.DN > 0)
                return project.Parameters.DN;

            return 80;
        }
    }
}

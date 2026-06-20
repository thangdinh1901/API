using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// Standard ASME catalog parts in catalog_generator/parts are maintained in source control only.
    /// Composer scenes that insert them are for preview / port study — never overwrite library folders.
    /// </summary>
    internal static class StandardCatalogGuard
    {
        public const string SandboxRoot = "_composer_exports";

        public static bool IsProtectedStandardPart(string? partId)
        {
            if (string.IsNullOrWhiteSpace(partId))
                return false;

            if (partId.StartsWith("_", System.StringComparison.Ordinal))
                return false;

            CustomPartDefinition? def = CustomPartCatalog.FindById(partId);
            if (def?.IsStandardCatalog == true)
                return true;

            return BwSch40StandardCatalog.IsStandardPart(partId)
                || SwCl3000StandardCatalog.IsStandardPart(partId);
        }

        public static bool IsSandboxDirectory(string? directoryName) =>
            !string.IsNullOrWhiteSpace(directoryName)
            && directoryName.StartsWith("_", System.StringComparison.Ordinal);

        /// <summary>Scene contains only insertable standard catalog part(s) — reference / test, not a custom export.</summary>
        public static bool IsStandardReferenceScene(ValveProject project)
        {
            if (project.Parts.Count == 0)
                return false;

            return project.Parts.All(p =>
                p.Kind == SceneNodeKind.Catalog
                && IsProtectedStandardPart(p.CatalogPartId));
        }

        public static bool ShouldSkipSceneExport(ValveProject project) =>
            project.Parts.Count == 0 || IsStandardReferenceScene(project);

        public static bool IsStandardPortReference(ValveProject project) =>
            IsStandardReferenceScene(project)
            && (project.Ports.Count > 0
                || project.Operations.Count > 0
                || project.Parts.Any(HasNonIdentityTransform));

        public static string? TryGetSingleStandardPartId(ValveProject project)
        {
            if (project.Parts.Count != 1)
                return null;

            PrimitiveNode part = project.Parts[0];
            if (part.Kind != SceneNodeKind.Catalog || string.IsNullOrEmpty(part.CatalogPartId))
                return null;

            return IsProtectedStandardPart(part.CatalogPartId) ? part.CatalogPartId : null;
        }

        public static string ResolveExportFolderName(string folderName, bool isStandardPortReference) =>
            isStandardPortReference
                ? System.IO.Path.Combine(SandboxRoot, folderName)
                : folderName;

        private static bool HasNonIdentityTransform(PrimitiveNode part)
        {
            double x = part.Origin.Length > 0 ? part.Origin[0] : 0;
            double y = part.Origin.Length > 1 ? part.Origin[1] : 0;
            double z = part.Origin.Length > 2 ? part.Origin[2] : 0;
            if (System.Math.Abs(x) > 1e-9 || System.Math.Abs(y) > 1e-9 || System.Math.Abs(z) > 1e-9)
                return true;

            if (part.Rotation == null || part.Rotation.Length < 9)
                return false;

            double[] r = part.Rotation;
            return System.Math.Abs(r[0] - 1) > 1e-6 || System.Math.Abs(r[4] - 1) > 1e-6 || System.Math.Abs(r[8] - 1) > 1e-6
                   || System.Math.Abs(r[1]) > 1e-6 || System.Math.Abs(r[2]) > 1e-6 || System.Math.Abs(r[3]) > 1e-6
                   || System.Math.Abs(r[5]) > 1e-6 || System.Math.Abs(r[6]) > 1e-6 || System.Math.Abs(r[7]) > 1e-6;
        }
    }
}

using System;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    public static class CatalogPartService
    {
        public static Guid Insert(
            string? dwgPath,
            ValveProject project,
            CustomPartDefinition part,
            double? dnOverride = null)
        {
            if (!part.IsStandardCatalog)
                throw new InvalidOperationException($"'{part.DisplayName}' is not an insertable catalog part.");

            if (dnOverride is > 0)
            {
                project.Parameters.DN = dnOverride.Value;
            }

            string prefix = ShortPrefix(part.Id);
            var node = new PrimitiveNode
            {
                Name = project.NextName(prefix),
                Kind = SceneNodeKind.Catalog,
                CatalogPartId = part.Id,
            };

            foreach (CatalogPartParam spec in part.CatalogParams)
            {
                double value = ResolveParamValue(spec, part, project, dnOverride);
                node.Parameters[spec.Name] = new ParamValue { Value = value };
            }

            if (part.CatalogFrameRotation is { Length: 9 })
                node.CatalogFrameRotation = part.CatalogFrameRotation.ToArray();

            project.Parts.Add(node);
            DocumentStore.Save(dwgPath, project);
            return node.Id;
        }

        private static double ResolveParamValue(
            CatalogPartParam spec,
            CustomPartDefinition part,
            ValveProject project,
            double? dnOverride)
        {
            if (spec.UseSkeletonDN)
            {
                if (dnOverride is > 0)
                    return dnOverride.Value;
                return project.Parameters.DN > 0 ? project.Parameters.DN : part.DefaultDN;
            }

            if (spec.UseSkeletonDN2 || spec.Name.Equals("DN2", StringComparison.OrdinalIgnoreCase))
            {
                int largeDn = (int)Math.Round(
                    dnOverride is > 0 ? dnOverride.Value
                    : project.Parameters.DN > 0 ? project.Parameters.DN : part.DefaultDN);
                if (project.Parameters.DN2 > 0)
                {
                    return BwFittingSizeCatalog.NormalizeReducerSmallDn(
                        largeDn, (int)Math.Round(project.Parameters.DN2));
                }

                return BwFittingSizeCatalog.DefaultReducerSmallDn(largeDn);
            }

            if (spec.Default > 0 || spec.Name.Equals("CEL", StringComparison.OrdinalIgnoreCase))
                return spec.Default;

            return part.DefaultDN;
        }

        private static string ShortPrefix(string partId)
        {
            int idx = partId.IndexOf('_');
            string head = idx > 0 ? partId[..idx] : partId;
            return head.Length > 4 ? head[..4] + "_" : head + "_";
        }
    }
}

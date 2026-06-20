using System;
using System.Collections.Generic;
using System.Linq;
using Plant3DCatalogComposer.Services;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    public static class PrimitiveService
    {
        public static readonly IReadOnlyList<PrimitiveDefinition> Primitives = PrimitiveCatalog.All;

        public static PrimitiveDefinition? FindDefinition(PrimitiveType type) =>
            Primitives.FirstOrDefault(p => p.Type == type);

        public static Guid Insert(string? dwgPath, ValveProject project, PrimitiveDefinition primitive)
        {
            FittingDimensionService.EnsureRunDimensions(project);

            if (project.Parameters.BodyOD <= 0)
                throw new InvalidOperationException(
                    "Set catalog DN (Catalog → Apply) or run Create Skeleton for valve body dimensions.");

            string name = project.NextName(primitive.Prefix);
            var node = new PrimitiveNode
            {
                Name = name,
                Type = primitive.Type,
            };

            foreach (var (logical, expr, valueMm, _) in primitive.Parameters)
            {
                node.Parameters[logical] = new ParamValue
                {
                    Value = valueMm(project.Parameters),
                    Expression = expr,
                };
            }

            project.Parts.Add(node);
            DocumentStore.Save(dwgPath, project);
            return node.Id;
        }
    }
}

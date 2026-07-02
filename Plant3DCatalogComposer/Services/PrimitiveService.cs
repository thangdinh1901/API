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
            if (project.Parameters.DN <= 0)
                throw new InvalidOperationException(
                    "Set catalog DN (Catalog → Apply) before inserting primitives.");

            string name = project.NextName(primitive.Prefix);
            var node = new PrimitiveNode
            {
                Name = name,
                Type = primitive.Type,
            };

            // Primitive default sizes are seeded from pipe OD. Envelope dims (BodyOD, …) are no
            // longer auto-stored on the project (user declares them by hand), so derive a transient
            // BodyOD from DN just to size the new primitive — without writing it back to the scene.
            SkeletonParameters seed = SeedParametersForPrimitive(project.Parameters);

            foreach (var (logical, _, valueMm, _) in primitive.Parameters)
            {
                node.Parameters[logical] = new ParamValue
                {
                    Value = valueMm(seed),
                };
            }

            project.Parts.Add(node);
            SceneGraphCatalogService.AutoUnionIntoMainBody(project, node.Id);
            DocumentStore.Save(dwgPath, project);
            return node.Id;
        }

        /// <summary>Copy of the project skeleton with BodyOD filled from DN (pipe OD) when the user
        /// has not declared it, so primitive defaults size sensibly. Not persisted to the scene.</summary>
        private static SkeletonParameters SeedParametersForPrimitive(SkeletonParameters source)
        {
            if (source.BodyOD > 0)
                return source;

            return new SkeletonParameters
            {
                DN = source.DN,
                DN2 = source.DN2,
                PressureClass = source.PressureClass,
                PipeSchedule = source.PipeSchedule,
                FaceToFace = source.FaceToFace,
                BodyOD = PipeSizeCatalog.OdSch40Mm(source.DN),
                ElbowCenterToFace = source.ElbowCenterToFace,
                BodyLength = source.BodyLength,
                BonnetHeight = source.BonnetHeight,
                StemDia = source.StemDia,
                HandwheelOD = source.HandwheelOD,
                CustomDimensions = source.CustomDimensions,
            };
        }
    }
}

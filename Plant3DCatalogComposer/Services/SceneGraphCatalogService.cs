using System;
using System.Collections.Generic;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Keep scene graph edits aligned with catalog export / testacpscript geometry.</summary>
    internal static class SceneGraphCatalogService
    {
        /// <summary>Reload the scene JSON saved for <paramref name="dwgPath"/>.</summary>
        public static ValveProject ReloadScene(string dwgPath)
        {
            return DocumentStore.LoadOrCreate(
                dwgPath,
                System.IO.Path.GetFileNameWithoutExtension(dwgPath));
        }

        /// <summary>
        /// Union a newly inserted part into the main catalog body so Deploy / Test export it
        /// (matches what scene_builder preview shows after boolean combine).
        /// </summary>
        public static void AutoUnionIntoMainBody(ValveProject project, Guid newPartId)
        {
            PrimitiveNode? newNode = project.FindNode(newPartId);
            if (project.Parts.Count <= 1 || newNode == null)
                return;

            // Cutters (fillet/chamfer) are subtract tools — auto-unioning them into the body would
            // cancel the intended subtract, so they are never auto-combined.
            if (newNode.Kind == SceneNodeKind.Primitive && PrimitiveCatalog.IsCutterType(newNode.Type))
                return;

            Guid targetId = ResolveUnionTarget(project, newPartId);
            if (targetId == Guid.Empty || targetId == newPartId)
                return;

            if (project.Operations.Any(op =>
                    op.Type == BooleanOpType.UNION
                    && op.Target == targetId
                    && op.Tools.Contains(newPartId)))
            {
                return;
            }

            int order = project.Operations.Count == 0
                ? 1
                : project.Operations.Max(o => o.Order) + 1;

            project.Operations.Add(new BooleanOperation
            {
                Order = order,
                Type = BooleanOpType.UNION,
                Target = targetId,
                Tools = { newPartId },
            });
        }

        private static Guid ResolveUnionTarget(ValveProject project, Guid excludePartId)
        {
            BooleanOperation? lastUnion = project.Operations
                .Where(o => o.Type == BooleanOpType.UNION)
                .OrderByDescending(o => o.Order)
                .FirstOrDefault();

            if (lastUnion != null && project.FindNode(lastUnion.Target) != null)
                return lastUnion.Target;

            PrimitiveNode? first = project.Parts.FirstOrDefault(p => p.Id != excludePartId);
            return first?.Id ?? Guid.Empty;
        }

        /// <summary>Union scene parts that are not yet in any boolean op (e.g. added after last export).</summary>
        public static void EnsureUnreferencedPartsUnioned(ValveProject project)
        {
            if (project.Parts.Count <= 1)
                return;

            var referenced = new HashSet<Guid>();
            foreach (BooleanOperation op in project.Operations)
            {
                referenced.Add(op.Target);
                foreach (Guid toolId in op.Tools)
                    referenced.Add(toolId);
            }

            Guid targetId = ResolveUnionTarget(project, Guid.Empty);
            if (targetId == Guid.Empty)
                return;

            foreach (PrimitiveNode part in project.Parts)
            {
                if (part.Id == targetId || referenced.Contains(part.Id))
                    continue;

                AutoUnionIntoMainBody(project, part.Id);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Plant3DSkeletonManager.Core
{
    public static class BooleanGraph
    {
        public static BooleanOperation AddOperation(
            ValveProject project,
            BooleanOpType type,
            Guid targetId,
            IEnumerable<Guid> toolIds)
        {
            var tools = toolIds.Distinct().Where(t => t != targetId).ToList();
            if (tools.Count == 0)
                throw new ArgumentException("At least one tool node is required (distinct from target).");

            if (project.FindNode(targetId) == null)
                throw new ArgumentException("Target node not found.");

            foreach (Guid id in tools)
            {
                if (project.FindNode(id) == null)
                    throw new ArgumentException($"Tool node {id} not found.");
            }

            var op = new BooleanOperation
            {
                Order = project.NextOperationOrder(),
                Type = type,
                Target = targetId,
                Tools = tools,
            };
            project.Operations.Add(op);
            return op;
        }

        public static void RemoveOperation(ValveProject project, int order)
        {
            project.Operations.RemoveAll(o => o.Order == order);
            Renumber(project);
        }

        public static int NextOperationOrder(this ValveProject project) =>
            project.Operations.Count == 0 ? 1 : project.Operations.Max(o => o.Order) + 1;

        private static void Renumber(ValveProject project)
        {
            int n = 1;
            foreach (BooleanOperation op in project.Operations.OrderBy(o => o.Order))
                op.Order = n++;
        }
    }
}

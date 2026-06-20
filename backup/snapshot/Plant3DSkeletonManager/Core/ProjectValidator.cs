using System;
using System.Collections.Generic;
using System.Linq;

namespace Plant3DSkeletonManager.Core
{
    public sealed class ValidationIssue
    {
        public required string Message { get; init; }
        public bool IsError { get; init; }
    }

    public sealed class ValidationResult
    {
        public List<ValidationIssue> Issues { get; } = new();

        public bool IsValid => !Issues.Any(i => i.IsError);

        public IEnumerable<string> Errors =>
            Issues.Where(i => i.IsError).Select(i => i.Message);

        public IEnumerable<string> Warnings =>
            Issues.Where(i => !i.IsError).Select(i => i.Message);

        public void AddError(string message) =>
            Issues.Add(new ValidationIssue { Message = message, IsError = true });

        public void AddWarning(string message) =>
            Issues.Add(new ValidationIssue { Message = message, IsError = false });
    }

    /// <summary>Pre-export validation rules from architecture v1.</summary>
    public static class ProjectValidator
    {
        public static ValidationResult Validate(ValveProject project)
        {
            var result = new ValidationResult();
            if (project.Parts.Count == 0)
                result.AddWarning("Scene graph has no primitives.");

            var ids = new HashSet<Guid>(project.Parts.Select(p => p.Id));

            foreach (PrimitiveNode node in project.Parts)
            {
                if (node.Parent.HasValue && !ids.Contains(node.Parent.Value))
                    result.AddError($"Node '{node.Name}' references missing parent.");

                if (node.Kind == SceneNodeKind.Catalog)
                {
                    if (string.IsNullOrWhiteSpace(node.CatalogPartId))
                        result.AddError($"Node '{node.Name}' is a catalog part but has no catalogPartId.");
                    else if (!node.Parameters.ContainsKey("DN") || node.Parameters["DN"].Value <= 0)
                        result.AddError($"Node '{node.Name}': catalog DN must be > 0.");
                    continue;
                }

                foreach (KeyValuePair<string, ParamValue> kv in node.Parameters)
                {
                    if (kv.Value.Value <= 0)
                        result.AddError($"Node '{node.Name}': parameter '{kv.Key}' must be > 0 mm.");

                    if (!string.IsNullOrWhiteSpace(kv.Value.Expression) &&
                        !ExpressionEvaluator.TryEvaluate(kv.Value.Expression, project.Parameters, out _))
                    {
                        result.AddWarning(
                            $"Node '{node.Name}': expression '{kv.Value.Expression}' for '{kv.Key}' could not be resolved.");
                    }
                }
            }

            var referenced = new HashSet<Guid>();
            foreach (BooleanOperation op in project.Operations.OrderBy(o => o.Order))
            {
                if (!ids.Contains(op.Target))
                {
                    result.AddError($"Boolean op #{op.Order}: target node not found.");
                    continue;
                }

                if (op.Tools.Count == 0)
                    result.AddError($"Boolean op #{op.Order}: no tool nodes specified.");

                referenced.Add(op.Target);
                foreach (Guid toolId in op.Tools)
                {
                    if (!ids.Contains(toolId))
                        result.AddError($"Boolean op #{op.Order}: tool node {toolId} not found.");
                    else if (toolId == op.Target)
                        result.AddError($"Boolean op #{op.Order}: target cannot be its own tool.");
                    else
                        referenced.Add(toolId);
                }
            }

            if (project.Operations.Count > 0)
            {
                foreach (PrimitiveNode node in project.Parts)
                {
                    if (!referenced.Contains(node.Id))
                    {
                        result.AddWarning(
                            $"Primitive '{node.Name}' is not referenced by any boolean operation.");
                    }
                }
            }

            foreach (ConnectionPort port in project.Ports)
            {
                if (port.ParentNodeId.HasValue && !ids.Contains(port.ParentNodeId.Value))
                    result.AddError($"{PortConnectionTypeHelper.PortLabel(port)} references missing parent node.");

                if (port.Number <= 0)
                    result.AddError($"{PortConnectionTypeHelper.PortLabel(port)}: number must be > 0.");

                double dirLen = Math.Sqrt(
                    port.Direction[0] * port.Direction[0] +
                    port.Direction[1] * port.Direction[1] +
                    port.Direction[2] * port.Direction[2]);
                if (dirLen < 1e-9)
                    result.AddError($"{PortConnectionTypeHelper.PortLabel(port)}: direction vector must be non-zero.");
            }

            var portNumbers = project.Ports.GroupBy(p => p.Number).Where(g => g.Count() > 1).ToList();
            if (portNumbers.Count > 0)
                result.AddWarning("Duplicate port numbers detected.");

            return result;
        }
    }
}

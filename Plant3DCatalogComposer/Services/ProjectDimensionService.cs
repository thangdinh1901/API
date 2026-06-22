using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal static class ProjectDimensionService
    {
        public static readonly string[] BuiltInNames =
        {
            "FaceToFace", "BodyOD", "ElbowCenterToFace",
            "BodyLength", "BonnetHeight", "StemDia", "HandwheelOD",
        };

        private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "DN", "DN2", "PressureClass", "PipeSchedule", "Units",
        };

        private static readonly Regex ValidName = new(
            @"^[A-Za-z][A-Za-z0-9_]*$",
            RegexOptions.CultureInvariant);

        public static IReadOnlyList<(string Name, double ValueMm)> LoadRows(SkeletonParameters parameters)
        {
            var rows = new List<(string, double)>();

            foreach (string name in BuiltInNames)
            {
                double value = GetBuiltIn(parameters, name);
                if (value > 0)
                    rows.Add((name, value));
            }

            foreach (KeyValuePair<string, double> pair in parameters.CustomDimensions
                         .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (IsBuiltIn(pair.Key) || pair.Value <= 0)
                    continue;
                rows.Add((pair.Key, pair.Value));
            }

            return rows;
        }

        public static IReadOnlyList<string> LoadRowNames(ValveProject project)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach ((string name, _) in LoadRows(project.Parameters))
                names.Add(name);
            foreach (string key in project.DimensionBindings.Keys)
                names.Add(key);
            return names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static double GetValue(ValveProject project, string name)
        {
            if (IsBuiltIn(name))
            {
                double v = GetBuiltIn(project.Parameters, name);
                if (v > 0)
                    return v;
            }

            if (project.Parameters.CustomDimensions.TryGetValue(name, out double custom))
                return custom;

            return 0;
        }

        public static void ApplyToProject(
            ValveProject project,
            IReadOnlyList<(string Name, double ValueMm)> rows,
            IReadOnlyDictionary<string, DimensionBinding>? bindings = null)
        {
            ApplyRows(project.Parameters, rows);

            var keep = new HashSet<string>(
                rows.Select(r => r.Name),
                StringComparer.OrdinalIgnoreCase);
            foreach (string key in project.DimensionBindings.Keys.ToList())
            {
                if (!keep.Contains(key))
                    project.DimensionBindings.Remove(key);
            }

            if (bindings == null)
                return;

            foreach (KeyValuePair<string, DimensionBinding> pair in bindings)
            {
                if (!keep.Contains(pair.Key))
                    continue;
                project.DimensionBindings[pair.Key] = pair.Value;
            }
        }

        public static void ApplyRows(SkeletonParameters parameters, IReadOnlyList<(string Name, double ValueMm)> rows)
        {
            ClearBuiltIn(parameters);
            parameters.CustomDimensions.Clear();

            foreach ((string name, double value) in rows)
            {
                if (IsBuiltIn(name))
                    SetBuiltIn(parameters, name, value);
                else
                    parameters.CustomDimensions[name] = value;
            }
        }

        public static bool TryValidateRow(string name, string valueText, out string? error, out string normalizedName, out double valueMm)
        {
            normalizedName = name.Trim();
            valueMm = 0;
            error = null;

            if (normalizedName.Length == 0)
            {
                error = "Dimension name is required.";
                return false;
            }

            if (ReservedNames.Contains(normalizedName))
            {
                error = $"'{normalizedName}' is set on the Catalog tab (DN, class, schedule).";
                return false;
            }

            if (!ValidName.IsMatch(normalizedName))
            {
                error = "Name must start with a letter and use only letters, digits, or underscore.";
                return false;
            }

            if (!TryParsePositive(valueText, out valueMm))
            {
                error = "Enter a positive value in mm.";
                return false;
            }

            return true;
        }

        public static bool IsBuiltIn(string name) =>
            BuiltInNames.Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase));

        public static string FormatBinding(DimensionBinding? binding)
        {
            if (binding == null)
                return "—";

            if (binding.FromPort.HasValue && binding.ToPort.HasValue)
                return $"P{binding.FromPort}↔P{binding.ToPort}";

            if (!string.IsNullOrWhiteSpace(binding.SceneNodeName))
            {
                return string.IsNullOrWhiteSpace(binding.ParamKey)
                    ? binding.SceneNodeName
                    : $"{binding.SceneNodeName}.{binding.ParamKey}";
            }

            return binding.MeasureKind switch
            {
                "pickDeltaX" => "pick ΔX",
                "pickDeltaY" => "pick ΔY",
                "pickDeltaZ" => "pick ΔZ",
                "pickDistance" => "pick |Δ|",
                "portDistance" => "ports",
                "sceneBind" => "scene",
                "manual" => "manual",
                _ => binding.MeasureKind,
            };
        }

        public static string ScanUsedIn(ValveProject project, string dimensionName)
        {
            if (string.IsNullOrWhiteSpace(dimensionName))
                return "";

            var hits = new List<string>();
            foreach (PrimitiveNode node in project.Parts)
            {
                foreach (KeyValuePair<string, ParamValue> pair in node.Parameters)
                {
                    if (ExpressionReferencesName(pair.Value.Expression, dimensionName))
                        hits.Add($"{node.Name}.{pair.Key}");
                }
            }

            return hits.Count == 0 ? "—" : string.Join(", ", hits);
        }

        public static void RefreshAllUsedIn(ValveProject project, DataGridView grid, int usedInColumnIndex)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow)
                    continue;

                string? name = row.Cells[0].Value?.ToString()?.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                row.Cells[usedInColumnIndex].Value = ScanUsedIn(project, name);
            }
        }

        private static bool ExpressionReferencesName(string? expression, string dimensionName)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            return Regex.IsMatch(
                expression,
                $@"\b{Regex.Escape(dimensionName)}\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static void ClearBuiltIn(SkeletonParameters p)
        {
            p.FaceToFace = 0;
            p.BodyOD = 0;
            p.ElbowCenterToFace = 0;
            p.BodyLength = 0;
            p.BonnetHeight = 0;
            p.StemDia = 0;
            p.HandwheelOD = 0;
        }

        private static double GetBuiltIn(SkeletonParameters p, string name) => name switch
        {
            "FaceToFace" => p.FaceToFace,
            "BodyOD" => p.BodyOD,
            "ElbowCenterToFace" => p.ElbowCenterToFace,
            "BodyLength" => p.BodyLength,
            "BonnetHeight" => p.BonnetHeight,
            "StemDia" => p.StemDia,
            "HandwheelOD" => p.HandwheelOD,
            _ => 0,
        };

        private static void SetBuiltIn(SkeletonParameters p, string name, double value)
        {
            switch (name.ToUpperInvariant())
            {
                case "FACETOFACE": p.FaceToFace = value; break;
                case "BODYOD": p.BodyOD = value; break;
                case "ELBOWCENTERTOFACE": p.ElbowCenterToFace = value; break;
                case "BODYLENGTH": p.BodyLength = value; break;
                case "BONNETHEIGHT": p.BonnetHeight = value; break;
                case "STEMDIA": p.StemDia = value; break;
                case "HANDWHEELOD": p.HandwheelOD = value; break;
                default:
                    p.CustomDimensions[name] = value;
                    break;
            }
        }

        private static bool TryParsePositive(string text, out double value)
        {
            value = 0;
            if (!double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && !double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                return false;
            }

            return value > 0;
        }

        public static void ResolveAllPrimitiveExpressions(ValveProject project)
        {
            foreach (PrimitiveNode node in project.Parts)
            {
                if (node.Kind != SceneNodeKind.Primitive)
                    continue;

                SceneGraphEditor.ResolveExpressions(node, project.Parameters);
            }
        }
    }
}

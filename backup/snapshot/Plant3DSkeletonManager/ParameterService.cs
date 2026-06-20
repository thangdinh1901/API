using System;
using System.Collections.Generic;
using Inventor;
using Plant3DSkeletonManager.Adapter;
using Plant3DSkeletonManager.Core;

namespace Plant3DSkeletonManager
{
    /// <summary>
    /// Creates/updates the skeleton user parameters in the active assembly
    /// document AND stores them in the scene graph (the source of truth).
    /// No geometry is created.
    /// </summary>
    public static class ParameterService
    {
        public static readonly string[] DimensionNames =
            { "FaceToFace", "BodyOD", "BodyLength", "BonnetHeight", "StemDia", "HandwheelOD" };

        /// <summary>
        /// Initial sizing factors (multiples of DN) per valve type, order matching DimensionNames.
        /// Placeholder estimates meant to be replaced by project standards.
        /// </summary>
        private static readonly Dictionary<string, double[]> ValveFactors =
            new(StringComparer.OrdinalIgnoreCase)
            {
                //                     F2F  BodyOD BodyLen Bonnet Stem Handwheel
                ["Gate Valve"] = new[] { 3.6, 1.8, 2.0, 3.0, 0.40, 1.6 },
                ["Globe Valve"] = new[] { 4.0, 1.9, 2.6, 2.4, 0.40, 1.6 },
                ["Ball Valve"] = new[] { 1.6, 1.6, 1.5, 1.2, 0.35, 1.4 },
                ["Butterfly Valve"] = new[] { 0.9, 1.4, 0.6, 1.5, 0.30, 1.2 },
                ["Check Valve"] = new[] { 3.4, 1.7, 2.2, 1.0, 0.30, 1.0 },
            };

        public static IReadOnlyCollection<string> ValveTypes => ValveFactors.Keys;

        /// <summary>Suggested dimensions (mm) in DimensionNames order, to pre-fill the editable table.</summary>
        public static double[] SuggestDimensions(string valveType, double dn)
        {
            if (!ValveFactors.TryGetValue(valveType, out double[]? f))
                throw new ArgumentException($"Unknown valve type: {valveType}");

            var result = new double[f.Length];
            for (int i = 0; i < f.Length; i++)
                result[i] = dn * f[i];
            return result;
        }

        public static void CreateSkeletonParameters(
            Inventor.Application app, string valveType, SkeletonParameters data)
        {
            if (app.ActiveDocument is not AssemblyDocument asmDoc)
                throw new InvalidOperationException(
                    "Please open or activate an assembly document (.iam) first.");

            UserParameters userParams = asmDoc.ComponentDefinition.Parameters.UserParameters;

            Transaction tx = app.TransactionManager.StartTransaction(
                (_Document)asmDoc, "Create Skeleton Parameters");
            try
            {
                SetNumeric(userParams, "DN", data.DN, "Nominal diameter");
                SetText(userParams, "PressureClass", data.PressureClass, "Pressure class / rating");
                SetNumeric(userParams, "FaceToFace", data.FaceToFace, $"Face-to-face dimension ({valveType})");
                SetNumeric(userParams, "BodyOD", data.BodyOD, $"Body outer diameter ({valveType})");
                SetNumeric(userParams, "BodyLength", data.BodyLength, $"Body length ({valveType})");
                SetNumeric(userParams, "BonnetHeight", data.BonnetHeight, $"Bonnet height ({valveType})");
                SetNumeric(userParams, "StemDia", data.StemDia, $"Stem diameter ({valveType})");
                SetNumeric(userParams, "HandwheelOD", data.HandwheelOD, $"Handwheel outer diameter ({valveType})");

                // Scene graph is the master copy
                ValveProject project = DocumentStore.LoadOrCreate(asmDoc);
                project.Parameters = data;
                project.ValveName =
                    $"{valveType.Replace(" ", "")}_DN{data.DN:0.###}_{data.PressureClass}";
                DocumentStore.Save(asmDoc, project);

                tx.End();
            }
            catch
            {
                tx.Abort();
                throw;
            }

            asmDoc.Update();
        }

        private static void SetNumeric(UserParameters userParams, string name, double mm, string comment)
        {
            string expression = TransformConverter.MmExpression(mm);

            UserParameter? existing = TryGet(userParams, name);
            if (existing != null)
            {
                existing.Expression = expression;
                existing.Comment = comment;
            }
            else
            {
                UserParameter p = userParams.AddByExpression(
                    name, expression, UnitsTypeEnum.kMillimeterLengthUnits);
                p.Comment = comment;
            }
        }

        private static void SetText(UserParameters userParams, string name, string value, string comment)
        {
            UserParameter? existing = TryGet(userParams, name);
            if (existing != null)
            {
                existing.Value = value;
                existing.Comment = comment;
            }
            else
            {
                UserParameter p = userParams.AddByValue(name, value, UnitsTypeEnum.kTextUnits);
                p.Comment = comment;
            }
        }

        private static UserParameter? TryGet(UserParameters userParams, string name)
        {
            foreach (UserParameter p in userParams)
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }
    }
}

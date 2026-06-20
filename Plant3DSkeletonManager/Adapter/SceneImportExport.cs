using System;
using System.IO;
using Inventor;
using Plant3DSkeletonManager.Core;

namespace Plant3DSkeletonManager.Adapter
{
    public static class SceneImportExport
    {
        public static ValveProject LoadJsonFile(string path)
        {
            string json = System.IO.File.ReadAllText(path);
            ValveProject? project = JsonCodec.Deserialize(json)
                ?? throw new InvalidOperationException("Invalid scene JSON file.");
            project.PruneOperations();
            return project;
        }

        public static void SaveJsonFile(string path, ValveProject project) =>
            System.IO.File.WriteAllText(path, JsonCodec.Serialize(project));

        /// <summary>Replaces the in-document scene graph with imported JSON (no geometry yet).</summary>
        public static void ReplaceProject(AssemblyDocument asmDoc, ValveProject project)
        {
            DocumentStore.Save(asmDoc, project);
        }

        public sealed record RebuildResult(int Removed, int Inserted);

        /// <summary>
        /// Round-trip rebuild: delete all tagged occurrences, recreate from scene graph,
        /// re-apply boolean appearance cues.
        /// </summary>
        public static RebuildResult RebuildFromProject(
            Inventor.Application app, AssemblyDocument asmDoc, ValveProject project)
        {
            if (string.IsNullOrEmpty(asmDoc.FullFileName))
                throw new InvalidOperationException("Please save the assembly first.");

            ApplySkeletonParameters(asmDoc, project.Parameters);

            Transaction tx = app.TransactionManager.StartTransaction(
                (_Document)asmDoc, "Rebuild Scene from Graph");
            int removed;
            int inserted;
            try
            {
                removed = PrimitiveService.ClearAllTaggedOccurrences(asmDoc);

                inserted = 0;
                foreach (PrimitiveNode node in project.Parts)
                {
                    PrimitiveService.InsertFromNode(app, asmDoc, project, node);
                    inserted++;
                }

                DocumentStore.Save(asmDoc, project);
                BooleanAppearanceService.ApplyAll(asmDoc, project);
                tx.End();
            }
            catch
            {
                tx.Abort();
                throw;
            }

            asmDoc.Update();
            return new RebuildResult(removed, inserted);
        }

        private static void ApplySkeletonParameters(AssemblyDocument asmDoc, SkeletonParameters p)
        {
            UserParameters up = asmDoc.ComponentDefinition.Parameters.UserParameters;
            SetNumeric(up, "DN", p.DN);
            SetNumeric(up, "FaceToFace", p.FaceToFace);
            SetNumeric(up, "BodyOD", p.BodyOD);
            SetNumeric(up, "BodyLength", p.BodyLength);
            SetNumeric(up, "BonnetHeight", p.BonnetHeight);
            SetNumeric(up, "StemDia", p.StemDia);
            SetNumeric(up, "HandwheelOD", p.HandwheelOD);

            UserParameter? pc = TryGet(up, "PressureClass");
            if (pc != null)
                pc.Value = p.PressureClass;
            else
                up.AddByValue("PressureClass", p.PressureClass, UnitsTypeEnum.kTextUnits);
        }

        private static void SetNumeric(UserParameters up, string name, double mm)
        {
            string expr = TransformConverter.MmExpression(mm);
            UserParameter? existing = TryGet(up, name);
            if (existing != null)
                existing.Expression = expr;
            else
                up.AddByExpression(name, expr, UnitsTypeEnum.kMillimeterLengthUnits);
        }

        private static UserParameter? TryGet(UserParameters up, string name)
        {
            foreach (UserParameter p in up)
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }
    }
}

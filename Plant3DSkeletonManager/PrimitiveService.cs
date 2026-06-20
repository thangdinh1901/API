using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Inventor;
using Plant3DSkeletonManager.Adapter;
using Plant3DSkeletonManager.Core;

namespace Plant3DSkeletonManager
{
    /// <summary>
    /// One catalog primitive: template geometry, occurrence naming and the
    /// mapping between logical (Plant 3D) parameter names and Inventor part
    /// parameter names (some logical names collide with Inventor unit symbols).
    /// </summary>
    public sealed class PrimitiveDefinition
    {
        public required PrimitiveType Type { get; init; }
        public required string DisplayName { get; init; }
        public required string Prefix { get; init; }
        public required string TemplateFile { get; init; }

        /// <summary>
        /// Logical = JSON/Plant3D name. InventorName = part parameter name.
        /// Unit drives push/sync formatting (mm, deg, unitless).
        /// Preview-only Inventor params (V_*) are defined in PreviewGeometry and are NOT listed here.
        /// </summary>
        public required (string Logical, string InventorName, string Expression, Func<SkeletonParameters, double> ValueMm, Core.CatalogParamUnit Unit)[] Parameters { get; init; }

        public required Action<Inventor.Application, PartComponentDefinition> BuildGeometry { get; init; }
    }

    /// <summary>
    /// Inserts catalog primitives. Each insert copies the template to a
    /// per-instance part file (independent dimensions), tags the occurrence
    /// with the node GUID and registers the node in the scene graph.
    /// </summary>
    public static class PrimitiveService
    {
        public static readonly IReadOnlyList<PrimitiveDefinition> Primitives = PrimitiveCatalog.All;

        public static PrimitiveDefinition? FindDefinition(PrimitiveType type) =>
            Primitives.FirstOrDefault(p => p.Type == type);

        public static Guid Insert(Inventor.Application app, PrimitiveDefinition primitive)
        {
            if (app.ActiveDocument is not AssemblyDocument asmDoc)
                throw new InvalidOperationException(
                    "Please open or activate an assembly document (.iam) first.");

            if (string.IsNullOrEmpty(asmDoc.FullFileName))
                throw new InvalidOperationException(
                    "Please save the assembly first (per-instance part files are stored next to it).");

            ValveProject project = DocumentStore.LoadOrCreate(asmDoc);
            if (project.Parameters.BodyOD <= 0)
                throw new InvalidOperationException(
                    "Skeleton parameters not found. Run 'Create Skeleton' first.");

            string asmDir = System.IO.Path.GetDirectoryName(asmDoc.FullFileName)!;
            string templatePath = EnsureTemplate(app, primitive, asmDir);

            string name = project.NextName(primitive.Prefix);
            string instancePath = CreateInstanceFile(templatePath, asmDir, name);

            var node = new PrimitiveNode
            {
                Name = name,
                Type = primitive.Type,
            };
            foreach (var (logical, _, expr, valueMm, _) in primitive.Parameters)
            {
                node.Parameters[logical] = new ParamValue
                {
                    Value = valueMm(project.Parameters),
                    Expression = expr,
                };
            }

            Transaction tx = app.TransactionManager.StartTransaction(
                (_Document)asmDoc, $"Insert {primitive.DisplayName}");
            try
            {
                Matrix transform = app.TransientGeometry.CreateMatrix();
                ComponentOccurrence occ = asmDoc.ComponentDefinition.Occurrences.Add(
                    instancePath, transform);

                occ.Name = name;

                PartComponentDefinition partDef = (PartComponentDefinition)occ.Definition;
                foreach (var (logical, invName, _, _, unit) in primitive.Parameters)
                {
                    if (!TrySetPartParameter(partDef, invName, node.Parameters[logical].Value, unit))
                    {
                        throw new InvalidOperationException(
                            $"Template for {primitive.DisplayName} is missing parameter '{invName}'. " +
                            "Reload the add-in with -PurgeTemplates.");
                    }
                }

                OccurrenceTagger.Tag(occ, node.Id, primitive.Type);

                project.Parts.Add(node);
                DocumentStore.Save(asmDoc, project);

                tx.End();
            }
            catch
            {
                tx.Abort();
                throw;
            }

            asmDoc.Update();
            return node.Id;
        }

        /// <summary>
        /// Creates one Inventor occurrence from an existing scene graph node (used by rebuild/import).
        /// The node must already exist in project.Parts with a stable Id.
        /// </summary>
        public static void InsertFromNode(
            Inventor.Application app,
            AssemblyDocument asmDoc,
            ValveProject project,
            PrimitiveNode node)
        {
            PrimitiveDefinition? primitive = FindDefinition(node.Type)
                ?? throw new InvalidOperationException($"Unsupported primitive type: {node.Type}.");

            if (string.IsNullOrEmpty(asmDoc.FullFileName))
                throw new InvalidOperationException("Please save the assembly first.");

            string asmDir = System.IO.Path.GetDirectoryName(asmDoc.FullFileName)!;
            string templatePath = EnsureTemplate(app, primitive, asmDir);
            string instancePath = CreateInstanceFile(templatePath, asmDir, node.Name);

            Matrix transform = TransformConverter.CreateMatrix(app.TransientGeometry, node);
            ComponentOccurrence occ = asmDoc.ComponentDefinition.Occurrences.Add(
                instancePath, transform);
            occ.Name = node.Name;

            PushService.PushDimensionsOnly(occ, node);
            OccurrenceTagger.Tag(occ, node.Id, node.Type);
        }

        /// <summary>Removes every tagged P3D occurrence from the assembly (geometry only).</summary>
        public static int ClearAllTaggedOccurrences(AssemblyDocument asmDoc)
        {
            var toDelete = new List<ComponentOccurrence>();
            foreach (ComponentOccurrence occ in asmDoc.ComponentDefinition.Occurrences)
            {
                if (OccurrenceTagger.GetNodeId(occ) != null)
                    toDelete.Add(occ);
            }

            foreach (ComponentOccurrence occ in toDelete)
                occ.Delete();

            return toDelete.Count;
        }

        private const int TemplateBuildVersion = 4;

        /// <summary>Read-only template library lives in the add-in Templates folder; generated on demand.</summary>
        private static string EnsureTemplate(Inventor.Application app, PrimitiveDefinition primitive, string asmDir)
        {
            string templateDir = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(typeof(PrimitiveService).Assembly.Location)!,
                "Templates");
            Directory.CreateDirectory(templateDir);

            string path = System.IO.Path.Combine(templateDir, primitive.TemplateFile);
            string versionPath = path + ".version";
            if (System.IO.File.Exists(path) &&
                System.IO.File.Exists(versionPath) &&
                System.IO.File.ReadAllText(versionPath).Trim() ==
                TemplateBuildVersion.ToString(System.Globalization.CultureInfo.InvariantCulture))
            {
                return path;
            }

            PartDocument partDoc = (PartDocument)app.Documents.Add(
                DocumentTypeEnum.kPartDocumentObject,
                app.FileManager.GetTemplateFile(DocumentTypeEnum.kPartDocumentObject),
                false);
            try
            {
                primitive.BuildGeometry(app, partDoc.ComponentDefinition);
                partDoc.Update();
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
                partDoc.SaveAs(path, false);
                System.IO.File.WriteAllText(
                    versionPath,
                    TemplateBuildVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            finally
            {
                partDoc.Close(true);
            }
            return path;
        }

        /// <summary>Copies the template to a per-instance part file so each primitive is independently dimensioned.</summary>
        private static string CreateInstanceFile(string templatePath, string asmDir, string name)
        {
            string dir = System.IO.Path.Combine(asmDir, "Primitives");
            Directory.CreateDirectory(dir);

            string path = System.IO.Path.Combine(dir, name + ".ipt");
            int suffix = 1;
            while (System.IO.File.Exists(path))
                path = System.IO.Path.Combine(dir, $"{name}_{suffix++}.ipt");

            System.IO.File.Copy(templatePath, path);
            return path;
        }

        private static bool TrySetPartParameter(
            PartComponentDefinition partDef,
            string invName,
            double value,
            Core.CatalogParamUnit unit)
        {
            try
            {
                partDef.Parameters[invName].Expression =
                    TransformConverter.ParamExpression(value, unit);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

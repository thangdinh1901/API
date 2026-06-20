using System;
using System.Collections.Generic;
using Inventor;
using Plant3DSkeletonManager.Core;

namespace Plant3DSkeletonManager.Adapter
{
    /// <summary>Scene graph editing operations that keep Inventor occurrences in sync.</summary>
    public static class SceneEditorService
    {
        public static void Delete(
            Inventor.Application app, AssemblyDocument asmDoc, ValveProject project, Guid nodeId)
        {
            HashSet<Guid> toRemove = SceneGraphHelpers.CollectSubtree(project, nodeId);

            Transaction tx = app.TransactionManager.StartTransaction(
                (_Document)asmDoc, "Delete Primitive");
            try
            {
                foreach (Guid id in toRemove)
                {
                    ComponentOccurrence? occ = OccurrenceLookup.FindByNodeId(asmDoc, id);
                    occ?.Delete();
                }

                project.Parts.RemoveAll(n => toRemove.Contains(n.Id));
                project.PruneOperations();
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

        public static Guid Duplicate(
            Inventor.Application app, AssemblyDocument asmDoc, ValveProject project, Guid nodeId)
        {
            if (string.IsNullOrEmpty(asmDoc.FullFileName))
                throw new InvalidOperationException("Please save the assembly first.");

            PrimitiveNode? source = project.FindNode(nodeId);
            if (source == null)
                throw new InvalidOperationException("Node not found.");

            ComponentOccurrence? sourceOcc = OccurrenceLookup.FindByNodeId(asmDoc, nodeId);
            if (sourceOcc == null)
                throw new InvalidOperationException("Source occurrence not found in Inventor.");

            // Pull latest transform/dimensions from Inventor before cloning
            TransformConverter.ReadTransform(sourceOcc.Transformation, source);
            SyncService.ReadNodeDimensions(sourceOcc, source);

            string prefix = SceneGraphHelpers.PrefixFromName(source.Name);
            string newName = project.NextName(prefix);
            PrimitiveNode clone = SceneGraphHelpers.CloneNode(source, newName);

            string sourcePath = sourceOcc.ReferencedDocumentDescriptor.FullDocumentName;
            string asmDir = System.IO.Path.GetDirectoryName(asmDoc.FullFileName)!;
            string instancePath = CopyInstanceFile(sourcePath, asmDir, newName);

            Transaction tx = app.TransactionManager.StartTransaction(
                (_Document)asmDoc, $"Duplicate {source.Name}");
            try
            {
                Matrix transform = TransformConverter.CreateMatrix(app.TransientGeometry, clone);
                ComponentOccurrence occ = asmDoc.ComponentDefinition.Occurrences.Add(
                    instancePath, transform);
                occ.Name = newName;

                PushService.PushDimensionsOnly(occ, clone);
                OccurrenceTagger.Tag(occ, clone.Id, clone.Type);

                project.Parts.Add(clone);
                DocumentStore.Save(asmDoc, project);
                tx.End();
            }
            catch
            {
                tx.Abort();
                throw;
            }

            asmDoc.Update();
            return clone.Id;
        }

        public static void Rename(
            AssemblyDocument asmDoc, ValveProject project, Guid nodeId, string newName)
        {
            newName = newName.Trim();
            if (newName.Length == 0)
                throw new ArgumentException("Name cannot be empty.");

            PrimitiveNode? node = project.FindNode(nodeId)
                ?? throw new InvalidOperationException("Node not found.");

            node.Name = newName;

            ComponentOccurrence? occ = OccurrenceLookup.FindByNodeId(asmDoc, nodeId);
            if (occ != null)
                occ.Name = newName;

            DocumentStore.Save(asmDoc, project);
        }

        public static void Reparent(ValveProject project, Guid nodeId, Guid? newParentId)
        {
            if (!SceneGraphHelpers.CanReparent(project, nodeId, newParentId))
                throw new InvalidOperationException("Cannot reparent: would create a cycle.");

            PrimitiveNode? node = project.FindNode(nodeId)
                ?? throw new InvalidOperationException("Node not found.");

            if (newParentId.HasValue && project.FindNode(newParentId.Value) == null)
                throw new InvalidOperationException("Parent node not found.");

            node.Parent = newParentId;
        }

        public static void SelectInInventor(AssemblyDocument asmDoc, Guid nodeId)
        {
            ComponentOccurrence? occ = OccurrenceLookup.FindByNodeId(asmDoc, nodeId);
            if (occ == null)
                return;

            SelectSet sel = asmDoc.SelectSet;
            sel.Clear();
            sel.Select(occ);
        }

        private static string CopyInstanceFile(string sourcePath, string asmDir, string name)
        {
            string dir = System.IO.Path.Combine(asmDir, "Primitives");
            System.IO.Directory.CreateDirectory(dir);

            string path = System.IO.Path.Combine(dir, name + ".ipt");
            int suffix = 1;
            while (System.IO.File.Exists(path))
                path = System.IO.Path.Combine(dir, $"{name}_{suffix++}.ipt");

            System.IO.File.Copy(sourcePath, path);
            return path;
        }
    }
}

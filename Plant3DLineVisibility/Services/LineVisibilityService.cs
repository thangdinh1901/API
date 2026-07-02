using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.ProcessPower.PlantInstance;
using Autodesk.ProcessPower.DataLinks;
using Plant3DLineVisibility.Models;

namespace Plant3DLineVisibility.Services
{
    /// <summary>
    /// Core service for scanning Plant 3D piping objects and controlling
    /// their visibility based on Line Number assignment.
    /// </summary>
    public static class LineVisibilityService
    {
        /// <summary>
        /// Scans the active drawing for all Plant 3D piping objects,
        /// groups them by Line Number, and returns the result.
        /// </summary>
        public static List<LineGroupInfo> ScanDrawing(Document doc)
        {
            var groups = new Dictionary<string, LineGroupInfo>(StringComparer.OrdinalIgnoreCase);

            using (DocumentLock dl = doc.LockDocument())
            {
                Database db = doc.Database;

                DataLinksManager? dlm = null;
                try
                {
                    PlantProject proj = PlantApplication.CurrentProject;
                    if (proj == null)
                        return new List<LineGroupInfo>();

                    var pipingPart = proj.ProjectParts["Piping"];
                    if (pipingPart == null)
                        return new List<LineGroupInfo>();

                    dlm = pipingPart.DataLinksManager;
                }
                catch
                {
                    // Not a Plant 3D project or piping project unavailable
                    return new List<LineGroupInfo>();
                }

                if (dlm == null)
                    return new List<LineGroupInfo>();

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(
                        bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                    foreach (ObjectId id in ms)
                    {
                        if (id.IsErased) continue;

                        try
                        {
                            Entity? ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                            if (ent == null) continue;

                            // Check if this entity is a Plant 3D object
                            int rowId = dlm.FindAcPpRowId(id);
                            if (rowId <= 0) continue;

                            string lineTag = "(Unassigned)";
                            int lgRowId = -1;

                            try
                            {
                                // Navigate: Part → LineGroup via relationship
                                var lgIds = dlm.GetRelatedRowIds(
                                    "P3dLineGroupPartRelationship",
                                    "Part",
                                    rowId,
                                    "LineGroup");

                                if (lgIds != null && lgIds.Count > 0)
                                {
                                    // PnPRowIdArray[0] returns the first related Line Group row ID
                                    lgRowId = lgIds[0];

                                    // Read all properties of the Line Group to find LineNumberTag
                                    var props = dlm.GetAllProperties(lgRowId, false);
                                    if (props != null)
                                    {
                                        foreach (var kv in props)
                                        {
                                            if (string.Equals(kv.Key, "LineNumberTag",
                                                    StringComparison.OrdinalIgnoreCase))
                                            {
                                                string? val = kv.Value?.ToString();
                                                if (!string.IsNullOrWhiteSpace(val))
                                                    lineTag = val;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // Relationship lookup failed — treat as unassigned
                            }

                            if (!groups.TryGetValue(lineTag, out LineGroupInfo? info))
                            {
                                info = new LineGroupInfo
                                {
                                    RowId = lgRowId,
                                    LineNumberTag = lineTag,
                                    IsVisible = true,
                                    ObjectIds = new List<ObjectId>()
                                };
                                groups[lineTag] = info;
                            }

                            info.ObjectIds.Add(id);

                            // If any component in the group is hidden, mark the whole group hidden
                            if (!ent.Visible)
                                info.IsVisible = false;
                        }
                        catch
                        {
                            // Skip problematic entities silently
                        }
                    }

                    tr.Commit();
                }
            }

            return groups.Values
                .OrderBy(g => g.LineNumberTag == "(Unassigned)" ? 1 : 0)
                .ThenBy(g => g.LineNumberTag)
                .ToList();
        }

        /// <summary>
        /// Sets the visibility of all entities in the given list.
        /// Must be called from a context where the document can be locked.
        /// </summary>
        public static void SetVisibility(Document doc, IReadOnlyList<ObjectId> objectIds, bool visible)
        {
            if (objectIds == null || objectIds.Count == 0) return;

            Database db = doc.Database;
            using (DocumentLock dl = doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in objectIds)
                {
                    if (id.IsErased) continue;
                    try
                    {
                        Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                        ent.Visible = visible;
                    }
                    catch
                    {
                        // Entity may have been erased or is not writable
                    }
                }

                tr.Commit();
            }

            // Force viewport refresh so visibility changes are drawn
            try { doc.Editor.UpdateScreen(); } catch { }
        }

        /// <summary>Show all objects across all line groups.</summary>
        public static void ShowAll(Document doc, List<LineGroupInfo> allGroups)
        {
            var allIds = allGroups.SelectMany(g => g.ObjectIds).ToList();
            SetVisibility(doc, allIds, true);
            foreach (var g in allGroups)
                g.IsVisible = true;
        }

        /// <summary>Hide all objects across all line groups.</summary>
        public static void HideAll(Document doc, List<LineGroupInfo> allGroups)
        {
            var allIds = allGroups.SelectMany(g => g.ObjectIds).ToList();
            SetVisibility(doc, allIds, false);
            foreach (var g in allGroups)
                g.IsVisible = false;
        }

        /// <summary>
        /// Isolate a single line: show only the specified line, hide all others.
        /// </summary>
        public static void IsolateLine(Document doc, List<LineGroupInfo> allGroups, string lineNumberTag)
        {
            // Collect show/hide lists to minimize transaction count
            var showIds = new List<ObjectId>();
            var hideIds = new List<ObjectId>();

            foreach (var g in allGroups)
            {
                bool isTarget = string.Equals(g.LineNumberTag, lineNumberTag, StringComparison.OrdinalIgnoreCase);
                if (isTarget)
                {
                    showIds.AddRange(g.ObjectIds);
                    g.IsVisible = true;
                }
                else
                {
                    hideIds.AddRange(g.ObjectIds);
                    g.IsVisible = false;
                }
            }

            // Apply in a single pass per visibility state
            Database db = doc.Database;
            using (DocumentLock dl = doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in hideIds)
                {
                    if (id.IsErased) continue;
                    try
                    {
                        Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                        ent.Visible = false;
                    }
                    catch { }
                }

                foreach (ObjectId id in showIds)
                {
                    if (id.IsErased) continue;
                    try
                    {
                        Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                        ent.Visible = true;
                    }
                    catch { }
                }

                tr.Commit();
            }

            try { doc.Editor.UpdateScreen(); } catch { }
        }
    }
}

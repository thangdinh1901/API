using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Draws connection port markers in the active drawing (independent of geometry rebuild).</summary>
    internal static class PortVisualService
    {
        private const string LayerName = "P3D_COMPOSER_PORTS";
        private const string RegAppName = "P3DCOMPOSER";

        private static readonly Color NormalColor = Color.FromRgb(0, 105, 148);
        private static readonly Color HighlightColor = Color.FromRgb(255, 220, 0);

        public static void Refresh(Document doc, ValveProject project, Guid? highlightPortId = null)
        {
            using DocumentLock docLock = doc.LockDocument();
            Database db = doc.Database;
            using Transaction tr = db.TransactionManager.StartTransaction();
            EnsureLayer(tr, db);
            ClearPortEntities(tr, db);

            if (project.ShowPortMarkers)
            {
                double arrowLen = ResolveArrowLength(project);
                foreach (ConnectionPort port in project.Ports.OrderBy(p => p.Number))
                {
                    bool highlight = highlightPortId.HasValue && highlightPortId.Value == port.Id;
                    DrawPortMarker(tr, db, project, port, arrowLen, highlight);
                }
            }

            tr.Commit();
        }

        public static void Clear(Document doc)
        {
            using DocumentLock docLock = doc.LockDocument();
            Database db = doc.Database;
            using Transaction tr = db.TransactionManager.StartTransaction();
            ClearPortEntities(tr, db);
            tr.Commit();
        }

        private const double ShaftLengthMm = 20.0;
        /// <summary>Forward overlap (mm) where cylinder meets cone — avoids a visible gap.</summary>
        private const double JoinOverlapMm = 5.0;
        /// <summary>Shift cylinder off sphere center toward the arrow tip (half of shaft + overlap).</summary>
        private static double ShaftStartOffsetMm => (ShaftLengthMm + JoinOverlapMm) / 2.0;

        private static double ResolveArrowLength(ValveProject project)
        {
            if (project.Parameters.DN > 1)
                return Math.Max(55, project.Parameters.DN * 0.5);
            return 55;
        }

        private static void DrawPortMarker(
            Transaction tr,
            Database db,
            ValveProject project,
            ConnectionPort port,
            double arrowLen,
            bool highlight)
        {
            double[] origin = PortTransformMath.GetWorldPosition(project, port);
            double[] dirArr = PortTransformMath.GetWorldDirection(project, port);
            var basePt = new Point3d(origin[0], origin[1], origin[2]);
            var direction = new Vector3d(dirArr[0], dirArr[1], dirArr[2]).GetNormal();
            Color color = highlight ? HighlightColor : NormalColor;

            BlockTableRecord space = (BlockTableRecord)tr.GetObject(
                db.CurrentSpaceId,
                OpenMode.ForWrite);

            double sphereRadius = Math.Max(4.0, arrowLen * 0.10);
            double shaftRadius = Math.Max(2.5, arrowLen * 0.055);
            double headLen = Math.Max(10.0, arrowLen * 0.20);
            double headBaseRadius = shaftRadius * 1.55;
            const double TipRadius = 0.001;

            ObjectId baseId = AddSphere(tr, space, basePt, sphereRadius, color);
            TagEntity(tr, db, baseId, port.Id);

            Matrix3d baseXform = AlignZToDirection(basePt, direction);
            double shaftStart = ShaftStartOffsetMm;

            Matrix3d shaftXform = baseXform * Matrix3d.Displacement(Vector3d.ZAxis * shaftStart);
            ObjectId shaftId = AddFrustumSolid(
                tr, space, shaftXform, ShaftLengthMm + JoinOverlapMm, shaftRadius, shaftRadius, color);
            TagEntity(tr, db, shaftId, port.Id);

            Matrix3d headXform = baseXform * Matrix3d.Displacement(Vector3d.ZAxis * (shaftStart + ShaftLengthMm));
            ObjectId headId = AddFrustumSolid(
                tr, space, headXform, headLen, headBaseRadius, TipRadius, color);
            TagEntity(tr, db, headId, port.Id);
        }

        private static ObjectId AddFrustumSolid(
            Transaction tr,
            BlockTableRecord space,
            Matrix3d transform,
            double height,
            double baseRadius,
            double topRadius,
            Color color)
        {
            var solid = new Solid3d();
            solid.CreateFrustum(height, baseRadius, baseRadius, topRadius);
            solid.TransformBy(transform);
            ApplySolidStyle(solid, color);
            ObjectId id = space.AppendEntity(solid);
            tr.AddNewlyCreatedDBObject(solid, true);
            return id;
        }

        private static ObjectId AddSphere(
            Transaction tr,
            BlockTableRecord space,
            Point3d center,
            double radius,
            Color color)
        {
            var solid = new Solid3d();
            solid.CreateSphere(radius);
            solid.TransformBy(Matrix3d.Displacement(center.GetAsVector()));
            ApplySolidStyle(solid, color);
            ObjectId id = space.AppendEntity(solid);
            tr.AddNewlyCreatedDBObject(solid, true);
            return id;
        }

        private static void ApplySolidStyle(Solid3d solid, Color color)
        {
            solid.Layer = LayerName;
            solid.Color = color;
        }

        private static Matrix3d AlignZToDirection(Point3d origin, Vector3d direction)
        {
            Vector3d z = Vector3d.ZAxis;
            Vector3d d = direction.GetNormal();

            Matrix3d align;
            if (d.IsParallelTo(z))
            {
                align = d.DotProduct(z) < 0
                    ? Matrix3d.Rotation(Math.PI, Vector3d.XAxis, Point3d.Origin)
                    : Matrix3d.Identity;
            }
            else
            {
                Vector3d axis = z.CrossProduct(d);
                double angle = z.GetAngleTo(d, axis);
                align = Matrix3d.Rotation(angle, axis, Point3d.Origin);
            }

            return Matrix3d.Displacement(origin.GetAsVector()) * align;
        }

        private static void TagEntity(Transaction tr, Database db, ObjectId id, Guid portId)
        {
            Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
            ent.XData = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, portId.ToString("N")));
        }

        private static void EnsureLayer(Transaction tr, Database db)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (!lt.Has(LayerName))
            {
                lt.UpgradeOpen();
                var layer = new LayerTableRecord
                {
                    Name = LayerName,
                    Color = NormalColor,
                };
                lt.Add(layer);
                tr.AddNewlyCreatedDBObject(layer, true);
            }

            RegAppTable rat = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
            if (!rat.Has(RegAppName))
            {
                rat.UpgradeOpen();
                var reg = new RegAppTableRecord { Name = RegAppName };
                rat.Add(reg);
                tr.AddNewlyCreatedDBObject(reg, true);
            }
        }

        private static void ClearPortEntities(Transaction tr, Database db)
        {
            BlockTableRecord space = (BlockTableRecord)tr.GetObject(
                db.CurrentSpaceId,
                OpenMode.ForWrite);

            var toErase = new List<ObjectId>();
            foreach (ObjectId id in space)
            {
                if (id.IsErased)
                    continue;

                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                if (!string.Equals(ent.Layer, LayerName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (HasPortTag(ent))
                    toErase.Add(id);
            }

            foreach (ObjectId id in toErase)
            {
                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                ent.Erase();
            }
        }

        private static bool HasPortTag(Entity ent)
        {
            ResultBuffer? xdata = ent.GetXDataForApplication(RegAppName);
            return xdata != null;
        }
    }
}

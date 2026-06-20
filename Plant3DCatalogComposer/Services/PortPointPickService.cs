using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class PointPickResult
    {
        public Point3d Position { get; init; }
        public Vector3d Direction { get; init; }
    }

    /// <summary>Picks WCS point(s) for port position and connection direction.</summary>
    internal static class PortPointPickService
    {
        private const double MinDirectionLengthMm = 0.5;

        public static bool TryPickPoint(Document doc, out PointPickResult? result)
        {
            result = null;
            Editor ed = doc.Editor;

            var ppo1 = new PromptPointOptions("\nP3D Composer — chọn vị trí port: ");
            ppo1.AllowNone = false;
            PromptPointResult pt1 = ed.GetPoint(ppo1);
            if (pt1.Status != PromptStatus.OK)
                return false;

            var ppo2 = new PromptPointOptions("\nP3D Composer — chọn hướng kết nối (điểm thứ 2): ");
            ppo2.BasePoint = pt1.Value;
            ppo2.UseBasePoint = true;
            ppo2.UseDashedLine = true;
            ppo2.AllowNone = false;
            PromptPointResult pt2 = ed.GetPoint(ppo2);

            Vector3d direction = Vector3d.XAxis;
            if (pt2.Status == PromptStatus.OK)
            {
                Vector3d delta = pt2.Value - pt1.Value;
                if (delta.Length >= MinDirectionLengthMm)
                    direction = delta.GetNormal();
            }

            result = new PointPickResult
            {
                Position = pt1.Value,
                Direction = direction,
            };
            return true;
        }
    }
}

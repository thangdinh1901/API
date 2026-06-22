using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace Plant3DCatalogComposer.Services
{
    public readonly struct DisplacementPickResult
    {
        public double Dx { get; init; }
        public double Dy { get; init; }
        public double Dz { get; init; }
        public double Distance { get; init; }
        public double FromX { get; init; }
        public double FromY { get; init; }
        public double FromZ { get; init; }
        public double ToX { get; init; }
        public double ToY { get; init; }
        public double ToZ { get; init; }
    }

    /// <summary>WCS displacement between two picked points (drawing units, typically mm).</summary>
    internal static class DistancePickService
    {
        private const double MinDistance = 0.1;

        public static bool TryPickDisplacement(Document doc, out DisplacementPickResult result)
        {
            result = default;
            Editor ed = doc.Editor;

            var ppo1 = new PromptPointOptions("\nP3D Composer — pick from point (on part): ");
            ppo1.AllowNone = false;
            PromptPointResult pt1 = ed.GetPoint(ppo1);
            if (pt1.Status != PromptStatus.OK)
                return false;

            var ppo2 = new PromptPointOptions("\nP3D Composer — pick to point (target): ");
            ppo2.BasePoint = pt1.Value;
            ppo2.UseBasePoint = true;
            ppo2.UseDashedLine = true;
            ppo2.AllowNone = false;
            PromptPointResult pt2 = ed.GetPoint(ppo2);
            if (pt2.Status != PromptStatus.OK)
                return false;

            Vector3d delta = pt2.Value - pt1.Value;
            double distance = delta.Length;
            if (distance < MinDistance
                && Math.Abs(delta.X) < MinDistance
                && Math.Abs(delta.Y) < MinDistance
                && Math.Abs(delta.Z) < MinDistance)
                return false;

            result = new DisplacementPickResult
            {
                Dx = delta.X,
                Dy = delta.Y,
                Dz = delta.Z,
                Distance = distance,
                FromX = pt1.Value.X,
                FromY = pt1.Value.Y,
                FromZ = pt1.Value.Z,
                ToX = pt2.Value.X,
                ToY = pt2.Value.Y,
                ToZ = pt2.Value.Z,
            };
            return true;
        }
    }
}

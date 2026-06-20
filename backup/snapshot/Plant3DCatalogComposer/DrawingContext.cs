using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace Plant3DCatalogComposer
{
    internal static class DrawingContext
    {
        public static string? GetActiveDrawingPath()
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return null;

            string path = doc.Database.Filename;
            if (!string.IsNullOrEmpty(path))
                return path;

            return doc.Name;
        }

        public static string RequireActiveDrawingPath()
        {
            string? path = GetActiveDrawingPath();
            if (path == null)
            {
                throw new InvalidOperationException(
                    "No active drawing. Open a Plant 3D DWG first.");
            }

            return path;
        }
    }
}

using System;
using Inventor;

namespace BoxExtrudeAddIn
{
    /// <summary>
    /// Tạo sketch hình chữ nhật trên mặt phẳng XY và extrude thành khối box.
    /// Đơn vị API Inventor: cm (database length units).
    /// </summary>
    public static class BoxExtrudeService
    {
        public static ExtrudeFeature CreateBox(
            Application inventorApp,
            double lengthCm,
            double widthCm,
            double heightCm)
        {
            if (lengthCm <= 0 || widthCm <= 0 || heightCm <= 0)
                throw new ArgumentException("Chiều dài, rộng, cao phải lớn hơn 0.");

            PartDocument partDoc = GetOrCreatePartDocument(inventorApp);
            PartComponentDefinition compDef = partDoc.ComponentDefinition;

            // WorkPlanes[3] = mặt phẳng XY (thường là mặt phẳng mặc định khi tạo part mới)
            PlanarSketch sketch = compDef.Sketches.Add(compDef.WorkPlanes[3]);
            TransientGeometry geom = inventorApp.TransientGeometry;

            sketch.SketchLines.AddAsTwoPointRectangle(
                geom.CreatePoint2d(0, 0),
                geom.CreatePoint2d(lengthCm, widthCm));

            Profile profile = sketch.Profiles.AddForSolid();

            ExtrudeDefinition extrudeDef = compDef.Features.ExtrudeFeatures.CreateExtrudeDefinition(
                profile,
                PartFeatureOperationEnum.kJoinOperation);

            extrudeDef.SetDistanceExtent(heightCm, PartFeatureExtentDirectionEnum.kPositiveExtentDirection);

            ExtrudeFeature extrude = compDef.Features.ExtrudeFeatures.Add(extrudeDef);

            inventorApp.ActiveView.Fit();
            return extrude;
        }

        private static PartDocument GetOrCreatePartDocument(Application inventorApp)
        {
            if (inventorApp.ActiveDocument is PartDocument activePart)
                return activePart;

            string templatePath = inventorApp.FileManager.GetTemplateFile(
                DocumentTypeEnum.kPartDocumentObject);

            return (PartDocument)inventorApp.Documents.Add(
                DocumentTypeEnum.kPartDocumentObject,
                templatePath,
                true);
        }
    }
}

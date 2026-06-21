using System;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Lap-joint stub end vs collar export aliases (same geometry, different PnP class).</summary>
    internal static class CatalogLapJointIds
    {
        public static bool IsCollarExport(string partId) =>
            partId.StartsWith("COLLAR_LJ_", StringComparison.OrdinalIgnoreCase);

        public static bool IsStubEndExport(string partId) =>
            partId.StartsWith("STUBEND_LJ_", StringComparison.OrdinalIgnoreCase);

        public static bool IsLjStubOrCollar(string partId) =>
            IsStubEndExport(partId) || IsCollarExport(partId);

        public static string? CollarExportIdFromStub(string stubPartId) =>
            IsStubEndExport(stubPartId)
                ? "COLLAR_" + stubPartId["STUBEND_".Length..]
                : null;

        public static string StubExportIdFromCollar(string collarPartId) =>
            IsCollarExport(collarPartId)
                ? "STUBEND_" + collarPartId["COLLAR_".Length..]
                : collarPartId;
    }
}

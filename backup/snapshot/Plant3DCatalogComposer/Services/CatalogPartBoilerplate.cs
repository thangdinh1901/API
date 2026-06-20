namespace Plant3DCatalogComposer.Services
{
    /// <summary>Identical files written for every catalog part on Generate Code.</summary>
    internal static class CatalogPartBoilerplate
    {
        /// <summary>Plant 3D module stub — {ScriptName}.xml and __INIT__.xml.</summary>
        public const string EmptyArrayOfScriptXml =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
            "<ArrayOfScript xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n" +
            "</ArrayOfScript>";
    }
}

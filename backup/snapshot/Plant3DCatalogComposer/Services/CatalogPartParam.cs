namespace Plant3DCatalogComposer.Services
{
    public sealed class CatalogPartParam
    {
        public required string Name { get; init; }

        public string Label { get; init; } = "";

        public double Default { get; init; }

        public bool UseSkeletonDN { get; init; }

        public bool UseSkeletonDN2 { get; init; }
    }
}

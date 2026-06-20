using System;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer
{
    public sealed class PrimitiveDefinition
    {
        public required PrimitiveType Type { get; init; }
        public required string DisplayName { get; init; }
        public required string Prefix { get; init; }

        public required (string Logical, string Expression, Func<SkeletonParameters, double> ValueMm, CatalogParamUnit Unit)[] Parameters { get; init; }
    }
}

using Inventor;

namespace Plant3DSkeletonManager
{
    /// <summary>
    /// Inventor-only preview parameters (V_*) derived from logical Plant 3D params.
    /// Never exported to scene JSON — expressions reference D, R, L, etc. on the part.
    /// </summary>
    internal static class PreviewGeometry
    {
        public static void AddElbowPreviewParams(PartComponentDefinition def)
        {
            AddDerivedLength(def, "V_leg", "R");
            AddDerivedLength(def, "V_arc_R", "R");
            AddDerivedLength(def, "V_tube_R", "D / 2");
        }

        public static void AddReducedElbowPreviewParams(PartComponentDefinition def)
        {
            AddElbowPreviewParams(def);
            AddDerivedLength(def, "V_tube_R_out", "D2 / 2");
        }

        public static void AddPyramidPreviewParams(PartComponentDefinition def)
        {
            AddDerivedLength(def, "V_top_L", "L * 0.12");
            AddDerivedLength(def, "V_top_W", "W1 * 0.12");
        }

        public static void AddEllipsoidSegmentPreviewParams(PartComponentDefinition def)
        {
            AddDerivedLength(def, "V_height", "RX");
        }

        private static void AddDerivedLength(PartComponentDefinition def, string name, string expression)
        {
            UserParameters up = def.Parameters.UserParameters;
            if (ParameterExists(up, name))
                return;
            up.AddByExpression(name, expression, UnitsTypeEnum.kMillimeterLengthUnits);
        }

        private static bool ParameterExists(UserParameters up, string name)
        {
            foreach (UserParameter p in up)
            {
                if (string.Equals(p.Name, name, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}

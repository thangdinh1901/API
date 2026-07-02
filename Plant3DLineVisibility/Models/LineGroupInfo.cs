using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace Plant3DLineVisibility.Models
{
    /// <summary>
    /// Represents a piping Line Group with its associated drawing objects.
    /// </summary>
    public class LineGroupInfo
    {
        /// <summary>Plant 3D database row ID of the Line Group (-1 if unassigned).</summary>
        public int RowId { get; set; } = -1;

        /// <summary>Line Number tag string (e.g. "10"-P-0001-A1A-N").</summary>
        public string LineNumberTag { get; set; } = "(Unassigned)";

        /// <summary>Number of piping components belonging to this line.</summary>
        public int ComponentCount => ObjectIds.Count;

        /// <summary>Current visibility state in the drawing.</summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>AutoCAD ObjectIds of all entities belonging to this line.</summary>
        public List<ObjectId> ObjectIds { get; set; } = new List<ObjectId>();
    }
}

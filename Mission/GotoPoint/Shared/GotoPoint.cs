using System;
using System.Collections.Generic;
using System.Text;

using CitizenFX.Core;


namespace Shared
{
    public struct CheckpointInfo
    {
        public int Icon { get; set; }
        public int IconColor { get; set; }
        public int Color { get; set; }
        public Vector3 Position { get; set; }
        public float Radius { get; set; }
    }

    public class CheckpointsInfo : Dictionary<int, CheckpointInfo> { }

}

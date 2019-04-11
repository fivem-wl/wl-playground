using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;

namespace Client
{
    public readonly struct MissionMarker
    {
        public Vector3 Position { get; }
        public Vector3 Scale { get; }
        /// <summary>
        /// Marker type.
        /// For marker type visual effect, please refer to <seealso cref="https://wiki.gtanet.work/index.php?title=Marker"/>.
        /// </summary>
        private MarkerType Type { get; }
        private Color Color { get; }
        private Vector3 Direction { get; }
        private Vector3 Rotation { get; }
        private float DrawDistance { get; }

        public bool IsPlayerInDrawDistance
        {
            get { return Position.DistanceToSquared(Game.PlayerPed.Position) <= Math.Pow(DrawDistance, 2); }
        }

        public bool IsPositionInDrawDistance(Vector3 position)
        {
            return Position.DistanceToSquared(position) <= Math.Pow(DrawDistance, 2);
        }

        public MissionMarker(Vector3 position, Vector3 scale)
        {
            Type = MarkerType.VerticalCylinder;
            Color = Color.FromArgb(128, 255, 255, 0);
            Direction = Vector3.Zero;
            Rotation = Vector3.Zero;
            DrawDistance = 250f;

            Position = position;
            Scale = scale;
        }

        /// <summary>
        /// Draws mission marker in the world, this needs to be done on a per frame basis
        /// </summary>
        public void DrawThisFrame()
        {
            if (IsPlayerInDrawDistance)
            {
                World.DrawMarker(Type, Position, Direction, Rotation, Scale, Color);
            }
        }

    }
}

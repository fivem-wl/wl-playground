using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace Client
{
    public struct Location
    {
        public Vector3 Position { get; }
        public float Radius { get; }
        private float DrawDistance { get; }

        public Location(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
            DrawDistance = 100f;
        }

        public bool IsPositionInside(Vector3 position)
        {
            return Vector3.DistanceSquared(Position, position) <= Radius * Radius;
        }
        public bool IsPositionInDrawDistance(Vector3 position)
        {
            return Vector3.DistanceSquared(Position, position) <= DrawDistance * DrawDistance;
        }

        public bool IsPlayerInside
        {
            get { return IsPositionInside(Game.PlayerPed.Position); }
        }
        public bool IsPlayerInDrawDistance
        {
            get { return IsPositionInDrawDistance(Game.PlayerPed.Position); }
        }
    }
}

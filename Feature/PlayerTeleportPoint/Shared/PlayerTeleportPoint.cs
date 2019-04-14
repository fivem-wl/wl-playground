using System;
using System.Collections.Generic;
using System.Text;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;


namespace Shared
{
    public class PlayerTeleportPoint
    {
        public string CommandName { get; private set; }
        public Vector3 Position { get; private set; }
        public float Heading { get; private set; }
        public string CreatorIdentifier { get; private set; }

        public PlayerTeleportPoint(string commandName, Vector3 position, float heading, string creatorIdentifier)
        {
            CommandName = commandName;
            Position = position;
            Heading = heading;
            CreatorIdentifier = creatorIdentifier;
        }
    }

}

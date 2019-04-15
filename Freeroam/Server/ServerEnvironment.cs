using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace Server
{
    public class ServerEnvironment : BaseScript
    {
        public ServerEnvironment()
        {
            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(LogOnPlayerConnect);
            EventHandlers["playerDropped"] += new Action<Player, string>(LogOnPlayerDisconnect);
        }

        /// <summary>
        /// Log player connecting info.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="playerName"></param>
        /// <param name="setKickReason"></param>
        /// <param name="deferrals"></param>
        private void LogOnPlayerConnect([FromSource]Player player, string playerName, dynamic setKickReason, dynamic deferrals)
        {
            deferrals.defer();
            var licenseIdentifier = player.Identifiers["license"];

            Debug.WriteLine(
                $"[wl][{DateTime.UtcNow}]A player with the name {playerName} (Identifier: [{licenseIdentifier}] - [{player.EndPoint}]) " +
                $"is connecting to the server.");

            deferrals.update($"Hello {playerName}, your license [{licenseIdentifier}] is being checked");

            deferrals.done();
        }

        /// <summary>
        /// Log player disconnect info.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reason"></param>
        private void LogOnPlayerDisconnect([FromSource]Player player, string reason)
        {
            Debug.WriteLine($"[wl][{DateTime.UtcNow}]Player {player.Name} dropped (Reason: {reason}).");
        }
    }
}

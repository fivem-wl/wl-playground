using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;


namespace Server
{
    class JoinAndLeaveMessage : BaseScript
    {
        public JoinAndLeaveMessage()
        {
            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(OnPlayerConnecting);
            EventHandlers["playerDropped"] += new Action<Player, string>(OnPlayerDropped);

        }

        private void OnPlayerConnecting([FromSource]Player player, string playerName, dynamic setKickReason, dynamic deferrals)
        {
            TriggerEvent("chat:addMessage", new
            {
                color = new[] { 255, 255, 255 },
                args = new[] { "", $"{Game.Player.Name}进入了服务器" }
            });
        }

        private void OnPlayerDropped([FromSource]Player player, string reason)
        {
            TriggerEvent("chat:addMessage", new
            {
                color = new[] { 255, 255, 255 },
                args = new[] { "", $"{Game.Player.Name}离开了服务器" }
            });
        }
    }
}

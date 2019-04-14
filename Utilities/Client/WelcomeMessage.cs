using System;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace Client
{
    // 登录欢迎
    class WelcomeMessage : BaseScript
    {
        public WelcomeMessage()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
        }

        private void OnClientResourceStart(string resourceName)
        {
            if (GetCurrentResourceName() != resourceName)
            {
                return;
            }

            TriggerEvent("chat:addMessage", new
            {
                color = new[] { 255, 255, 255 },
                args = new[] { "[系统]", $"欢迎来到未来世界，{GetPlayerName(Game.Player.ServerId)}" }
            });
            
        }
    }
}

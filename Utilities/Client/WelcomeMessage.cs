using System;
using CitizenFX.Core;

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
            TriggerEvent("chat:addMessage", new
            {
                color = new[] { 255, 255, 255 },
                args = new[] { "", $"欢迎来到未来世界，{Game.Player.Name}" }
            });
            
        }
    }
}

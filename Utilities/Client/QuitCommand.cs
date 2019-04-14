using System;
using System.Collections.Generic;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace Client
{
    // /q
    class QuitCommand : BaseScript
    {
        public QuitCommand()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
        }

        private void OnClientResourceStart(string resourceName)
        {
            if (GetCurrentResourceName() != resourceName)
            {
                return;
            }

            RegisterCommand("q", new Action<int, List<object>, string>(async (source, args, raw) =>
            {
                TriggerEvent("chat:addMessage", new
                {
                    color = new[] { 255, 255, 255 },
                    args = new[] { "[系统]", $"下次再来~" }
                });

                // 2秒延迟
                await BaseScript.Delay(2000);

                // 退出游戏，使用native
                ForceSocialClubUpdate();
            }), false);

            // /q的提示
            TriggerEvent("chat:addSuggestion", "/q", "2秒后退出游戏");
        }
    }
}

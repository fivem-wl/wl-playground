using System;
using CitizenFX.Core;

namespace Client
{
    // 增加命令提示
    class CommandSuggestions : BaseScript
    {
        public CommandSuggestions()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
        }

        private void OnClientResourceStart(string resourceName)
        {
            // car的提示
            TriggerEvent("chat:addSuggestion", "/car", "用来出生载具", new[]
            {
                new { name="车名", help="可在此查看 https://wiki.gt-mp.net/index.php/Vehicle_Models" }
            });
        }
    }
}

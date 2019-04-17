using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

using Extensions;
using Shared;


namespace Server
{

    public class SimpleRace
    {

        private CheckpointsInfo CheckpointsInfo { get; set; }

        public string Attending { get; set; } = string.Empty;

        public void UpdateRace(string checkpointsInfo)
            => JsonConvert.DeserializeObject<CheckpointsInfo>(checkpointsInfo);

        public string GetRaceAsJson()
            => JsonConvert.SerializeObject(CheckpointsInfo);

    }

    public class GotoPoint : BaseScript
    {

        private const string ResourceName = "wlMissionGotoPoint";
        private const string ResourceCommand = "srace";
        private Dictionary<string, SimpleRace> PlayersSimpleRace = new Dictionary<string, SimpleRace>();
        
        public GotoPoint()
        {

            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(OnPlayerConnect);
            EventHandlers["playerDropped"] += new Action<Player, string>(OnPlayerDisconnect);

            EventHandlers.Add($"{ResourceName}:UpdatePlayerRace", new Action<Player, string>(UpdatePlayerSimpleRace));
            EventHandlers.Add($"{ResourceName}:PullPlayerRace", new Action<Player, string>(PullPlayerRace));
            EventHandlers.Add($"{ResourceName}:BroadcastPlayerRace", new Action<Player>(BroadcastPlayerRace));

        }

        private void UpdatePlayerSimpleRace([FromSource] Player source, string checkpointsInfo)
        {
            PlayersSimpleRace[source.Identifiers["license"]].UpdateRace(checkpointsInfo);

            TriggerClientEvent(source, $"{ResourceName}:WaitForServerResponse");
        }

        private void PullPlayerRace([FromSource] Player source, string inviterServerId)
        {
            var inviterLicense = FindPlayerByServerId(inviterServerId)?.Identifiers["license"];

            PlayersSimpleRace[source.Identifiers["license"]].Attending = inviterLicense;

            TriggerClientEvent(
                source, $"{ResourceName}:PullPlayerRace",
                PlayersSimpleRace.ContainsKey(inviterLicense) ? PlayersSimpleRace[inviterLicense].GetRaceAsJson() : string.Empty);

            TriggerClientEvent(source, $"{ResourceName}:WaitForServerResponse");
        }

        private Player FindPlayerByServerId(string serverId)
            => Players.Where(p => p.Handle == serverId)?.FirstOrDefault();

        private void BroadcastPlayerRace([FromSource] Player source)
        {
            TriggerClientEvent("chat:addMessage", new
            {
                color = new[] { 255, 255, 0 },
                args = new[] { "[简易比赛]", $"{source.Name}发起了比赛, 输入 - /{ResourceCommand} join {source.Handle} - 加入比赛!" },
            });
        }

        private void OnPlayerConnect([FromSource] Player source, string playerName, dynamic setKickReason, dynamic deferrals)
        {
            PlayersSimpleRace.Add(source.Identifiers["license"], new SimpleRace());
        }

        private void OnPlayerDisconnect([FromSource] Player source, string reason)
        {
            PlayersSimpleRace.Remove(source.Identifiers["license"]);
        }


        ///// <summary>
        ///// 获取玩家的license
        ///// </summary>
        ///// <param name="source"></param>
        ///// <returns></returns>
        //private string PlayerId([FromSource] Player source)
        //{
        //    var licenseIdentifier = source.Identifiers["license"];
        //    return licenseIdentifier;
        //}
    }
}

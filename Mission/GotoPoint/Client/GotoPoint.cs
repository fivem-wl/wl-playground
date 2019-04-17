using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using Newtonsoft.Json;


using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using CitizenFX.Core.UI;


using Shared;


namespace Client
{
    /*
    功能: 
    1. /race make1 -> 设置起点
    2. /race make2 -> 设置终点
    3. /race goto 1 -> 传送到起点
    4. /race goto 2 -> 传送到终点
    5. /race start -> 开始倒计时 - 首先一个countdown, 再一个计时
    */
        

    public class SimpleRace
    {
        public CheckpointsInfo CheckpointsInfo = new CheckpointsInfo();
        private Dictionary<int, Checkpoint> Checkpoints = new Dictionary<int, Checkpoint>();
        private Dictionary<int, Blip> Blips = new Dictionary<int, Blip>();

        public SimpleRace()
        {

        }

        public void SetCheckPoint(int index)
        {
            // Might use code.
            //CheckpointCustomIcon checkpointCustomIcon = new CheckpointCustomIcon
            //{
            //    Style = CheckpointCustomIconStyle.CycleArrow
            //};
            //CylinderFarHeight = 10f,
            //CylinderNearHeight = 5f,
            //CylinderRadius = 10f,
            //CustomIcon = checkpointCustomIcon

            CheckpointsInfo[index] = new CheckpointInfo
            {
                Icon = (int)CheckpointIcon.CycleArrow,
                IconColor = Color.FromArgb(128, 255, 255, 0).ToArgb(),
                Color = Color.FromArgb(128, 255, 255, 0).ToArgb(),
                Position = Game.PlayerPed.Position,
                Radius = 10f,
            };
        }

        public string CheckpointsInfoAsJson()
        {
            return JsonConvert.SerializeObject(this.CheckpointsInfo);
        }

        public void ShowCheckpoint(int index)
        {
            Checkpoints[index] = World.CreateCheckpoint(
                (CheckpointIcon) CheckpointsInfo[index].Icon,
                CheckpointsInfo[index].Position,
                CheckpointsInfo.ContainsKey(index + 1) ? CheckpointsInfo[index + 1].Position : Vector3.Zero,
                CheckpointsInfo[index].Radius,
                Color.FromArgb(CheckpointsInfo[index].Color));

            Blips[index] = World.CreateBlip(CheckpointsInfo[index].Position);
            Blips[index].IsShortRange = false;
        }

        public void HideCheckpoint(int index)
        {
            Checkpoints[index].Delete();
            Blips[index].Delete();
        }
        
        public void Update(string checkpointsInfoAsJson)
        {
            this.CheckpointsInfo = JsonConvert.DeserializeObject<CheckpointsInfo>(checkpointsInfoAsJson);
        }
    }
    

    public class GotoPoint : BaseScript
    {
        public const string ResourceName = "wlMissionGotoPoint";
        public const string ResourceCommand = "srace";

        private int WaitTime = 0;
        private async Task WaitForServerResponse(int timeout = 1000 * 10)
        {
            var now = GetGameTimer();
            WaitTime = now;

            while (now == WaitTime && GetGameTimer() - now < timeout) await Delay(100);

            if (GetGameTimer() - now >= timeout)
            {
                throw (new TimeoutException($"Timeout exceeded ({timeout})."));
            }
        }

        private SimpleRace SimpleRace = new SimpleRace();

        private void UpdateWaitTime()
            => WaitTime = GetGameTimer();
        
        public GotoPoint()
        {

            EventHandlers.Add($"{ResourceName}:WaitForServerResponse", new Action(() =>
            {
                UpdateWaitTime();
            }));

            EventHandlers.Add($"{ResourceName}:PullPlayerRace", new Action<string>((string checkpointsInfoAsJson) =>
            {
                SimpleRace.Update(checkpointsInfoAsJson);
                UpdateWaitTime();
            }));

            RegisterCommand("srace", new Action<int, List<object>, string>(async (source, args, raw) =>
            {
                if (args.Count <= 0)
                {
                    SimpleRace.ShowHelp();
                    return;
                }

                switch (args[0].ToString())
                {
                    case "make1":
                        SimpleRace.SetCheckPoint(1);
                        Notify.Alert("set point 1");
                        break;
                    case "make2":
                        SimpleRace.SetCheckPoint(2);
                        Notify.Alert("set point 2");
                        break;
                    case "start":

                        TriggerServerEvent($"{ResourceName}:UpdatePlayerRace", SimpleRace.CheckpointsInfoAsJson());

                        try { await WaitForServerResponse(); }
                        catch (TimeoutException e) { Notify.Alert($"Failed, reason: {e.Message}"); }

                        TriggerServerEvent($"{ResourceName}:BroadcastPlayerRace");

                        Notify.Info("uploaded");
                        break;

                    case "join":

                        if (args.Count == 1 || args[1].ToString().Any(c => !char.IsDigit(c)))
                        {
                            Notify.Alert("Input player id || digit only");
                            return;
                        }
                        var inviterServerId = args[1].ToString();
                        TriggerServerEvent("wlMissionGotoPoint:PullPlayerRace", inviterServerId);

                        try { await WaitForServerResponse(); }
                        catch (TimeoutException e) { Notify.Alert($"Failed, reason: {e.Message}"); }

                        break;

                    case "ready":
                        TriggerServerEvent("wlMissionGotoPoint:StartPlayerRace");
                        break;
                }


            }), false);

        }

    }
}

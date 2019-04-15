using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace Client
{
    public class CarCommands : BaseScript
    {
        public CarCommands()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
        }

        private bool driftMode = false;

        private void OnClientResourceStart(string resourceName)
        {
            if (GetCurrentResourceName() != resourceName)
            {
                return;
            }

            // 修复与清洗载具
            RegisterCommand("fix", new Action<int, List<object>, string>( (source, args, raw) =>
            {
                int vehicle = GetVehiclePedIsIn(Game.PlayerPed.Handle, false);
                SetVehicleEngineHealth(vehicle, 1000);
                SetVehicleEngineOn(vehicle, true, true, false);
                SetVehicleFixed(vehicle);
                SetVehicleDirtLevel(vehicle, 0);

                Notify.Success($"你的载具来了一套洗吹剪", false, false);

            }), false);

            // 漂移模式
            RegisterCommand("drift", new Action<int, List<object>, string>((source, args, raw) =>
            {
               
                int vehicle = GetVehiclePedIsIn(Game.PlayerPed.Handle, false);

                if (driftMode == false)
                {
                    SetVehicleReduceGrip(vehicle, true);
                    driftMode = true;
                    Notify.Info($"拓海严肃脸", false, false);
                }
                else
                {
                    SetVehicleReduceGrip(vehicle, false);
                    driftMode = false;
                    Notify.Info($"拓海打工脸", false, false);
                }

            }), false);

            // fix和drift的命令提示
            TriggerEvent("chat:addSuggestion", "/fix", "修复与清洁载具");
            TriggerEvent("chat:addSuggestion", "/drift", "漂移模式");
        }
    }
}

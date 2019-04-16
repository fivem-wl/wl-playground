using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace Client
{
    // 出生载具有关的命令
    public class CarSpawner : BaseScript
    {
        private class SpawnCarCommand
        {
            public string Command { get; set; }
            public string VehicleModel { get; set; }
            public string Description { get; set; }

            public SpawnCarCommand(string command, string model, string description)
            {
                Command = command;
                VehicleModel = model;
                Description = description;
            }
        }

        // 怀旧的samp类型指令
        private static readonly IReadOnlyList<SpawnCarCommand> SampCommands = new List<SpawnCarCommand>
        {
            new SpawnCarCommand("tur", "Turismo2", "出生经典Turismo"),
            new SpawnCarCommand("tur2", "Turismor", "出生Turismo R"),
            new SpawnCarCommand("inf", "Infernus2", "出生经典Infernus"),
            new SpawnCarCommand("inf2", "Infernus", "出生Infernus"),
            new SpawnCarCommand("sho", "Shotaro", "出生Shotaro")
        };

        private Vehicle previousCar = null;

        public CarSpawner() =>
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);

        // 如果上一辆车还存在
        // 被玩家拥有
        // 删除
        private void RemovePreviousCar()
        {
            if (previousCar != null)
            {
                if (previousCar.Exists() && previousCar.PreviouslyOwnedByPlayer)
                {
                    previousCar.PreviouslyOwnedByPlayer = false;
                    SetEntityAsMissionEntity(previousCar.Handle, true, true);
                    previousCar.Delete();
                }
                previousCar = null;
            }
        }

        // 刷车
        private async Task SpawnCar(string model)
        {
            // 检查模型存在与否
            var hash = (uint)GetHashKey(model);
            if (!IsModelInCdimage(hash) || !IsModelAVehicle(hash))
            {
                Notify.Alert($"xiaogo, {model}不存在!", true, false);
                return;
            }

            // 删除上辆车
            RemovePreviousCar();

            // 创造车辆
            var vehicle = await World.CreateVehicle(model, Game.PlayerPed.Position, Game.PlayerPed.Heading);

            // 给车辆上属性
            vehicle.PreviouslyOwnedByPlayer = true;
            vehicle.IsPersistent = true;
            vehicle.NeedsToBeHotwired = false;
            vehicle.IsStolen = false;
            vehicle.IsEngineRunning = true;

            // 这车成为‘上一辆车’
            previousCar = vehicle;

            // 把玩家扔进车里
            Game.PlayerPed.SetIntoVehicle(vehicle, VehicleSeat.Driver);

            Notify.Success($"把你扔进了{vehicle.LocalizedName}", false, false);
        }

        private void OnClientResourceStart(string resourceName)
        {
            if (GetCurrentResourceName() != resourceName)
                return;

            RegisterCommand("car", new Action<int, List<object>, string>(async (source, args, raw) =>
            {
                // 检查输入的arg
                // 没输入或者多输入了，弹错
                switch (args.Count)
                {
                    case 0:
                        Notify.Alert("太北上了, 你敲的车名是空的...", true, false);
                        break;

                    case 1:
                        // 刷车
                        var model = args[0].ToString();
                        await SpawnCar(model);
                        break;

                    default:
                        Notify.Alert("太TK了, 我只接受一个车名...", true, false);
                        break;
                }
            }), false);

            // /car的提示
            TriggerEvent("chat:addSuggestion", "/car", "出生载具", new[]
            {
                new { name = "车名", help = "https://wiki.gt-mp.net/index.php/Vehicle_Models" }
            });

            // samp类型的提示
            foreach (var cmd in SampCommands)
            {
                RegisterCommand(cmd.Command, new Action<int, List<object>, string>(async (source, args, raw) =>
                {
                    await SpawnCar(cmd.VehicleModel);
                }), false);

                TriggerEvent("chat:addSuggestion", $"/{cmd.Command}", cmd.Description);
            }
        }
    }
}

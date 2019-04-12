using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace Client
{
    public class CarSpawner : BaseScript
    {
        public CarSpawner()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
        }

        private Vehicle previousCar = null;

        // 如果上一辆车还存在
        // 被玩家拥有
        // 删除
        private void removePreviousCar()
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
        private async Task spawnCar(String model)
        {
            // 检查模型存在与否
            var hash = (uint)GetHashKey(model);
            if (!IsModelInCdimage(hash) || !IsModelAVehicle(hash))
            {
                TriggerEvent("chat:addMessage", new
                {
                    color = new[] { 255, 0, 0 },
                    args = new[] { "[车管]", $"xiaogo, {model}不存在!" }
                });
                return;
            }

            // 删除上辆车
            removePreviousCar();

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

            TriggerEvent("chat:addMessage", new
            {
                color = new[] { 0, 128, 255 },
                args = new[] { "[车管]", $"把你扔进了{vehicle.LocalizedName}" }
            });
        }

        private void OnClientResourceStart(string resourceName)
        {
            if (GetCurrentResourceName() != resourceName)
            {
                return;
            }

            RegisterCommand("car", new Action<int, List<object>, string>(async (source, args, raw) =>
            {
                // 检查输入的arg
                // 没输入或者多输入了，弹错
                var model = " ";
                if (args.Count == 1)
                {
                    model = args[0].ToString();
                }
                else if (args.Count == 0)
                {
                    TriggerEvent("chat:addMessage", new
                    {
                        color = new[] { 255, 0, 0 },
                        args = new[] { "[车管]", $"太北上了, 你敲的空的车名..." }
                    });
                    return;
                }
                else
                {
                    TriggerEvent("chat:addMessage", new
                    {
                        color = new[] { 255, 0, 0 },
                        args = new[] { "[车管]", $"太TK了, 我只接受一个车名..." }
                    });
                    return;
                }

                // 刷车
                await spawnCar(model);

            }), false);

            // 怀旧的samp类型指令
            var sampCommands = new Dictionary<string, string>();
            sampCommands.Add("tur", "Turismo2");
            sampCommands.Add("tur2", "Turismor");
            sampCommands.Add("inf", "Infernus2");
            sampCommands.Add("inf2", "Infernus");
            sampCommands.Add("sho", "Shotaro");

            foreach (KeyValuePair<string, string> command in sampCommands)
            {
                RegisterCommand(command.Key, new Action<int, List<object>, string>(async (source, args, raw) =>
                {
                    var model = command.Value;
                    // 刷车
                    await spawnCar(model);
                }), false);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace Client
{
    public class CarSpawner : BaseScript
    {
        public CarSpawner()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart));
        }

        private void OnClientResourceStart(string resourceName)
        {
            RegisterCommand("car", new Action<int, List<object>, string>(async (source, args, raw) =>
            {
                // 检查输入的arg
                // -h == --help
                // 没输入或者多输入了，弹错
                var model = " ";
                if (args.Count == 1)
                {
                    if (args[0].ToString() == "-h")
                    {
                        TriggerEvent("chat:addMessage", new
                        {
                            color = new[] { 51, 51, 255 },
                            args = new[] { "[车管帮助]", $"请输入/car %车名%" }
                        });
                        TriggerEvent("chat:addMessage", new
                        {
                            color = new[] { 51, 51, 255 },
                            args = new[] { "[车管帮助]", $"%车名%请看 https://wiki.gt-mp.net/index.php/Vehicle_Models" }
                        });
                        return;
                    }

                    model = args[0].ToString();
                }
                else if (args.Count == 0)
                {
                    TriggerEvent("chat:addMessage", new
                    {
                        color = new[] { 255, 0, 0 },
                        args = new[] { "[车管]", $"太北上了, 你敲的车名都是null..." }
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

                // 检查模型存在与否
                // assumes the directive `using static CitizenFX.Core.Native.API;`
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

                // 创造车辆
                var vehicle = await World.CreateVehicle(model, Game.PlayerPed.Position, Game.PlayerPed.Heading);

                // 把玩家扔进车里
                Game.PlayerPed.SetIntoVehicle(vehicle, VehicleSeat.Driver);

                TriggerEvent("chat:addMessage", new
                {
                    color = new[] { 51, 51, 255 },
                    args = new[] { "[车管]", $"{model}, ging!" }
                });
            }), false);

            RegisterCommand("tur", new Action<int, List<object>, string>(async (source, args, raw) =>
            {
                // 检查输入的arg
                // -h = --help
                // 空白默认成Turismo R
                // -sa则是sa的Turismo
                var model = "Turismor";
                if (args.Count == 1)
                {
                    if (args[0].ToString() == "-h")
                    {
                        TriggerEvent("chat:addMessage", new
                        {
                            color = new[] { 51, 51, 255 },
                            args = new[] { "[车管帮助]", $"请输入/tur" }
                        });
                        TriggerEvent("chat:addMessage", new
                        {
                            color = new[] { 51, 51, 255 },
                            args = new[] { "[车管帮助]", $"/tur -sa可以出生怀旧Turismo" }
                        });
                        return;
                    }
                    else if (args[0].ToString() == "-sa")
                    {
                        model = "Turismo2";
                    }   
                }

                // 检查模型存在与否
                // assumes the directive `using static CitizenFX.Core.Native.API;`
                var hash = (uint)GetHashKey(model);
                if (!IsModelInCdimage(hash) || !IsModelAVehicle(hash))
                {
                    TriggerEvent("chat:addMessage", new
                    {
                        color = new[] { 255, 0, 0 },
                        args = new[] { "[车管]", $"请联系管理员关于/tur的问题，提起{model}不存在." }
                    });
                    return;
                }

                // 创造车辆
                var vehicle = await World.CreateVehicle(model, Game.PlayerPed.Position, Game.PlayerPed.Heading);

                // 把玩家扔进车里
                Game.PlayerPed.SetIntoVehicle(vehicle, VehicleSeat.Driver);

                TriggerEvent("chat:addMessage", new
                {
                    color = new[] { 51, 51, 255 },
                    args = new[] { "[车管]", $"{model}, ging!" }
                });
            }), false);
        }
    }
}

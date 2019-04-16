using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;

using Shared;
using Extensions;


namespace Client
{

    /// <summary>
    /// 传送命令列表
    /// </summary>
    public class PlayerTeleportPoints : Dictionary<string, PlayerTeleportPoint>
    {

        /// <summary>
        /// 检查命令是否存在
        /// </summary>
        /// <param name="commandName"></param>
        /// <returns></returns>
        public async Task<bool> Existed(string commandName)
        {
            // 首先检查本地记录, 如果有则检查本地记录; 为null值则表示不存在
            if (this.ContainsKey(commandName))
            {
                return !(this[commandName] is null);
            }
            // 如果无本地记录, 则从服务器请求, 并写入到本地, 然后再检查本地记录
            else
            {
                // 执行服务端的Event
                BaseScript.TriggerServerEvent("wlPlayerTeleportPoint:LoadTeleportPoint", commandName);
                // 等待服务端Event执行完成
                await WaitForSetTeleportPoint();
                var now = GetGameTimer();

                var playerTeleportPoint = this.GetValueOrDefault(commandName);
                return !(playerTeleportPoint is null);
            }
        }

        /// <summary>
        /// 创建命令 - From Client
        /// </summary>
        public async Task AddNewCommand(string commandName)
        {
            var position = Game.PlayerPed.Position;
            var heading = Game.PlayerPed.Heading;
            BaseScript.TriggerServerEvent("wlPlayerTeleportPoint:AddNewCommand", commandName, position, heading);
            // 等待服务端Event执行完成
            await WaitForSetTeleportPoint();
        }


        private int LastWaitForSetTeleportPoint = 0;
        /// <summary>
        /// 等待至SetTeleportPoint调用, 用于等待/确认服务端已经执行相关Event
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        private async Task WaitForSetTeleportPoint(int timeout = 1000 * 10)
        {
            var now = GetGameTimer();
            LastWaitForSetTeleportPoint = now;
            while (now == LastWaitForSetTeleportPoint && GetGameTimer() - now < timeout) await BaseScript.Delay(100);
        }
        /// <summary>
        /// 设置传送命令 - Trigger by TriggerServerEvent
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="commandExistedInServer"></param>
        /// <param name="position"></param>
        /// <param name="heading"></param>
        /// <param name="creatorIdentifier"></param>
        public void SetTeleportPoint(string commandName, bool commandExistedInServer, Vector3 position, float heading, string creatorIdentifier)
        {
            // 如果服务器有记录, 则使用服务器记录; 否则, 标记为null
            if (commandExistedInServer)
            {
                var playerTeleportPoint = new PlayerTeleportPoint(commandName, position, heading, creatorIdentifier);
                this[commandName] = playerTeleportPoint;
            }
            else
            {
                this[commandName] = null;
            }
            // 设置最近一次调用次Method的时间
            LastWaitForSetTeleportPoint = GetGameTimer();
        }

    }

    public class Main : BaseScript
    {

        private const string ResourceDisplayName = "传送";
        
        private PlayerTeleportPoints PlayerTeleportPoints = new PlayerTeleportPoints();
        

        public Main()
        {
            // 注册PlayerTeleportPoints - SetTeleportPoint
            EventHandlers.Add("wlPlayerTeleportPoint:SetTeleportPoint",
                new Action<string, bool, Vector3, float, string>(PlayerTeleportPoints.SetTeleportPoint));

            RegisterCommand("tpmake", new Action<int, List<object>, string>
                (async (source, args, raw) => await CommandMakeTeleportPoint(source, args, raw)), false);

            RegisterCommand("tp", new Action<int, List<object>, string>
                (async (source, args, raw) => await CommandGotoTeleportPoint(source, args, raw)), false);

        }

        // 命令: 制作传送点
        private async Task CommandMakeTeleportPoint(int source, List<object> args, string raw)
        {
            if (args.Count <= 0)
            {
                Notify.Alert($"[{ResourceDisplayName}]请输入传送点名称");
                return;
            }

            var commandName = args[0].ToString();

            /* - Failed to use regex in Fivem Mono runtime library - System.TimeSpan is not accessible
            var matchPattern = @"^[a-zA-Z0-9_\p{L}]{1,16}$";     // 只能包含大小写字母, 数字, 中文以及下划线, 长度不大于16
            if (!Regex.IsMatch(commandName, matchPattern))
            {
                Notify.Alert($"[{ResourceDisplayName}]名称不合法, 是否过长或者使用了特殊符号?");
                return;
            }
            */

            if (commandName.Length > 16 || commandName.Any(c => !char.IsLetterOrDigit(c)))
            {
                Notify.Alert($"[{ResourceDisplayName}]名称不合法, 是否过长或者使用了特殊符号?");
                return;
            }
            
            if (await PlayerTeleportPoints.Existed(commandName))
            {
                Notify.Alert($"[{ResourceDisplayName}]创建失败, 名称已经被使用, 请使用新的名称");
                return;
            }
            
            await PlayerTeleportPoints.AddNewCommand(commandName);

            // 再次确认传送命令是否写入 - 考虑网络波动情况
            if (await PlayerTeleportPoints.Existed(commandName))
            {
                Notify.Success($"[{ResourceDisplayName}]创建成功, 呼唤基友一起到这里玩耍吧");
            }
            else
            {
                Notify.Alert($"[{ResourceDisplayName}]创建失败, 与服务器的连接不稳定, 请重试");
            }
            return;
        }

        // 命令: 去传送点
        private async Task CommandGotoTeleportPoint(int source, List<object> args, string raw)
        {

            if (args.Count <= 0)
            {
                Notify.Alert($"[{ResourceDisplayName}]传送失败, 请输入传送点名称");
                return;
            }
            if (args.Count >= 2)
            {
                Notify.Alert($"[{ResourceDisplayName}]传送失败, 传送点名称不包含空格");
                return;
            }

            var commandName = args[0].ToString();

            if (await PlayerTeleportPoints.Existed(commandName))
            {
                await Teleport.TeleportToCoords(PlayerTeleportPoints[commandName].Position);
                Game.PlayerPed.Heading = PlayerTeleportPoints[commandName].Heading;

                TriggerServerEvent("wlPlayerTeleportPoint:RecordCommandUsage", Game.Player.ServerId,  commandName);
            }
            else
            {
                Notify.Alert($"[{ResourceDisplayName}]传送失败, 是否输入了不存在的传送命令?");
            }

        }

    }
}

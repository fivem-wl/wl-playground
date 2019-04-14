using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

using Shared;


namespace Server
{

    public sealed class PlayerTeleportPoints : Dictionary<string, PlayerTeleportPoint>
    {
        /// <summary>
        /// 是否包含传送命令
        /// </summary>
        /// <param name="commandName"></param>
        /// <returns></returns>
        public bool Has(string commandName)
        {
            return this.ContainsKey(commandName);
        }
    }

    public sealed class Main : BaseScript
    {

        private PlayerTeleportPoints PlayerTeleportPoints = new PlayerTeleportPoints();

        public Main()
        {
            Storage.Instance.Load(ref PlayerTeleportPoints);

            EventHandlers.Add("wlPlayerTeleportPoint:AddNewCommand", new Action<Player, string, Vector3, float>(AddNewCommand));
            EventHandlers.Add("wlPlayerTeleportPoint:RecordCommandUsage", 
                new Action<Player, string>(async (source, commandName) => await RecordCommandUsageAsync(source, commandName)));

            EventHandlers.Add("wlPlayerTeleportPoint:LoadTeleportPoint", new Action<Player, string>(LoadTeleportPoint));
        }

        /// <summary>
        /// 加载传送点到客户端 - 有则返回成功已经传送, 命令信息; 无则返回失败, null
        /// </summary>
        private void LoadTeleportPoint([FromSource] Player source, string commandName)
        {
            // 检查传送命令是否存在, 有则返回成功已经传送, 命令信息; 无则返回失败, null
            if (PlayerTeleportPoints.Has(commandName))
            {
                TriggerClientEvent(source, "wlPlayerTeleportPoint:SetTeleportPoint", commandName, true, 
                    PlayerTeleportPoints[commandName].Position,
                    PlayerTeleportPoints[commandName].Heading,
                    PlayerTeleportPoints[commandName].CreatorIdentifier);
            }
            else
            {
                TriggerClientEvent(source, "wlPlayerTeleportPoint:SetTeleportPoint", commandName, false, null);
            }
        }

        /// <summary>
        /// 获取玩家的license
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private string GetPlayerLicenseIdentifier([FromSource] Player source)
        {
            var licenseIdentifier = source.Identifiers["license"];
            return licenseIdentifier;
        }

        /// <summary>
        /// 新增传送点
        /// </summary>
        /// <param name="source"></param>
        /// <param name="commandName"></param>
        private void AddNewCommand([FromSource] Player source, string commandName, Vector3 position, float heading)
        {
            // 已有对应传送点, 则跳过添加
            if (PlayerTeleportPoints.ContainsKey(commandName)) return;
            // 写入服务期内存
            PlayerTeleportPoints[commandName] = new PlayerTeleportPoint(
                commandName, position, heading, GetPlayerLicenseIdentifier(source));
            // 写入数据库
            Storage.Instance.Save(commandName, position, heading, GetPlayerLicenseIdentifier(source));
            // 写入客户端
            TriggerClientEvent(source, "wlPlayerTeleportPoint:SetTeleportPoint", commandName, true,
                    PlayerTeleportPoints[commandName].Position,
                    PlayerTeleportPoints[commandName].Heading,
                    PlayerTeleportPoints[commandName].CreatorIdentifier);
        }

        // +1s
        private async Task RecordCommandUsageAsync([FromSource] Player source, string commandName)
        {
            var playerTeleportPoint = PlayerTeleportPoints[commandName];
            Storage.Instance.CommandCountPlusOne(commandName);
            await Storage.Instance.AddNewRecordAsync(
                GetPlayerLicenseIdentifier(source), playerTeleportPoint.CommandName, 
                playerTeleportPoint.Position, playerTeleportPoint.Heading,
                playerTeleportPoint.CreatorIdentifier, DateTime.UtcNow);
        }
    }
}

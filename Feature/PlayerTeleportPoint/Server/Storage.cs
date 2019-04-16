using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

using CitizenFX.Core;
using Vector3 = CitizenFX.Core.Vector3;

using Shared;


namespace Server
{
    public sealed class Storage
    {
        // Singleton
        private static readonly Lazy<Storage>
            lazy = new Lazy<Storage>(() => new Storage());
        public static Storage Instance { get { return lazy.Value; } }
        private Storage()
        {
            DbFactory = new OrmLiteConnectionFactory(
                $"Data Source={DbFilePath};Version=3;foreign keys = true", 
                SqliteDialect.Provider); // 创建数据库连接Factory
            RecordDbFactory = new OrmLiteConnectionFactory(
                $"Data Source={RecordDbFilePath};Version=3;", 
                SqliteDialect.Provider); // 创建Record数据库连接Factory
            CreateTableIfNotExists();
        }

        private const string DbFilePath = "wl/PlayerTeleportPoint.db";
        private OrmLiteConnectionFactory DbFactory;

        private const string RecordDbFilePath = "wl/PlayerTeleportPoint.record.db";
        private OrmLiteConnectionFactory RecordDbFactory;

        public class Table
        {
            // 传送点表
            public class PlayerTeleportPoint
            {
                [AutoIncrement]
                public int Id { get; set; }

                [Required, Unique]
                public string CommandName { get; set; }

                [Required, Default(0)]
                public float PositionX { get; set; }

                [Required, Default(0)]
                public float PositionY { get; set; }

                [Required, Default(0)]
                public float PositionZ { get; set; }

                [Required, Default(0)]
                public float Heading { get; set; }
                
                //[Required, References(typeof(Player))]
                public string Creator { get; set; }

                [Required, Default(0)]
                public int UseCount { get; set; }

                [Required, Default(OrmLiteVariables.SystemUtc)]
                public DateTime CreateTime { get; set; }

                [Required, Default(OrmLiteVariables.SystemUtc)]
                public DateTime LastUsedTime { get; set; }
            }
        }

        public class RecordTable
        {
            // 使用记录表
            public class PlayerTeleportPointRecord
            {
                [AutoIncrement]
                public int Id { get; set; }

                public string PlayerIdentifier { get; set; }

                public string CommandName { get; set; }
                public float PositionX { get; set; }
                public float PositionY { get; set; }
                public float PositionZ { get; set; }
                public float Heading { get; set; }
                public string CreatorIdentifier { get; set; }

                public DateTime UsedTime { get; set; }
            }
        }

        private void CreateTableIfNotExists()
        {
            // 建立数据库连接
            using (var db = DbFactory.Open())
            {
                // 创建表
                db.CreateTableIfNotExists<Table.PlayerTeleportPoint>();
            }
            // Record
            using (var db = RecordDbFactory.Open())
            {
                db.CreateTableIfNotExists<RecordTable.PlayerTeleportPointRecord>();
            }
        }

        public void Save(string commandName, Vector3 position, float heading, string playerIdentifier)
        {
            using (var db = DbFactory.Open())
            {
                // Save if not exists Table.PlayerTeleportPoint
                if (!db.Exists<Table.PlayerTeleportPoint>(new { CommandName = playerIdentifier }))
                {
                    db.Save(new Table.PlayerTeleportPoint
                    {
                        CommandName = commandName,
                        PositionX = position.X,
                        PositionY = position.Y,
                        PositionZ = position.Z,
                        Heading = heading,
                        Creator = playerIdentifier,
                    });
                }
                // Otherwise, skip
            }
        }

        // +1s
        public void CommandCountPlusOne(string commandName)
        {
            using (var db = DbFactory.Open())
            {
                var playerTeleportPoint = db.Single<Table.PlayerTeleportPoint>(new { CommandName = commandName });
                playerTeleportPoint.UseCount += 1;
                playerTeleportPoint.LastUsedTime = DateTime.UtcNow;
                db.Save(playerTeleportPoint);
            }
        }

        /// <summary>
        /// 新增一条传送命令使用记录
        /// </summary>
        /// <param name="playerIdentifier"></param>
        /// <param name="commandName"></param>
        /// <param name="position"></param>
        /// <param name="heading"></param>
        /// <param name="creatorIdentifier"></param>
        /// <param name="usedTime"></param>
        /// <returns></returns>
        public async Task AddNewRecordAsync(
            string playerIdentifier,
            string commandName, Vector3 position, float heading, string creatorIdentifier, DateTime usedTime)
        {
            using (var db = RecordDbFactory.Open())
            {
                await db.SaveAsync(new RecordTable.PlayerTeleportPointRecord
                {
                    PlayerIdentifier = playerIdentifier,
                    CommandName = commandName,
                    PositionX = position.X,
                    PositionY = position.Y,
                    PositionZ = position.Z,
                    Heading = heading,
                    CreatorIdentifier = creatorIdentifier,
                    UsedTime = usedTime,
                });
            }
        }

        /// <summary>
        /// 加载传送点
        /// </summary>
        /// <param name="playerTeleportPoints"></param>
        public void Load(ref PlayerTeleportPoints playerTeleportPoints)
        {
            using (var db = DbFactory.Open())
            {
                var query = db.From<Table.PlayerTeleportPoint>();
                var results = db.SelectLazy(query);
                foreach (var p in results)
                {
                    playerTeleportPoints[p.CommandName] = new PlayerTeleportPoint(
                        p.CommandName, new Vector3(p.PositionX, p.PositionY, p.PositionZ), p.Heading, p.Creator);
                }
            }
        }
    }
}

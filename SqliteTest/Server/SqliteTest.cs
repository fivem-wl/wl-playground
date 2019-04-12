using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;



namespace Server
{

    // 传送点表
    public class TeleportLocations
    {
        [AutoIncrement]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required, Default(0)]
        public float PositionX { get; set; }

        [Required, Default(0)]
        public float PositionY { get; set; }

        [Required, Default(0)]
        public float PositionZ { get; set; }

        [Required, Default(0)]
        public float Heading { get; set; }

        [Reference]
        public Player Creator { get; set; }

        [Required, Default(0)]
        public int Count { get; set; }

        [Required, Default(OrmLiteVariables.SystemUtc)]
        public DateTime CreateTime { get; set; }

        [Required, Default(OrmLiteVariables.SystemUtc)]
        public DateTime LastUsedTime { get; set; }
    }

    // 玩家信息表
    public class Player
    {
        [PrimaryKey]
        public string Id { get; set; }
    }

    public class SqliteTest : BaseScript
    {
        private const string DbFilePath = "wl/SqliteTest.db";
        // 创建数据库连接Factory
        private OrmLiteConnectionFactory dbFactory = new OrmLiteConnectionFactory($"{DbFilePath}", SqliteDialect.Provider);
        //private OrmLiteConnectionFactory dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);

        public SqliteTest()
        {

            Debug.WriteLine("TEst");

            // 建立数据库连接
            using (var db = dbFactory.Open())
            {
                // 创建表
                db.CreateTableIfNotExists<Player>();
                db.CreateTableIfNotExists<TeleportLocations>();

                // 写入数据库
                var creator = new Player { Id = GetGameTimer().ToString() };
                db.Insert(creator);
                db.Insert(new TeleportLocations
                {
                    Id = (int)GetGameTimer(),
                    Name = "ldz",
                    Creator = creator,
                    LastUsedTime = DateTime.UtcNow,
                });
            }

            Debug.WriteLine("TEst2");

            RegisterCommand("SqliteTest", new Action<int, List<object>, string>((source, args, raw) =>
            {
                // 建立数据库连接
                using (var db = dbFactory.Open())
                {
                    db.CreateTableIfNotExists<Player>();
                    db.CreateTableIfNotExists<TeleportLocations>();

                    // 写入数据
                    var creator = new Player { Id = GetGameTimer().ToString() };
                    db.Insert(creator);
                    db.Insert(new TeleportLocations
                    {
                        Id = (int)GetGameTimer(),
                        Name = GetGameTimer().ToString(),
                        Creator = creator,
                        LastUsedTime = DateTime.UtcNow,
                    });
                }
            }), false);

            Debug.WriteLine("TEst3");

            RegisterCommand("sqlitet", new Action<int, List<object>, string>((source, args, raw) =>
            {
                Debug.WriteLine("sqlitets");
            }), false);

        }
    }
}

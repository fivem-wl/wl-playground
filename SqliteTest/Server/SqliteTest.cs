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

    public class Player
    {
        [PrimaryKey]
        public string Id { get; set; }
    }

    public class SqliteTest : BaseScript
    {
        private const string DbFilePath = "wl/SqliteTest.db";
        private OrmLiteConnectionFactory dbFactory = new OrmLiteConnectionFactory($"{DbFilePath}", SqliteDialect.Provider);
        //private OrmLiteConnectionFactory dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);

        public SqliteTest()
        {

            Debug.WriteLine("TEst");

            using (var db = dbFactory.Open())
            {
                db.CreateTableIfNotExists<Player>();
                db.CreateTableIfNotExists<TeleportLocations>();

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
                using (var db = dbFactory.Open())
                {
                    db.CreateTableIfNotExists<Player>();
                    db.CreateTableIfNotExists<TeleportLocations>();

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

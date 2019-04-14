using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.UI.Screen;
using static CitizenFX.Core.Native.API;
using Extensions;

using Client;


namespace wlFreeroamClient
{
    public static class DefaultStatus
    {
        public const int MaxHealth = 200;
        public const int MaxArmor = 200;
    }


    class GameEnvironment : BaseScript
    {

        public GameEnvironment()
        {

            var playerHandle = Game.Player.Handle;

            #region 调整人物能力
            // SetEntityInvincible(Game.PlayerPed.Handle, false);
            // 满血满甲
            Game.PlayerPed.MaxHealth = DefaultStatus.MaxHealth;
            Game.PlayerPed.Health = DefaultStatus.MaxHealth;
            Game.Player.MaxArmor = DefaultStatus.MaxArmor;
            Game.PlayerPed.Armor = DefaultStatus.MaxArmor;
            // 无限体力
            StatSetInt((uint)GetHashKey("MP0_STAMINA"), 100, true);
            // 快速奔跑
            SetRunSprintMultiplierForPlayer(playerHandle, 1.49f);
            // 快速游泳
            SetSwimMultiplierForPlayer(playerHandle, 1.49f);
            // 更多
            StatSetInt((uint)GetHashKey("MP0_SHOOTING_ABILITY"), 100, true);        // Shooting
            StatSetInt((uint)GetHashKey("MP0_STRENGTH"), 100, true);                // Strength
            StatSetInt((uint)GetHashKey("MP0_STEALTH_ABILITY"), 100, true);         // Stealth
            StatSetInt((uint)GetHashKey("MP0_FLYING_ABILITY"), 100, true);          // Flying
            StatSetInt((uint)GetHashKey("MP0_WHEELIE_ABILITY"), 100, true);         // Driving
            StatSetInt((uint)GetHashKey("MP0_LUNG_CAPACITY"), 100, true);           // Lung Capacity
            StatSetFloat((uint)GetHashKey("MP0_PLAYER_MENTAL_STATE"), 100f, true);    // Mental State
            #endregion

            #region 关闭PvP
            NetworkSetFriendlyFireOption(false);
            SetCanAttackFriendly(Game.PlayerPed.Handle, false, false);
            #endregion
            
            Tick += PlayerSelfEveryTick;
            Tick += NoWeaponDropsWhenDeadTick;
            Tick += SetCopCustomWeaponNAccuracyTick;

            Tick += ModeAutoRegeneration.RegenerateAsync;
            Tick += TimeSyncer.SyncTimeAsync;

            AreaAkinaClearer AreaAkinaClearer = new AreaAkinaClearer();
            Tick += AreaAkinaClearer.FreezeDrivingPedVehicleAsync;
            Tick += AreaAkinaClearer.SetPedVehicleDensityToZeroEveryFrame;
            AreaAkinaClearer.ShowVisualRange();

            // EventHandlers.Add("DamageEvents:PedKilledByPlayer", new Action<int, int, uint, bool>(PlayerRefillByKillPed));
            //EventHandlers.Add("MissionMostWantedDelivery:OnPlayerMissionRunning", new Action<int, int>(HandleMissionRunningEvent));
            //EventHandlers.Add("MissionMostWantedDelivery:OnPlayerMissionStart", new Action<int, int>(HandleMissionStartEvent));
            //EventHandlers.Add("MissionMostWantedDelivery:OnPlayerMissionStop", new Action<int, string, int>(HandleMissionStopEvent));

        }

        // 时间
        private static class TimeSyncer
        {
            private static int MinuteClockSpeed = 1000;
            private static int CurrentMinutes = 0;
            private static int CurrentHours = 6;

            public static async Task SyncTimeAsync()
            {
                var now = GetGameTimer();

                CurrentMinutes = now / 1000 % 60;
                CurrentHours = now / 1000 / 60 % 24;
                NetworkOverrideClockTime(CurrentHours, CurrentMinutes, 0);

                await Delay(MinuteClockSpeed);
            }
        }

        // 呼吸回血
        private static class ModeAutoRegeneration
        {
            private const int TICK_INTERVAL = 250;
            // 打断持续时间
            private const int INTERRUPT_DURATION = 1000 * 5;
            // 恢复速率 ~= (1000/REGENERATION_INTERVAL)*REGENRRATION_RATE 每秒;
            private const int REGENERATION_INTERVAL = 250;
            private const int REGENRRATION_RATE = 2;


            private static float previousHealth = 0;
            private static float previousArmor = 0;
            private static int previousInterruptTime;
            private static int previousRegerationTime;

            public static async Task RegenerateAsync()
            {
                var player = Game.Player;
                var playerPed = Game.PlayerPed;
                var currentHealth = playerPed.Health;
                var currentArmor = playerPed.Armor;
                var now = GetGameTimer();

                if (currentHealth < previousHealth || currentArmor < previousArmor) previousInterruptTime = now;
                previousHealth = currentHealth;
                previousArmor = currentArmor;

                if (now - previousInterruptTime > INTERRUPT_DURATION)
                {
                    if (now - previousRegerationTime > REGENERATION_INTERVAL)
                    {
                        if (currentHealth < DefaultStatus.MaxHealth)
                        {
                            var newHealth = (
                                currentHealth + REGENRRATION_RATE <= DefaultStatus.MaxHealth ? currentHealth + REGENRRATION_RATE : DefaultStatus.MaxHealth);
                            //previousHealth = newHealth;
                            // set health
                            playerPed.MaxHealth = DefaultStatus.MaxHealth;
                            playerPed.Health = newHealth;
                        }
                        if (currentArmor < DefaultStatus.MaxArmor)
                        {
                            var newArmor = (
                                currentArmor + REGENRRATION_RATE <= DefaultStatus.MaxArmor ? currentArmor + REGENRRATION_RATE : DefaultStatus.MaxArmor);
                            player.MaxArmor = DefaultStatus.MaxArmor;
                            //previousArmor = newArmor;
                            // set armor
                            playerPed.Armor = newArmor;
                        }

                    }
                    previousRegerationTime = now;
                }

                await Delay(TICK_INTERVAL);
            }

        }

        #region 玩家自定义
        private async Task PlayerSelfEveryTick()
        {
            if (IsControlPressed(0, (int)Control.Sprint))
            {
                // 超级跳
                // SetSuperJumpThisFrame(Game.Player.Handle);
            }
            await Task.FromResult(0);
        }
        #endregion

        #region 死亡不掉落武器装备
        private Dictionary<int, bool> pedNoWeaponDropsWhenDead = new Dictionary<int, bool>();

        private async Task NoWeaponDropsWhenDeadTick()
        {
            foreach (var ped in World.GetAllPeds())
            {
                var pedHandle = ped.Handle;
                if (!pedNoWeaponDropsWhenDead.GetValueOrDefault(pedHandle, false))
                {
                    if (!IsEntityDead(pedHandle))
                    {
                        SetPedDropsWeaponsWhenDead(pedHandle, false);
                        pedNoWeaponDropsWhenDead[pedHandle] = true;
                    }
                }
            }
            await Delay(500);
        }
        #endregion

        #region 警察部队配备随机装备, 并统一射击准确率
        private readonly List<WeaponHash> CopWeaponList = new List<WeaponHash>
        {
            WeaponHash.APPistol,
            WeaponHash.SawnOffShotgun,
            WeaponHash.AssaultRifleMk2,
            WeaponHash.PistolMk2,
            // WeaponHash.MarksmanPistol,
            WeaponHash.HeavyPistol,
            // WeaponHash.CombatPistol,
            WeaponHash.MachinePistol,
        };
        private Random CopWeaponRand = new Random();
        private Dictionary<int, bool> copCustomWeaponNAccuracy = new Dictionary<int, bool>();
        private async Task SetCopCustomWeaponNAccuracyTick()
        {
            foreach (var ped in World.GetAllPeds())
            {
                var pedHandle = ped.Handle;
                if (IsPedAPlayer(pedHandle)) continue;
                if (!copCustomWeaponNAccuracy.GetValueOrDefault(pedHandle, false))
                {
                    var pedType = GetPedType(pedHandle);
                    // [Player,1|Male,4|Female,5|Cop,6|Human,26|SWAT,27|Animal,28|Army,29]
                    if (pedType == 6 || pedType == 27 || pedType == 29)
                    {
                        if (!IsEntityDead(pedHandle))
                        {
                            var weaponHash = CopWeaponList[CopWeaponRand.Next(CopWeaponList.Count)];
                            RemoveAllPedWeapons(pedHandle, true);
                            GiveWeaponToPed(pedHandle, (uint)weaponHash, 9999, false, true);
                            SetPedAccuracy(pedHandle, 5);
                            copCustomWeaponNAccuracy[pedHandle] = true;
                        }
                    }
                }
            }
            await Delay(1000);
        }
        #endregion

        #region 玩家击杀回复血量, 护甲
        //private void PlayerRefillByKillPed(int ped, int player, uint weaponHash, bool isMeleeDamage)
        //{
        //    var playerPed = GetPlayerPed(player);
        //    if (isMeleeDamage)
        //    {
        //        //SetEntityMaxHealth(playerPed, 200);
        //        SetEntityHealth(playerPed, 200);
        //        SetPedArmour(playerPed, 100);
        //    }
        //    else
        //    {
        //        //SetEntityMaxHealth(playerPed, 200);
        //        SetEntityHealth(playerPed, GetEntityHealth(playerPed) + 25);
        //        SetPedArmour(playerPed, GetPedArmour(playerPed) + 25);
        //    }
        //}
        #endregion

        #region 去除秋名山区域内的Dynamic Ped, Vehicle
        private class AreaAkinaClearer
        {
            private Dictionary<int, bool> EntityFreezedInAreaAkina = new Dictionary<int, bool>();
            private float Radius1 = 1300f;
            private Vector3 Pos1 = new Vector3(-3093f, 5419f, 0);
            private Vector3 Pos2 = new Vector3(-2495f, 6455f, 0);
            private Vector3 Pos3 = new Vector3(-1745f, 7247f, 0);
            private Vector3 Pos4 = new Vector3(-1229f, 7773f, 0);

            public void ShowVisualRange()
            {
                var blip1 = World.CreateBlip(Pos1, Radius1);
                blip1.Color = BlipColor.Yellow;
                blip1.Alpha = 64;
                var blip2 = World.CreateBlip(Pos2, Radius1);
                blip2.Color = BlipColor.Yellow;
                blip2.Alpha = 64;
                var blip3 = World.CreateBlip(Pos3, Radius1);
                blip3.Color = BlipColor.Yellow;
                blip3.Alpha = 64;
                var blip4 = World.CreateBlip(Pos4, Radius1);
                blip4.Color = BlipColor.Yellow;
                blip4.Alpha = 64;
            }

            public async Task SetPedVehicleDensityToZeroEveryFrame()
            {
                var position = Game.PlayerPed.Position;
                var rxr = Radius1 * Radius1;
                if (position.DistanceToSquared2D(Pos1) <= rxr ||
                    position.DistanceToSquared2D(Pos2) <= rxr ||
                    position.DistanceToSquared2D(Pos3) <= rxr ||
                    position.DistanceToSquared2D(Pos4) <= rxr)
                {
                    SetPedDensityMultiplierThisFrame(0);
                    SetVehicleDensityMultiplierThisFrame(0);

                    if (Game.Player.WantedLevel > 0)
                    {
                        Game.PlayerPed.Health = -100;
                        Notify.Alert("被通缉的时候禁止进入秋名山区域!");
                    }

                }
                await Task.FromResult(0);
            }

            public async Task FreezeDrivingPedVehicleAsync()
            {
                var rxr = Radius1 * Radius1;
                foreach (var ped in World.GetAllPeds())
                {
                    if (ped.IsPlayer) continue;
                    if (!EntityFreezedInAreaAkina.GetValueOrDefault(ped.Handle, false))
                    {
                        var position = ped.Position;
                        if (position.DistanceToSquared2D(Pos1) <= rxr ||
                        position.DistanceToSquared2D(Pos2) <= rxr ||
                        position.DistanceToSquared2D(Pos3) <= rxr ||
                        position.DistanceToSquared2D(Pos4) <= rxr)
                        {
                            Vehicle vehicle = null;
                            if (ped.IsInVehicle())
                            {
                                vehicle = ped.LastVehicle;
                            }
                            var randInt = new Random().Next(15);
                            ped.Position += 5f + (float)randInt;
                            FreezeEntityPosition(ped.Handle, true);
                            EntityFreezedInAreaAkina[ped.Handle] = true;
                            if (!(vehicle is null)) vehicle.Delete();
                        }
                    }

                }
                await Delay(1000);
            }
        }
        #endregion

        private void HandleMissionRunningEvent(int player, int remainTime)
        {
            TriggerEvent("chat:addMessage", new
            {
                color = new[] { 255, 0, 0 },
                args = new[]
                {
                    "[HandleMissionRunningEvent]",
                    $" {player}, {remainTime}"
                }
            });
        }

        private void HandleMissionStartEvent(int player, int duration)
        {
            TriggerEvent("chat:addMessage", new
            {
                color = new[] { 255, 0, 0 },
                args = new[]
                {
                    "[HandleMissionStartEvent]",
                    $" {player}, {duration}"
                }
            });
        }

        private void HandleMissionStopEvent(int player, string reason, int reamineTime)
        {
            TriggerEvent("chat:addMessage", new
            {
                color = new[] { 255, 0, 0 },
                args = new[]
                {
                    "[HandleMissionStopEvent]",
                    $" {player}, {reason}, {reamineTime}"
                }
            });
        }

    }
}

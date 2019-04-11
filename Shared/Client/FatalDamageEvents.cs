using System;
using System.Collections.Generic;
using System.Text;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace Client
{
    class FatalDamageEvents : BaseScript
    {
        public delegate void PlayerKillPedEvent(Ped victim, bool isMeleeDamage, uint weaponInfoHash, int damageTypeFlag);
        public static event PlayerKillPedEvent OnPlayerKillPed;

        public delegate void PlayerDeadEvent();
        public static event PlayerDeadEvent OnPlayerDead;

        /// <summary>
        /// Handle game event CEventNetworkEntityDamage,
        /// Useful for indicating entity damage/died/destroyed.
        /// </summary>
        /// <param name="victim">victim</param>
        /// <param name="attacker">attacker</param>
        /// <param name="arg2">Unknown</param>
        /// <param name="isDamageFatal">Is damage fatal to entity. or victim died/destroyed.</param>
        /// <param name="weaponInfoHash">Probably related to common.rpf/data/ai => Item type = "CWeaponInfo"</param>
        /// <param name="arg5">Unknown</param>
        /// <param name="arg6">Unknown</param>
        /// <param name="arg7">Unknown, might be int</param>
        /// <param name="arg8">Unknown, might be int</param>
        /// <param name="isMeleeDamage">Is melee damage</param>
        /// <param name="damageTypeFlag">0 for peds, 116 for the body of a vehicle, 93 for a tire, 120 for a side window, 121 for a rear window, 122 for a windscreen, etc</param>
        private void HandleCEventNetworkEntityDamaged(
            Entity victim, Entity attacker, int arg2, bool isDamageFatal, uint weaponInfoHash,
            int arg5, int arg6, object arg7, object arg8, bool isMeleeDamage,
            int damageTypeFlag)
        {
            if (isDamageFatal && victim is Ped p1 && attacker is Ped p2)
            {
                if (p2 == Game.PlayerPed)
                {
                    OnPlayerKillPed?.Invoke(p1, isMeleeDamage, weaponInfoHash, damageTypeFlag);
                }
            }
            if (isDamageFatal && victim == Game.PlayerPed)
            {
                OnPlayerDead?.Invoke();
            }
        }

        public FatalDamageEvents()
        {
            EventHandlers.Add("gameEventTriggered", new Action<string, List<object>>((string eventName, List<object> args) =>
            {
                if (eventName == "CEventNetworkEntityDamage")
                {
                    Entity victim = Entity.FromHandle(int.Parse(args[0].ToString()));
                    Entity attacker = Entity.FromHandle(int.Parse(args[1].ToString()));
                    bool isDamageFatal = int.Parse(args[3].ToString()) == 1;
                    uint weaponInfoHash = (uint)int.Parse(args[4].ToString());
                    bool isMeleeDamage = int.Parse(args[9].ToString()) != 0;
                    int damageTypeFlag = int.Parse(args[10].ToString());
                    HandleCEventNetworkEntityDamaged(
                        victim, attacker, int.Parse(args[2].ToString()), isDamageFatal, weaponInfoHash,
                        int.Parse(args[5].ToString()), int.Parse(args[6].ToString()), args[7], args[8], isMeleeDamage,
                        damageTypeFlag);
                }
            }));
        }
    }
}

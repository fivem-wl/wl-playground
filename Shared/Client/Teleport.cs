using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;


namespace Client
{

    /// <summary>
    /// 玩家传送.
    /// Port from - <seealso cref="https://github.com/TomGrobbe/vMenu"/>
    /// </summary>
    public static class Teleport
    {
        /// <summary>
        /// Teleport to the specified <see cref="pos"/>.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="safeModeDisabled"></param>
        /// <returns></returns>
        public static async Task TeleportToCoords(Vector3 pos, bool safeModeDisabled = false)
        {
            if (!safeModeDisabled)
            {
                // Is player in a vehicle and the driver? Then we'll use that to teleport.
                var veh = GetVehicle();
                bool inVehicle() => veh != null && veh.Exists() && Game.PlayerPed == veh.Driver;

                bool vehicleRestoreVisibility = inVehicle() && veh.IsVisible;
                bool pedRestoreVisibility = Game.PlayerPed.IsVisible;

                // Freeze vehicle or player location and fade out the entity to the network.
                if (inVehicle())
                {
                    veh.IsPositionFrozen = true;
                    if (veh.IsVisible)
                    {
                        NetworkFadeOutEntity(veh.Handle, true, false);
                    }
                }
                else
                {
                    ClearPedTasksImmediately(Game.PlayerPed.Handle);
                    Game.PlayerPed.IsPositionFrozen = true;
                    if (Game.PlayerPed.IsVisible)
                    {
                        NetworkFadeOutEntity(Game.PlayerPed.Handle, true, false);
                    }
                }

                // Fade out the screen and wait for it to be faded out completely.
                DoScreenFadeOut(500);
                while (!IsScreenFadedOut())
                {
                    await BaseScript.Delay(0);
                }

                // This will be used to get the return value from the groundz native.
                float groundZ = 850.0f;

                // Bool used to determine if the groundz coord could be found.
                bool found = false;

                // Loop from 950 to 0 for the ground z coord, and take away 25 each time.
                for (float zz = 950.0f; zz >= 0f; zz -= 25f)
                {
                    float z = zz;
                    // The z coord is alternating between a very high number, and a very low one.
                    // This way no matter the location, the actual ground z coord will always be found the fastest.
                    // If going from top > bottom then it could take a long time to reach the bottom. And vice versa.
                    // By alternating top/bottom each iteration, we minimize the time on average for ANY location on the map.
                    if (zz % 2 != 0)
                    {
                        z = 950f - zz;
                    }

                    // Request collision at the coord. I've never actually seen this do anything useful, but everyone keeps telling me this is needed.
                    // It doesn't matter to get the ground z coord, and neither does it actually prevent entities from falling through the map, nor does
                    // it seem to load the world ANY faster than without, but whatever.
                    RequestCollisionAtCoord(pos.X, pos.Y, z);

                    // Request a new scene. This will trigger the world to be loaded around that area.
                    NewLoadSceneStart(pos.X, pos.Y, z, pos.X, pos.Y, z, 50f, 0);

                    // Timer to make sure things don't get out of hand (player having to wait forever to get teleported if something fails).
                    int tempTimer = GetGameTimer();

                    // Wait for the new scene to be loaded.
                    while (IsNetworkLoadingScene())
                    {
                        // If this takes longer than 1 second, just abort. It's not worth waiting that long.
                        if (GetGameTimer() - tempTimer > 1000)
                        {
                            Log.Debug("Waiting for the scene to load is taking too long (more than 1s). Breaking from wait loop.");
                            break;
                        }

                        await BaseScript.Delay(0);
                    }

                    // If the player is in a vehicle, teleport the vehicle to this new position.
                    if (inVehicle())
                    {
                        SetEntityCoords(veh.Handle, pos.X, pos.Y, z, false, false, false, true);
                    }
                    // otherwise, teleport the player to this new position.
                    else
                    {
                        SetEntityCoords(Game.PlayerPed.Handle, pos.X, pos.Y, z, false, false, false, true);
                    }

                    // Reset the timer.
                    tempTimer = GetGameTimer();

                    // Wait for the collision to be loaded around the entity in this new location.
                    while (!HasCollisionLoadedAroundEntity(Game.PlayerPed.Handle))
                    {
                        // If this takes too long, then just abort, it's not worth waiting that long since we haven't found the real ground coord yet anyway.
                        if (GetGameTimer() - tempTimer > 1000)
                        {
                            Log.Debug("Waiting for the collision is taking too long (more than 1s). Breaking from wait loop.");
                            break;
                        }
                        await BaseScript.Delay(0);
                    }

                    // Check for a ground z coord.
                    found = GetGroundZFor_3dCoord(pos.X, pos.Y, z, ref groundZ, false);

                    // If we found a ground z coord, then teleport the player (or their vehicle) to that new location and break from the loop.
                    if (found)
                    {
                        Log.Debug($"Ground coordinate found: {groundZ}");
                        if (inVehicle())
                        {
                            SetEntityCoords(veh.Handle, pos.X, pos.Y, groundZ, false, false, false, true);

                            // We need to unfreeze the vehicle because sometimes having it frozen doesn't place the vehicle on the ground properly.
                            veh.IsPositionFrozen = false;
                            veh.PlaceOnGround();
                            // Re-freeze until screen is faded in again.
                            veh.IsPositionFrozen = true;
                        }
                        else
                        {
                            SetEntityCoords(Game.PlayerPed.Handle, pos.X, pos.Y, groundZ, false, false, false, true);
                        }
                        break;
                    }

                    // Wait 10ms before trying the next location.
                    await BaseScript.Delay(10);
                }

                // If the loop ends but the ground z coord has not been found yet, then get the nearest vehicle node as a fail-safe coord.
                if (!found)
                {
                    var safePos = pos;
                    GetNthClosestVehicleNode(pos.X, pos.Y, pos.Z, 0, ref safePos, 0, 0, 0);

                    // Notify the user that the ground z coord couldn't be found, so we will place them on a nearby road instead.
                    Notify.Alert("Could not find a safe ground coord. Placing you on the nearest road instead.");
                    Log.Debug("Could not find a safe ground coord. Placing you on the nearest road instead.");

                    // Teleport vehicle, or player.
                    if (inVehicle())
                    {
                        SetEntityCoords(veh.Handle, safePos.X, safePos.Y, safePos.Z, false, false, false, true);
                        veh.IsPositionFrozen = false;
                        veh.PlaceOnGround();
                        veh.IsPositionFrozen = true;
                    }
                    else
                    {
                        SetEntityCoords(Game.PlayerPed.Handle, safePos.X, safePos.Y, safePos.Z, false, false, false, true);
                    }
                }

                // Once the teleporting is done, unfreeze vehicle or player and fade them back in.
                if (inVehicle())
                {
                    if (vehicleRestoreVisibility)
                    {
                        NetworkFadeInEntity(veh.Handle, true);
                        if (!pedRestoreVisibility)
                        {
                            Game.PlayerPed.IsVisible = false;
                        }
                    }
                    veh.IsPositionFrozen = false;
                }
                else
                {
                    if (pedRestoreVisibility)
                    {
                        NetworkFadeInEntity(Game.PlayerPed.Handle, true);
                    }
                    Game.PlayerPed.IsPositionFrozen = false;
                }

                // Fade screen in and reset the camera angle.
                DoScreenFadeIn(500);
                SetGameplayCamRelativePitch(0.0f, 1.0f);
            }

            // Disable safe teleporting and go straight to the specified coords.
            else
            {
                RequestCollisionAtCoord(pos.X, pos.Y, pos.Z);

                // Teleport directly to the coords without trying to get a safe z pos.
                if (Game.PlayerPed.IsInVehicle() && GetVehicle().Driver == Game.PlayerPed)
                {
                    SetEntityCoords(GetVehicle().Handle, pos.X, pos.Y, pos.Z, false, false, false, true);
                }
                else
                {
                    SetEntityCoords(Game.PlayerPed.Handle, pos.X, pos.Y, pos.Z, false, false, false, true);
                }
            }
        }


#region GetVehicle from specified player id (if not specified, return the vehicle of the current player)
        /// <summary>
        /// Returns the current or last vehicle of the current player.
        /// </summary>
        /// <param name="lastVehicle"></param>
        /// <returns></returns>
        public static Vehicle GetVehicle(bool lastVehicle = false)
        {
            if (lastVehicle)
            {
                return Game.PlayerPed.LastVehicle;
            }
            else
            {
                if (Game.PlayerPed.IsInVehicle())
                {
                    return Game.PlayerPed.CurrentVehicle;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the current or last vehicle of the selected ped.
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="lastVehicle"></param>
        /// <returns></returns>
        public static Vehicle GetVehicle(Ped ped, bool lastVehicle = false)
        {
            if (lastVehicle)
            {
                return ped.LastVehicle;
            }
            else
            {
                if (ped.IsInVehicle())
                {
                    return ped.CurrentVehicle;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the current or last vehicle of the selected player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="lastVehicle"></param>
        /// <returns></returns>
        public static Vehicle GetVehicle(Player player, bool lastVehicle = false)
        {
            if (lastVehicle)
            {
                return player.Character.LastVehicle;
            }
            else
            {
                if (player.Character.IsInVehicle())
                {
                    return player.Character.CurrentVehicle;
                }
            }
            return null;
        }
#endregion

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;

namespace Client
{
    public delegate void MissionObjectiveStartedEvent();
    public delegate void MissionObjectiveStoppedEvent();
    public delegate void MissionObjectiveAccomplishedEvent();
    
    public delegate void MissionSubObjectiveStartedEvent();
    public delegate void MissionSubObjectiveStoppedEvent();
    public delegate void MissionSubObjectiveAccomplishedEvent();

    /// <summary>
    /// Abstract: Mission objective
    /// Contains: Objective, Sub objective
    /// </summary>
    public abstract class MissionObjective
    {

        public event MissionObjectiveStartedEvent OnMissionObjectiveStart;
        public event MissionObjectiveStoppedEvent OnMissionObjectiveStop;
        public event MissionObjectiveAccomplishedEvent OnMissionObjectiveAccomplish;

        public event MissionSubObjectiveStartedEvent OnMissionSubObjectiveStart;
        public event MissionSubObjectiveStoppedEvent OnMissionSubObjectiveStop;
        public event MissionSubObjectiveAccomplishedEvent OnMissionSubObjectiveAccomplish;

        public virtual int AccomplishCheckInterval { get; } = 500 * 1;
        public virtual int SubAccomplishCheckInterval { get; } = 500 * 1;

        public bool IsActivated { get; protected set; } = false;

        public bool IsAccomplished { get; protected set; } = false;
        public bool IsSubAccomplished { get; protected set; } = false;

        /// <summary>
        /// Check whether player meets requirement of accomplishment or not
        /// </summary>
        public abstract bool IsPlayerMeetRequirement { get; }
        /// <summary>
        /// Check whether player meets requirement of accomplishment or not
        /// </summary>
        public abstract bool IsPlayerMeetSubRequirement { get; }

        public string Guid { get; } = System.Guid.NewGuid().ToString();
        public string Name { get; }
        public bool IsShowRoute { get; } = false;

        protected string HelpText { get; }
        protected string SubHelpText { get; }

        /// <summary>
        /// Mission location blip
        /// </summary>
        protected Blip Blip { get; set; }
        /// <summary>
        /// Indicate 
        /// </summary>
        protected bool IsDrawMarker { get; set; } = false;
        protected MissionMarker MissionMarker { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public MissionObjective(string name, bool isShowRoute, string helpText, string subHelpText)
            : base()
        {
            IsActivated = false;
            IsAccomplished = false;
            IsSubAccomplished = false;

            HelpText = helpText;
            SubHelpText = subHelpText;

            Name = name;
            IsShowRoute = isShowRoute;
        }

        /// <summary>
        /// Reset objective
        /// </summary>
        public virtual void Reset()
        {
            IsActivated = true;
            IsAccomplished = false;
            IsSubAccomplished = false;
        }

        public virtual void Start()
        {
            IsActivated = true;
            IsAccomplished = false;
            IsSubAccomplished = false;

            StartDrawing();

            OnMissionObjectiveStart?.Invoke();
        }

        /// <summary>
        /// Stop objective
        /// </summary>
        public virtual void Stop()
        {
            IsActivated = false;
            IsAccomplished = false;
            IsSubAccomplished = false;

            StopDrawing();

            OnMissionObjectiveStop?.Invoke();
        }

        /// <summary>
        /// Accomplish objective check async
        /// </summary>
        /// <returns>Task</returns>
        public async Task HookOnTick_AccomplishCheckAsync()
        {
            if (IsActivated && !IsAccomplished && IsPlayerMeetRequirement)
            {
                IsAccomplished = true;

                OnMissionObjectiveAccomplish?.Invoke();
            }
            await BaseScript.Delay(AccomplishCheckInterval);
        }

        /// <summary>
        /// Accomplish subobjective check async
        /// </summary>
        public async Task HookOnTick_AccomplishSubObjectiveCheckAsync()
        {
            if (IsActivated && !IsAccomplished && IsPlayerMeetSubRequirement)
            {
                IsSubAccomplished = true;

                OnMissionSubObjectiveAccomplish?.Invoke();
            }
            await BaseScript.Delay(SubAccomplishCheckInterval);
        }

        public async Task HookOnTick_DrawOnEveryFrameAsync()
        {
            DrawOnEveryFrame();

            await Task.FromResult(0);
        }

        public abstract void StartDrawing();
        public abstract void StopDrawing();
        public abstract void DrawOnEveryFrame();
    }
    
    /// <summary>
    /// Mission objective: Enter location.
    /// </summary>
    public sealed class MissionObjectiveEnterLocation : MissionObjective
    {
        public Location Location { get; }

        /// <summary>
        /// Player in location and pressed/released Control.Context
        /// </summary>
        public override bool IsPlayerMeetRequirement
        {
            get { return Location.IsPlayerInside && IsControlPressed(0, (int)Control.Context); }
        }

        public override bool IsPlayerMeetSubRequirement
        {
            get { return false; }
        }

        public MissionObjectiveEnterLocation(string name, bool isShowRoute, string helpText, string subHelpText, Location location)
            : base(name, isShowRoute, helpText, subHelpText)
        {
            Location = location;
        }

        public override void StartDrawing()
        {
            if (!(Blip is null)) Blip.Delete();
            Blip = World.CreateBlip(Location.Position);
            Blip.Color = BlipColor.Yellow;
            Blip.IsShortRange = true;
            Blip.ShowRoute = IsShowRoute;
            Blip.Sprite = BlipSprite.Rampage;
            Blip.Name = Name;

            MissionMarker = new MissionMarker(Location.Position, new Vector3(Location.Radius));
            IsDrawMarker = true;
        }

        public override void StopDrawing()
        {
            if (!(Blip is null)) Blip.Delete();

            IsDrawMarker = false;
        }

        public override void DrawOnEveryFrame()
        {
            if (IsActivated && !IsAccomplished)
            {
                // Draw marker
                if (IsDrawMarker && MissionMarker.IsPlayerInDrawDistance)
                {
                    MissionMarker.DrawThisFrame();
                }
                // Draw hint
                if (Location.IsPlayerInside)
                {
                    Screen.DisplayHelpTextThisFrame(HelpText);
                }
            }
        }

    }

    /// <summary>
    /// Procedure: 1. Kill cops. 2. Goto victim position. 3. Repeat until meets Capture requirement.
    /// Limitation: 1. Victim and player should both in restrict location.
    /// </summary>
    public sealed class MissionObjectiveKillAndCaptureInLocation : MissionObjective
    {

        public Location RestrictLocation { get; }
        public Location CaptureLocation { get; private set; }

        public int CaptureRequirement { get; } = 30;
        public int CaptureCount { get; private set; } = 0;

        public override bool IsPlayerMeetRequirement
        {
            get { return CaptureCount >= CaptureRequirement; }
        }

        public override bool IsPlayerMeetSubRequirement
        {
            get { return CaptureLocation.IsPlayerInside && IsControlPressed(0, (int)Control.Context); }
        }

        public MissionObjectiveKillAndCaptureInLocation(
            string name, bool isShowRoute, string helpText, string subHelpText, Location restrictLocation, int captureRequirement)
            : base(name, isShowRoute, helpText, subHelpText)
        {
            RestrictLocation = restrictLocation;
            CaptureRequirement = captureRequirement;
            CaptureLocation = new Location(Vector3.Zero, 0f);
        }

        public override void Reset()
        {
            base.Reset();

            CaptureLocation = new Location(Vector3.Zero, 0f);
            CaptureCount = 0;
        }

        public override void Start()
        {
            base.Start();
            
            FatalDamageEvents.OnPlayerKillPed += UpdateCaptureLocation;
            OnMissionSubObjectiveAccomplish += UpdateCaptureCount;
            OnMissionSubObjectiveAccomplish += HintSubObjectiveStatus;
        }

        public override void Stop()
        {
            base.Stop();

            CaptureLocation = new Location(Vector3.Zero, 0f);
            CaptureCount = 0;
            
            FatalDamageEvents.OnPlayerKillPed -= UpdateCaptureLocation;
            OnMissionSubObjectiveAccomplish -= UpdateCaptureCount;
        }

        public override void StartDrawing()
        {
            if (!(Blip is null)) Blip.Delete();
            Blip = World.CreateBlip(RestrictLocation.Position, RestrictLocation.Radius);
            Blip.Alpha = 128;
            Blip.Color = BlipColor.Yellow;
            Blip.Name = Name;
            BeginTextCommandSetBlipName("STRING");
            AddTextComponentString(Name);
            EndTextCommandSetBlipName(Blip.Handle);
            Blip.IsFlashing = true;
            Blip.IsShortRange = true;
            Blip.Rotation = (int)RestrictLocation.Radius;
        }

        public override void StopDrawing()
        {
            if (!(Blip is null)) Blip.Delete();

            IsDrawMarker = false;
        }

        /// <summary>
        /// Update Capture location if confirmed victim killed by player, both in restrict location.
        /// </summary>
        private void UpdateCaptureLocation(Ped victim, bool isMeleeDamage, uint weaponInfoHash, int damageTypeFlag)
        {
            var position = victim.Position;
            var victimType = GetPedType(victim.Handle);
            // [Player,1|Male,4|Female,5|Cop,6|Human,26|SWAT,27|Animal,28|Army,29]
            if (victimType == 6 || victimType == 27 || victimType == 29)
            {
                if (RestrictLocation.IsPositionInside(position) && RestrictLocation.IsPlayerInside)
                {
                    CaptureLocation = new Location(position, 5f);
                    MissionMarker = new MissionMarker(position, new Vector3(5f));
                    IsDrawMarker = true;
                }
            }
        }

        private void UpdateCaptureCount()
        {
            CaptureCount += 1;
            CaptureLocation = new Location(Vector3.Zero, 0f);
            IsDrawMarker = false;
        }

        private void HintSubObjectiveStatus()
        {
            SetTextEntry_2("STRING");
            AddTextComponentString($"继续~y~占领~s~警察据点 - {CaptureCount}/{CaptureRequirement}");
            DrawSubtitleTimed(1000 * 30, true);

            PlaySoundFrontend(-1, "Boss_Message_Orange", "GTAO_Boss_Goons_FM_Soundset", false);
        }

        public override void DrawOnEveryFrame()
        {
            if (IsActivated && !IsAccomplished)
            {
                // Draw marker
                if (IsDrawMarker && CaptureLocation.IsPlayerInDrawDistance)
                {
                    MissionMarker.DrawThisFrame();
                }
                // Draw hint
                if (!RestrictLocation.IsPlayerInside)
                {
                    Screen.DisplayHelpTextThisFrame("请立即返回任务场地!");
                }
                else if (CaptureLocation.IsPlayerInside)
                {
                    Screen.DisplayHelpTextThisFrame(SubHelpText);
                }
            }
        }
    }

}

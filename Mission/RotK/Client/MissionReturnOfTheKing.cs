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
    /// <summary>
    /// 任务目标信息
    /// </summary>
    public readonly struct MissionObjectiveInfo
    {
        public Type MissionObjectiveType { get; }
        public Location Location { get; }
        public Duration Duration { get; }

        public MissionObjectiveInfo(Type missionObjectiveType, Location location, Duration duration)
        {
            MissionObjectiveType = missionObjectiveType;
            Location = location;
            Duration = duration;
        }
    }

    /// <summary>
    /// 任务信息
    /// </summary>
    public readonly struct MissionInfo
    {
        public Identifier Identifier { get; }
        public List<MissionObjectiveInfo> MissionObjectivesInfo { get; }

        public MissionInfo(Identifier identifier, List<MissionObjectiveInfo> missionObjectivesInfo)
        {
            Identifier = identifier;
            MissionObjectivesInfo = missionObjectivesInfo;
        }
    }
    
    /// <summary>
    /// 任务: 王者归来
    /// 流程: 触发(到达目的地) -> 到达目的地 -> 占领据点
    /// </summary>
    public sealed class MissionReturnOfTheKing
    {
        //public delegate void MissionStartedEvent(string title, string name, Vector3 position);
        public delegate void MissionStoppedEvent(string reason);
        public delegate void MissionObjectiveStartedEvent(
                MissionObjective missionObjective, int objectiveIndex, bool isFirstObjective, bool isLastObjective);
        public delegate void MissionObjectiveStoppedEvent(
                MissionObjective missionObjective, int objectiveIndex, bool isFirstObjective, bool isLastObjective, string reason);
        public delegate void MissionScheduledEvent(string title, string name, Vector3 position);

        //public static event MissionStartedEvent OnMissionStart;
        public static event MissionStoppedEvent OnMissionStop;
        public static event MissionObjectiveStartedEvent OnMissionObjectiveStart;
        public static event MissionObjectiveStoppedEvent OnMissionObjectiveStop;
        public static event MissionScheduledEvent OnMissionSchedule;

        public static Interval CheckInterval { get; } = 1000 * 1;
        public static bool IsScheduled { get { return CurrentMissionObjectiveIndex == 0; } }
        public static bool IsRunning { get { return CurrentMissionObjectiveIndex >= 1; } }

        public static MissionInfo MissionInfo { get; set; }
        public static MissionObjective CurrentMissionObjective { get; private set; }
        public static int CurrentMissionObjectiveIndex { get; private set; } = -1;


        static MissionReturnOfTheKing() { }

        public static void StartCurrentObjective()
        {
            var index = CurrentMissionObjectiveIndex;
            var objectiveType = MissionInfo.MissionObjectivesInfo[index].MissionObjectiveType;

            switch (true)
            {
                case true when objectiveType.IsAssignableFrom(typeof(MissionObjectiveEnterLocation)):
                    // 首个任务目标不自动设置导航点
                    var isShowRoute = index != 0;
                    var helpText = index == 0 ? "按住 ~INPUT_CONTEXT~ 开始任务 - ~y~王者归来~s~" : "按住 ~INPUT_CONTEXT~ 开始占领据点任务";
                    var location = MissionInfo.MissionObjectivesInfo[index].Location;
                    CurrentMissionObjective = new MissionObjectiveEnterLocation("王者归来", isShowRoute, helpText, "", location);
                    break;
                case true when objectiveType.IsAssignableFrom(typeof(MissionObjectiveKillAndCaptureInLocation)):
                    CurrentMissionObjective = new MissionObjectiveKillAndCaptureInLocation(
                        "王者归来", false, "", "按住 ~INPUT_CONTEXT~ 占领据点",
                        MissionInfo.MissionObjectivesInfo[index].Location, 20);
                    break;
            }


            CurrentMissionObjective.Start();
            CurrentMissionObjective.OnMissionObjectiveAccomplish += StartNextObjective;

            OnMissionObjectiveStart?.Invoke(
                CurrentMissionObjective, index, index == 0, index >= MissionInfo.MissionObjectivesInfo.Count);

        }

        public static void StopCurrentObjective(string reason)
        {
            // temp index.
            var index = CurrentMissionObjectiveIndex;

            CurrentMissionObjective.Stop();
            CurrentMissionObjective.OnMissionObjectiveAccomplish -= StartNextObjective;
            CurrentMissionObjectiveIndex = -1;

            OnMissionObjectiveStop?.Invoke(
                CurrentMissionObjective, index, index == 0, index >= MissionInfo.MissionObjectivesInfo.Count, reason);
        }

        public static void Schedule()
        {
            CurrentMissionObjectiveIndex = 0;

            StartCurrentObjective();

            FatalDamageEvents.OnPlayerDead += StopMissionWhenPlayerDead;

            OnMissionSchedule?.Invoke(MissionInfo.Identifier.Title, MissionInfo.Identifier.Name, MissionInfo.MissionObjectivesInfo[0].Location.Position);
        }

        public static void Stop(string reason)
        {
            var index = CurrentMissionObjectiveIndex;

            StopCurrentObjective(reason);

            if (index == 0) FatalDamageEvents.OnPlayerDead -= StopMissionWhenPlayerDead;

            OnMissionStop?.Invoke(reason);
        }

        private static void StartNextObjective()
        {
            if (CurrentMissionObjectiveIndex >= MissionInfo.MissionObjectivesInfo.Count - 1)
            {
                Stop("finish");
            }
            else
            {
                // temp index, in case Stop would set index to -1
                var index = CurrentMissionObjectiveIndex;

                StopCurrentObjective("finish");

                CurrentMissionObjectiveIndex = index + 1;
                StartCurrentObjective();
            }
            switch (CurrentMissionObjectiveIndex)
            {
                case 0: break;
                case 1:
                    SetTextEntry_2("STRING");
                    AddTextComponentString($"前往任务标记点 - ~y~{MissionInfo.Identifier.Name}~s~");
                    DrawSubtitleTimed(1000 * 60 * 10, true);
                    break;
                case 2:
                    SetTextEntry_2("STRING");
                    AddTextComponentString($"击杀附近的~r~警察~s~并~y~占领~s~他们的位置");
                    DrawSubtitleTimed(1000 * 60 * 10, true);
                    break;
            }
        }

        private static void StopMissionWhenPlayerDead()
        {
            Stop("dead");

            SetNotificationTextEntry("STRING");
            AddTextComponentString("任务失败, 再接再厉");
            SetNotificationMessage("CHAR_LJT", "CHAR_LJT", false, 2, "???", "王者归来");
            DrawNotification(false, true);
            PlaySoundFrontend(GetSoundId(), "BASE_JUMP_PASSED", "HUD_AWARDS", false);
        }

    }

    public sealed class Main : BaseScript
    {
        /// <summary>
        /// Mission info list
        /// </summary>
        private static readonly List<MissionInfo> MissionsInfo = new List<MissionInfo>
        {
            new MissionInfo(
                new Identifier("王者归来", "公牛直播中心"),
                new List<MissionObjectiveInfo>
                {
                    new MissionObjectiveInfo(
                        typeof(MissionObjectiveEnterLocation), 
                        new Location(new Vector3(461f, -3013f, 5f), 5f), 1000 * 60),
                    new MissionObjectiveInfo(
                        typeof(MissionObjectiveEnterLocation), 
                        // new Location(new Vector3(563f, -3026f, 5f), 5f), 1000 * 60),
                        new Location(new Vector3(-2237.56f, 249.68f, 175f), 5f), 1000 * 60),
                    new MissionObjectiveInfo(
                        typeof(MissionObjectiveKillAndCaptureInLocation),
                        new Location(new Vector3(-2247.03f, 267.03f, 174.62f), 200f), 1000 * 60),
                        //new Location(new Vector3(563f, -3026f, 5f), 100f), 1000 * 60 * 60),

                }),
             //new MissionInfo(
             //   new Identifier("RotK", "2"),
             //   new List<MissionObjectiveInfo>
             //   {
             //       new MissionObjectiveInfo(
             //           typeof(MissionObjectiveEnterLocation), 
             //           new Location(new Vector3(458f, -2949f, 5f), 5f), 1000 * 60),
             //       new MissionObjectiveInfo(
             //           typeof(MissionObjectiveEnterLocation), 
             //           new Location(new Vector3(563f, -3026f, 5f), 5f), 1000 * 60),
             //       new MissionObjectiveInfo(
             //           typeof(MissionObjectiveKillAndCaptureInLocation), 
             //           new Location(new Vector3(563f, -3026f, 5f), 100f), 1000 * 60 * 60),
             //   }),
        };

        private static int ScheduleInterval { get; } = 1000 * 60 * 8;
        private static int LastScheduleTime { get; set; } = GetGameTimer();
        private static bool IsReachScheduleInterval
        {
            get { return GetGameTimer() - LastScheduleTime >= ScheduleInterval; }
        }
        
        public Main()
        {
            
            Tick += ScheduleMissionAsync;
            Tick += KeepMostWantedOnMission;
            Tick += MissionEnvironment;

            MissionReturnOfTheKing.OnMissionObjectiveStart += (
                MissionObjective missionObjective, int objectiveIndex, bool isFirstObjective, bool isLastObjective) =>
            {
                Tick += missionObjective.HookOnTick_AccomplishCheckAsync;
                Tick += missionObjective.HookOnTick_AccomplishSubObjectiveCheckAsync;
                Tick += missionObjective.HookOnTick_DrawOnEveryFrameAsync;
            };

            MissionReturnOfTheKing.OnMissionObjectiveStop += (
                MissionObjective missionObjective, int objectiveIndex, bool isFirstObjective, bool isLastObjective, string reason) =>
            {
                Tick -= missionObjective.HookOnTick_AccomplishCheckAsync;
                Tick -= missionObjective.HookOnTick_AccomplishSubObjectiveCheckAsync;
                Tick -= missionObjective.HookOnTick_DrawOnEveryFrameAsync;
                
                // hint on first mission
                if (isFirstObjective) PlaySoundFrontend(GetSoundId(), "5s", "MP_MISSION_COUNTDOWN_SOUNDSET", false);
            };

            MissionReturnOfTheKing.OnMissionObjectiveStop += (
                MissionObjective missionObjective, int objectiveIndex, bool isFirstObjective, bool isLastObjective, string reason) =>
            {
                PlaySoundFrontend(GetSoundId(), "BASE_JUMP_PASSED", "HUD_AWARDS", false);
            };


            MissionReturnOfTheKing.OnMissionSchedule += HintOnMissionSchedule;
            
            MissionReturnOfTheKing.OnMissionStop += HintOnMissionFinish;
            MissionReturnOfTheKing.OnMissionStop += RescheduleWhenMissionStop;

        }

        public async Task MissionEnvironment()
        {
            await Task.FromResult(0);
        }

        private async Task KeepMostWantedOnMission()
        {
            if (MissionReturnOfTheKing.IsRunning)
            {
                SetPlayerWantedLevel(Game.Player.Handle, 5, false);
                SetPlayerWantedLevelNow(Game.Player.Handle, false);
            }
            await Delay(1000 * 10);
        }

        private void HintOnMissionSchedule(string title, string name, Vector3 position)
        {
            SetNotificationTextEntry("CELL_EMAIL_BCON");
            foreach (string s in Screen.StringToArray(
                "胜者为王标准差事: 在被通缉下赶往差事地点, 并占领区域"))
            {
                AddTextComponentSubstringPlayerName(s);
            }
            // AddTextComponentString("立即前往标记地点(限时未添加)");
            SetNotificationMessage("CHAR_LJT", "CHAR_LJT", false, 0, title, name);
            DrawNotification(false, true);
            PlaySoundFrontend(GetSoundId(), "BASE_JUMP_PASSED", "HUD_AWARDS", false);
        }

        private void HintOnMissionFinish(string reason)
        {
            if (reason == "finish")
            {
                SetNotificationTextEntry("STRING");
                AddTextComponentString("任务结束, 获得奖励$????.");
                SetNotificationMessage("CHAR_LJT", "CHAR_LJT", false, 2, "???", "王者归来");
                DrawNotification(false, true);
                PlaySoundFrontend(GetSoundId(), "BASE_JUMP_PASSED", "HUD_AWARDS", false);

                // ClearWantedLevelOnMissionFinish
                SetPlayerWantedLevel(Game.Player.Handle, 0, false);
                SetPlayerWantedLevelNow(Game.Player.Handle, false);
            }

        }

        private void RescheduleWhenMissionStop(string reason)
        {
            IsFastSchedule = true;
        }

        /// <summary>
        /// 相同时间内(根据ScheduleInterval), 将会返回同一个MissionInfo
        /// </summary>
        /// <returns>MissionInfo</returns>
        private static MissionInfo GetMissionInfoSynced()
        {
            var now = GetGameTimer();
            var index = now / ScheduleInterval % MissionsInfo.Count;
            return MissionsInfo[index];
        }

        private int PlayerConnectedTime { get; } = GetGameTimer();
        private bool IsFastSchedule { get; set; } = true;
        private bool IsFirstTimeCalled { get; set; } = true;
        private async Task ScheduleMissionAsync()
        {
            // 延迟第一次调用
            if (IsFirstTimeCalled)
            {
                IsFirstTimeCalled = false;
                await Delay(1000 * 30);
                return;
            }
            if (IsFastSchedule || IsReachScheduleInterval)
            {
                IsFastSchedule = false;
                if (!MissionReturnOfTheKing.IsScheduled && !MissionReturnOfTheKing.IsRunning)
                {
                    var missionInfo = GetMissionInfoSynced();
                    MissionReturnOfTheKing.MissionInfo = missionInfo;
                    MissionReturnOfTheKing.Schedule();
                }
            }

            await Delay(1000 * 30);
        }


    }
}

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
    //public class MissionScheduler
    //{

    //    private static readonly Lazy<MissionScheduler>
    //        lazy = new Lazy<MissionScheduler>(() => new MissionScheduler());
    //    public static MissionScheduler Instace { get { return lazy.Value; } }

    //    public int RescheduleInterval { get; }

    //    private List<MissionInfo> MissionsInfo { get; }
    //    private ScheduledMission ScheduledMission { get; }

    //    public MissionScheduler(int rescheduleInterval, List<MissionInfo> missionsInfo, ScheduledMission scheduledMission)
    //    {
    //        RescheduleInterval = rescheduleInterval;
    //        MissionsInfo = missionsInfo;
    //        ScheduledMission = scheduledMission;
    //    }

    //    public void Add(MissionInfo missionInfo)
    //    {
    //        if (!MissionsInfo.Contains(missionInfo)) MissionsInfo.Add(missionInfo);
    //    }

    //    public void Remove(MissionInfo missionInfo)
    //    {
    //        if (MissionsInfo.Contains(missionInfo)) MissionsInfo.Remove(missionInfo);
    //    }

    //    public List<MissionInfo> List()
    //    {
    //        return MissionsInfo;
    //    }

    //    /// <summary>
    //    /// 相同时间内(根据RescheduleInterval), 将会返回同一个MissionInfo
    //    /// </summary>
    //    /// <returns>MissionInfo</returns>
    //    public MissionInfo GetSynced()
    //    {
    //        var now = GetGameTimer();
    //        var index = now / RescheduleInterval % MissionsInfo.Count;
    //        return MissionsInfo[index];
    //    }

    //    /// <summary>
    //    /// 计划任务
    //    /// </summary>
    //    /// <returns></returns>
    //    public async Task ScheduleAsync()
    //    {
    //        ScheduledMission.Set(GetSynced());
    //        await BaseScript.Delay(RescheduleInterval);
    //    }
    //}
}

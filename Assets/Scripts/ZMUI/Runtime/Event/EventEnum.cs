using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EventEnum 
{
    LoginSuccess,
    ScencProgressUpdate,
    CameraActiveChange,
    /// <summary>
    /// 当前角色信息更新(切换角色)
    /// </summary>
    CurRoleInfoUpdate,

    /// <summary>
    /// 任务完成
    /// </summary>
    TaskComplete,
    /// <summary>
    /// 任务列表为空
    /// </summary>
    TaskListIsEmpty,

    TaskRoleActionBoxUpdate,
    /// <summary>
    /// 角色心情更新（当前摄像机激活的角色）
    /// </summary>
    RoleMoodUpdate,


    TimeSecondUpdate,
    TimeMinuteUpdate,
    TimeHourUpdate,
    TimeDayUpdate,

    TimeDayPartUpdate,

    WeatherUpdate,

    PlayBackRecordingUpdate,
    PlayBackPlayingUpdate,



}

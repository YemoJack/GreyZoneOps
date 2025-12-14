using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 事件派发中心
/// 由逻辑层调用，
/// 代替直接交互，进行解耦
/// </summary>
public class EventControl  
{
    /// <summary>
    /// 委托事件
    /// </summary>
    /// <param name="data"></param>
    public delegate void EventHandler(object[] data);
    /// <summary>
    /// 事件派发注册字典
    /// </summary>
    private static Dictionary<EventEnum, List<EventHandler>> mEventDic = new Dictionary<EventEnum, List<EventHandler>>();

    /// <summary>
    /// 注册事件
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="eventHandler"></param>
    public static void AddEvent(EventEnum eventType,EventHandler eventHandler)
    {
        if (!mEventDic.ContainsKey(eventType))
        {
            mEventDic.Add(eventType,new List<EventHandler>());
        }
        if (!mEventDic[eventType].Contains(eventHandler))
        {
            mEventDic[eventType].Add(eventHandler);
        }
    }
    /// <summary>
    /// 移除事件
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="eventHandler"></param>
    public static void RemoveEvent(EventEnum eventType, EventHandler eventHandler)
    {
        if (mEventDic.ContainsKey(eventType))
        {
            if (mEventDic[eventType].Contains(eventHandler))
            {
                mEventDic[eventType].Remove(eventHandler);
            }
        }
    }


    /// <summary>
    /// 分发事件
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="data"></param>
    public static void DispensEvent(EventEnum eventType,object[] data=null)
    {
        List<EventHandler> eventList = null;
        if (mEventDic.ContainsKey(eventType))
        {
            eventList = mEventDic[eventType];

            for (int i = 0; i < eventList.Count; i++)
            {
                eventList[i]?.Invoke(data);
            }
        }
        //else
        //{
        //    Debug.Log("没有注册该事件" + eventType.ToString());
        //}
    }
}

using System;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;



public interface IResLoader : IUtility
{
    /// <summary>
    /// 初始化资源加载器(快速初始化,不下载资源)
    /// </summary>
    /// <param name="mode">启动模式</param>
    /// <param name="onProgress">进度回调(进度0-1.0, 阶段描述)</param>
    UniTask InitLoader(EPlayMode mode);

    /// <summary>
    /// 更新远程资源(检查版本、下载资源)
    /// </summary>
    /// <param name="onProgress">进度回调(进度0-1.0, 阶段描述)</param>
    UniTask<bool> UpdateRes(Action<float, string> onProgress = null);
    /// <summary>
    ///同步加载
    /// </summary>
    /// <param name="path"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T LoadSync<T>(string path) where T : Object;
    void UnloadSync(Object res);
    /// <summary>
    /// 异步加载
    /// </summary>
    /// <param name="path"></param>
    /// <param name="onLoaded">加载回调</param>
    /// <param name="checkRemote">是否检查远程包(热更资源)</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    UniTask LoadAsync<T>(string path, Action<T> onLoaded, bool checkRemote = false) where T : Object;
    /// <summary>
    /// 异步加载
    /// </summary>
    /// <param name="path"></param>
    /// <param name="checkRemote">是否检查远程包(热更资源)</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    UniTask<T> LoadAsync<T>(string path, bool checkRemote = false) where T : Object;
    void UnloadAsync(Object res);
}
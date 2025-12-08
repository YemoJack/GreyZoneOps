using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using YooAsset;
using static UnityEngine.Rendering.ReloadAttribute;
using Object = UnityEngine.Object;

/// <summary>
/// 本地资源加载器（无远程、无热更）
/// </summary>
public class ResLoaderYoo : IResLoader
{
    private const string packageName = "DefaultPackage";
    private ResourcePackage package;


    private const string PackageNameRemote = "RemotePackage";
    private ResourcePackage remotePackage;

    private string _packageVersion = "1.0.0";

    /// <summary>
    /// 所有的加载句柄（用于引用计数管理）
    /// </summary>
    private readonly Dictionary<string, AssetHandle> _assetHandles =
        new Dictionary<string, AssetHandle>();


    public async UniTask InitLoader(EPlayMode playMode)
    {

        // 初始化 YooAssets
        YooAssets.Initialize();

       
        // 创建资源包裹类
        package = YooAssets.TryGetPackage(packageName);
        if (package == null)
            package = YooAssets.CreatePackage(packageName);

        // 编辑器下的模拟模式
        InitializationOperation initializationOperation = null;
        if (playMode == EPlayMode.EditorSimulateMode)
        {
            var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
            var packageRoot = buildResult.PackageRootDirectory;
            var createParameters1 = new EditorSimulateModeParameters();
            createParameters1.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
            initializationOperation = package.InitializeAsync(createParameters1);
        }

        // 单机运行模式
        else if (playMode == EPlayMode.OfflinePlayMode)
        {
            var createParameters2 = new OfflinePlayModeParameters();
            createParameters2.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            initializationOperation = package.InitializeAsync(createParameters2);
        }


        await initializationOperation;

        // 如果初始化失败弹出提示界面
        if (initializationOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogWarning($"{initializationOperation.Error}");
            await UniTask.CompletedTask;
        }



        var operation = package.RequestPackageVersionAsync();
        await operation;

        if (operation.Status != EOperationStatus.Succeed)
        {
            Debug.LogWarning(operation.Error);
            await UniTask.CompletedTask;
        }
        else
        {
            Debug.Log($"Request package version : {operation.PackageVersion}");
            _packageVersion = operation.PackageVersion;
        }



        var updateManifest = package.UpdatePackageManifestAsync(_packageVersion, timeout: 10);
        await updateManifest;
        if (updateManifest.Status == EOperationStatus.Succeed)
        {
           
            Debug.Log($"更新资源版本清单成功:{_packageVersion}");
        }
        else
        {
            
            Debug.LogError($"更新资源版本清单失败({_packageVersion}):{updateManifest.Error}");
            await UniTask.CompletedTask;
        }




        YooAssets.SetDefaultPackage(package);


        // 联机运行模式
        if (playMode != EPlayMode.HostPlayMode)
        {
            Debug.Log("初始化本地资源成功");
            return;
        }

        remotePackage = YooAssets.CreatePackage(PackageNameRemote);
        string defaultHostServer = GetHostServerURL();
        string fallbackHostServer = GetHostServerURL();
        IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
        var createParameters = new HostPlayModeParameters();
        createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
        createParameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
       
        initializationOperation = remotePackage.InitializeAsync(createParameters);
        await initializationOperation;
        if (initializationOperation.Status == EOperationStatus.Succeed)
        {
           
            Debug.Log("初始化远端资源成功");
        }
        else
        {
           
            Debug.LogError($"初始化远端资源失败:{initializationOperation.Error}");
        }


    }

    public async UniTask<bool> UpdateRes(Action<float, string> onProgress = null)
    {
        // 如果 Remote 包未初始化,直接返回
        if (remotePackage == null)
        {
            onProgress?.Invoke(1f, "无需更新资源");
            Debug.Log("Remote package not initialized, skip update");
            return true;
        }

        // 0-20%: 更新资源版本号
        onProgress?.Invoke(0f, "检查资源版本...");
        var updateVersion = remotePackage.RequestPackageVersionAsync(timeout: 10);
        await updateVersion;
        if (updateVersion.Status == EOperationStatus.Succeed)
        {
            _packageVersion = updateVersion.PackageVersion;
            onProgress?.Invoke(0.2f, $"资源版本: {_packageVersion}");
            Debug.Log($"更新资源版本成功:{_packageVersion}");
        }
        else
        {
            onProgress?.Invoke(1f, "资源版本检查失败");
#if UNITY_EDITOR
            Debug.LogError($"更新资源版本失败({_packageVersion}):{updateVersion.Error}");
#endif
            return false;
        }

        // 20-40%: 更新资源清单
        onProgress?.Invoke(0.2f, "更新资源清单...");
        var updateManifest = remotePackage.UpdatePackageManifestAsync(_packageVersion, timeout: 10);
        await updateManifest;
        if (updateManifest.Status == EOperationStatus.Succeed)
        {
            onProgress?.Invoke(0.4f, "资源清单更新完成");
            Debug.Log($"更新资源版本清单成功:{_packageVersion}");
        }
        else
        {
            onProgress?.Invoke(1f, "资源清单更新失败");
            Debug.LogError($"更新资源版本清单失败({_packageVersion}):{updateManifest.Error}");
            return false;
        }

        // 40-100%: 下载资源
        onProgress?.Invoke(0.4f, "检查需要下载的资源...");
        int downloadingMaxNum = 10;
        int failedTryAgain = 3;
        var downloader =  remotePackage.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);

        //没有需要下载的资源
        if (downloader.TotalDownloadCount == 0)
        {
            onProgress?.Invoke(1f, "无需下载资源");
            Debug.Log("No need to download resources");
            return true;
        }

        // 需要下载的文件总数和总大小
        int totalDownloadCount = downloader.TotalDownloadCount;
        long totalDownloadBytes = downloader.TotalDownloadBytes;
        float totalSizeMB = totalDownloadBytes / (1024f * 1024f);
        onProgress?.Invoke(0.4f, $"需要下载 {totalDownloadCount} 个文件 ({totalSizeMB:F2}MB)");
        Debug.Log($"Need to download resources: {totalDownloadCount} total size: {totalDownloadBytes}");

        // 开始下载
        downloader.DownloadUpdateCallback = (downloadUpdateData) =>
        {
            // 40-100% 映射到下载进度
            float downloadProgress = (float)downloadUpdateData.CurrentDownloadCount / downloadUpdateData.TotalDownloadCount;
            float overallProgress = 0.4f + downloadProgress * 0.6f;
            float currentSizeMB = downloadUpdateData.CurrentDownloadBytes / (1024f * 1024f);
            onProgress?.Invoke(overallProgress, $"下载资源中... {currentSizeMB:F2}MB / {totalSizeMB:F2}MB");
        };

        downloader.BeginDownload();
        await downloader;

        if (downloader.Status == EOperationStatus.Succeed)
        {
            onProgress?.Invoke(1f, "资源下载完成");
            Debug.Log("Download resources complete");
            return true;
        }
        else
        {
            onProgress?.Invoke(1f, "资源下载失败");
            Debug.LogError($"Download resources failed: {downloader.Error}");
            return false;
        }
    }

    public T LoadSync<T>(string path) where T : Object
    {
        return YooAssets.LoadAssetSync<T>(path).AssetObject as T;
    }

    public void UnloadSync(Object res)
    {

    }

    /// <summary>
    /// 从指定包加载资源(支持回退)
    /// </summary>
    private async UniTask<AssetHandle> LoadAssetFromPackage<T>(string path, bool checkRemote) where T : Object
    {
        AssetHandle handle = null;

        // 如果需要检查远程包且远程包已初始化
        if (checkRemote && remotePackage != null)
        {
            try
            {
                // 先尝试从 Remote 包加载
                handle = remotePackage.LoadAssetAsync<T>(path);
                await handle.ToUniTask();

                if (handle.Status == EOperationStatus.Succeed)
                {
                    Debug.Log($"从 Remote 包加载资源成功: {path}");
                    return handle;
                }
                else
                {
                    Debug.LogWarning($"从 Remote 包加载资源失败,尝试 Default 包: {path}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Remote 包加载异常,回退到 Default 包: {path}, Error: {e.Message}");
            }
        }

        // 从 Default 包加载
        handle = YooAssets.LoadAssetAsync<T>(path);
        await handle.ToUniTask();
        return handle;
    }


    public async UniTask LoadAsync<T>(string path, Action<T> onLoaded, bool checkRemote = false) where T : Object
    {
        try
        {
            AssetHandle load;
            if (!_assetHandles.TryGetValue(path, out load))
            {
                load = await LoadAssetFromPackage<T>(path, checkRemote);
            }

            if (load.AssetObject is T res)
            {
                _assetHandles[path] = load;
                onLoaded?.Invoke(res);
            }
            else
            {
                onLoaded?.Invoke(null);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Load '{path}' Err:{e}");
            onLoaded?.Invoke(null);
        }
    }

    public async UniTask<T> LoadAsync<T>(string path, bool checkRemote = false) where T : Object
    {
        try
        {
            AssetHandle load;
            if (!_assetHandles.TryGetValue(path, out load))
            {
                load = await LoadAssetFromPackage<T>(path, checkRemote);
            }

            if (load.AssetObject is T res)
            {
                _assetHandles[path] = load;
                return res;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Load '{path}' Err:{e}");
        }
        return null;
    }

    public void UnloadAsync(Object res)
    {

    }





    /// <summary>
    /// 获取资源服务器地址
    /// </summary>
    private string GetHostServerURL()
    {
        //string hostServerIP = "http://10.0.2.2"; //安卓模拟器地址
        string hostServerIP = "http://127.0.0.1";
        string appVersion = "v1.0";

#if UNITY_EDITOR
        if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
            return $"{hostServerIP}/CDN/Android/{appVersion}";
        else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
            return $"{hostServerIP}/CDN/IPhone/{appVersion}";
        else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
            return $"{hostServerIP}/CDN/WebGL/{appVersion}";
        else
            return $"{hostServerIP}/CDN/PC/{appVersion}";
#else
        if (Application.platform == RuntimePlatform.Android)
            return $"{hostServerIP}/CDN/Android/{appVersion}";
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
            return $"{hostServerIP}/CDN/IPhone/{appVersion}";
        else if (Application.platform == RuntimePlatform.WebGLPlayer)
            return $"{hostServerIP}/CDN/WebGL/{appVersion}";
        else
            return $"{hostServerIP}/CDN/PC/{appVersion}";
#endif
    }


    /// <summary>
    /// 远端资源地址查询服务类
    /// </summary>
    private class RemoteServices : IRemoteServices
    {
        private readonly string _defaultHostServer;
        private readonly string _fallbackHostServer;

        public RemoteServices(string defaultHostServer, string fallbackHostServer)
        {
            _defaultHostServer = defaultHostServer;
            _fallbackHostServer = fallbackHostServer;
        }
        string IRemoteServices.GetRemoteMainURL(string fileName)
        {
            return $"{_defaultHostServer}/{fileName}";
        }
        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            return $"{_fallbackHostServer}/{fileName}";
        }
    }


   
}
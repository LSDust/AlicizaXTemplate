# Resources 模块

Resources 模块是框架对 YooAsset 的资源服务封装，负责资源包初始化、资源查询、同步/异步加载、实例化 GameObject、资源引用缓存、下载器创建和资源回收。

源码位置：

- `Client/Packages/com.alicizax.unity.framework/Runtime/Resource`

> 注意：模块名是 Resources 文档名，但运行时代码命名空间和服务接口为 `AlicizaX.Resource.Runtime.IResourceService`。

## 使用前提

场景中的框架根节点需要挂载：

- `ObjectPoolComponent`
- `ResourceComponent`

`ResourceComponent` 会注册 `IResourceService` 并初始化 YooAsset 默认包。资源服务内部会通过 `IObjectPoolService` 创建资源对象池，所以 ObjectPool 必须先注册。

```csharp
using AlicizaX;
using AlicizaX.Resource.Runtime;

IResourceService resources = AppServices.Require<IResourceService>();
```

## 初始化资源包

`ResourceComponent.Awake()` 会设置默认包名、运行模式、下载参数，并调用 `Initialize()`。之后业务流程中通常需要初始化包：

```csharp
using AlicizaX;
using AlicizaX.Resource.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class ResourceInitExample : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        IResourceService resources = AppServices.Require<IResourceService>();

        bool succeed = await resources.InitPackageAsync(
            packageName: "DefaultPackage",
            hostServerURL: "https://cdn.example.com/Android",
            fallbackHostServerURL: "https://fallback-cdn.example.com/Android");

        Debug.Log($"Init package succeed: {succeed}");
    }
}
```

编辑器模拟、离线运行、联机运行等模式由 `ResourceComponent.PlayMode` 控制。

## 检查资源是否存在

```csharp
using AlicizaX;
using AlicizaX.Resource.Runtime;
using UnityEngine;

public sealed class ResourceCheckExample : MonoBehaviour
{
    private void Start()
    {
        IResourceService resources = AppServices.Require<IResourceService>();

        string location = "Assets/Bundles/UI/Login.prefab";
        if (!resources.CheckLocationValid(location))
        {
            Debug.LogError($"Invalid location: {location}");
            return;
        }

        HasAssetResult result = resources.HasAsset(location);
        Debug.Log($"Asset state: {result}");
    }
}
```

`HasAsset` 返回值用于区分：

- `NotExist`：资源不存在。
- `AssetOnline`：资源存在但需要从远端下载。
- `AssetOnDisk`：资源已经在本地可用。

## 同步加载资源

```csharp
using AlicizaX;
using AlicizaX.Resource.Runtime;
using UnityEngine;

public sealed class LoadTextureExample : MonoBehaviour
{
    private Texture2D _icon;

    private void Start()
    {
        IResourceService resources = AppServices.Require<IResourceService>();
        _icon = resources.LoadAsset<Texture2D>("Assets/Bundles/Icons/icon_start.png");
    }

    private void OnDestroy()
    {
        if (_icon != null && AppServices.TryGet<IResourceService>(out var resources))
        {
            resources.UnloadAsset(_icon);
            _icon = null;
        }
    }
}
```

同步加载适合启动阶段或确认资源已经在本地的小资源。普通业务流程优先使用异步加载。

## 异步加载资源

```csharp
using System.Threading;
using AlicizaX;
using AlicizaX.Resource.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class LoadSpriteExample : MonoBehaviour
{
    private CancellationTokenSource _cts;
    private Sprite _avatar;

    private async UniTaskVoid OnEnable()
    {
        _cts = new CancellationTokenSource();

        IResourceService resources = AppServices.Require<IResourceService>();
        _avatar = await resources.LoadAssetAsync<Sprite>(
            "Assets/Bundles/UI/avatar_default.png",
            _cts.Token);
    }

    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        if (_avatar != null && AppServices.TryGet<IResourceService>(out var resources))
        {
            resources.UnloadAsset(_avatar);
            _avatar = null;
        }
    }
}
```

也可以使用回调式加载：

```csharp
using AlicizaX;
using AlicizaX.Resource.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class LoadWithCallbacksExample : MonoBehaviour
{
    private void Start()
    {
        IResourceService resources = AppServices.Require<IResourceService>();

        var callbacks = new LoadAssetCallbacks(
            loadAssetSuccessCallback: OnLoadSuccess,
            loadAssetFailureCallback: OnLoadFailure,
            loadAssetUpdateCallback: OnLoadUpdate);

        resources.LoadAssetAsync(
            location: "Assets/Bundles/UI/Login.prefab",
            priority: 0,
            loadAssetCallbacks: callbacks,
            userData: this).Forget();
    }

    private void OnLoadSuccess(string assetName, object asset, float duration, object userData)
    {
        Debug.Log($"Load success: {assetName}, duration={duration:F3}");
    }

    private void OnLoadFailure(string assetName, LoadResourceStatus status, string errorMessage, object userData)
    {
        Debug.LogError($"Load failure: {assetName}, status={status}, error={errorMessage}");
    }

    private void OnLoadUpdate(string assetName, float progress, object userData)
    {
        Debug.Log($"Loading {assetName}: {progress:P0}");
    }
}
```

## 加载并实例化 GameObject

`LoadGameObject` 和 `LoadGameObjectAsync` 会加载 Prefab 并实例化到场景中。实例销毁时会通过资源引用组件自动卸载实例引用，通常不需要手动 `UnloadAsset` 这个实例对象。

```csharp
using AlicizaX;
using AlicizaX.Resource.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class LoadPrefabExample : MonoBehaviour
{
    [SerializeField] private Transform root;

    private GameObject _window;

    private async UniTaskVoid Start()
    {
        IResourceService resources = AppServices.Require<IResourceService>();
        _window = await resources.LoadGameObjectAsync("Assets/Bundles/UI/Login.prefab", root);
    }

    private void OnDestroy()
    {
        if (_window != null)
        {
            Destroy(_window);
            _window = null;
        }
    }
}
```

同步版本：

```csharp
GameObject window = resources.LoadGameObject("Assets/Bundles/UI/Login.prefab", root);
```

## 使用 AssetHandle

如果需要直接控制 YooAsset 句柄生命周期，可以使用 Handle API。

```csharp
using AlicizaX;
using AlicizaX.Resource.Runtime;
using UnityEngine;
using YooAsset;

public sealed class AssetHandleExample : MonoBehaviour
{
    private AssetHandle _handle;

    private void Start()
    {
        IResourceService resources = AppServices.Require<IResourceService>();
        _handle = resources.LoadAssetAsyncHandle<AudioClip>("Assets/Bundles/Audios/click.wav");
        _handle.Completed += OnCompleted;
    }

    private void OnCompleted(AssetHandle handle)
    {
        AudioClip clip = handle.AssetObject as AudioClip;
        Debug.Log(clip != null ? clip.name : "Load failed.");
    }

    private void OnDestroy()
    {
        if (_handle != null && _handle.IsValid)
        {
            _handle.Dispose();
        }
    }
}
```

使用 Handle API 时，句柄的释放由调用方负责。

## 多资源包加载

多数 API 的最后一个参数都是 `packageName`。不传时使用默认包。

```csharp
IResourceService resources = AppServices.Require<IResourceService>();

await resources.InitPackageAsync(
    packageName: "DlcPackage",
    hostServerURL: "https://cdn.example.com/DLC",
    fallbackHostServerURL: "https://fallback-cdn.example.com/DLC");

Texture2D dlcIcon = await resources.LoadAssetAsync<Texture2D>(
    "Assets/DLC/Icons/icon_dlc.png",
    packageName: "DlcPackage");
```

## 下载资源

资源更新流程可以使用 `CreateResourceDownloader()` 创建 YooAsset 下载器。

```csharp
using AlicizaX;
using AlicizaX.Resource.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

public sealed class ResourceDownloadExample : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        IResourceService resources = AppServices.Require<IResourceService>();
        ResourceDownloaderOperation downloader = resources.CreateResourceDownloader();

        if (downloader.TotalDownloadCount == 0)
        {
            Debug.Log("No files need download.");
            return;
        }

        downloader.DownloadErrorCallback = data =>
        {
            Debug.LogError($"Download error: {data.PackageName}/{data.FileName}, {data.ErrorInfo}");
        };

        downloader.DownloadUpdateCallback = data =>
        {
            Debug.Log($"Download: {data.CurrentDownloadCount}/{data.TotalDownloadCount}");
        };

        downloader.BeginDownload();
        await downloader;

        if (downloader.Status != EOperationStatus.Succeed)
        {
            Debug.LogError("Download failed.");
            return;
        }

        Debug.Log("Download succeed.");
    }
}
```

项目里 `Assets/Scripts/Startup/Procedure/ProcedureDownloadBundleState.cs` 已经有一份实际下载流程示例。

## 更新包版本和清单

```csharp
using AlicizaX;
using AlicizaX.Resource.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

public sealed class ResourceManifestExample : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        IResourceService resources = AppServices.Require<IResourceService>();

        RequestPackageVersionOperation versionOperation = resources.RequestPackageVersionAsync();
        await versionOperation;

        if (versionOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogError(versionOperation.Error);
            return;
        }

        string packageVersion = versionOperation.PackageVersion;

        UpdatePackageManifestOperation manifestOperation = resources.UpdatePackageManifestAsync(packageVersion);
        await manifestOperation;

        if (manifestOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogError(manifestOperation.Error);
        }
    }
}
```

## UI 图片扩展

如果场景中挂载了 `ResourceExtComponent`，可以使用 `SetSprite` 扩展方法给 `Image` 或 `SpriteRenderer` 设置资源图片。

```csharp
using UnityEngine;
using UnityEngine.UI;

public sealed class SetSpriteExample : MonoBehaviour
{
    [SerializeField] private Image icon;

    private void Start()
    {
        icon.SetSprite("Assets/Bundles/UI/icon_start.png", setNativeSize: true);
    }
}
```

图集子图：

```csharp
icon.SetSubSprite(
    location: "Assets/Bundles/UI/CommonAtlas.spriteatlas",
    spriteName: "btn_start",
    setNativeSize: false);
```

`ResourceExtComponent` 会跟踪目标对象生命周期，在目标销毁或替换资源时释放旧引用。

## 资源回收

```csharp
IResourceService resources = AppServices.Require<IResourceService>();

// 卸载单个通过 LoadAsset 加载出来的资源。
resources.UnloadAsset(sprite);

// 卸载引用计数为 0 的资源。
resources.UnloadUnusedAssets();

// 触发 ResourceComponent 的强制卸载流程，可选择是否 GC。
resources.ForceUnloadUnusedAssets(performGCCollect: true);

// 强制卸载所有资源包资源，WebGL 不支持。
resources.ForceUnloadAllAssets();
```

`ResourceComponent` 也会按 Inspector 中的间隔自动触发资源回收，并在低内存回调中强制回收未使用资源。

## API 速查

```csharp
UniTask<bool> InitPackageAsync(string packageName = "", string hostServerURL = "", string fallbackHostServerURL = "");

bool CheckLocationValid(string location, string packageName = "");
HasAssetResult HasAsset(string location, string packageName = "");
AssetInfo GetAssetInfo(string location, string packageName = "");
AssetInfo[] GetAssetInfos(string tag, string packageName = "");

T LoadAsset<T>(string location, string packageName = "") where T : UnityEngine.Object;
UniTask<T> LoadAssetAsync<T>(string location, CancellationToken cancellationToken = default, string packageName = "") where T : UnityEngine.Object;
UniTask LoadAsset<T>(string location, Action<T> callback, string packageName = "") where T : UnityEngine.Object;

GameObject LoadGameObject(string location, Transform parent = null, string packageName = "");
UniTask<GameObject> LoadGameObjectAsync(string location, Transform parent = null, CancellationToken cancellationToken = default, string packageName = "");

AssetHandle LoadAssetSyncHandle<T>(string location, string packageName = "") where T : UnityEngine.Object;
AssetHandle LoadAssetAsyncHandle<T>(string location, string packageName = "") where T : UnityEngine.Object;

ResourceDownloaderOperation CreateResourceDownloader(string customPackageName = "");
RequestPackageVersionOperation RequestPackageVersionAsync(bool appendTimeTicks = false, int timeout = 60, string customPackageName = "");
UpdatePackageManifestOperation UpdatePackageManifestAsync(string packageVersion, int timeout = 60, string customPackageName = "");
```

## 注意事项

- 资源服务依赖 ObjectPool，组件初始化顺序要保证 ObjectPool 先于 Resource。
- `LoadAsset<T>` 返回的资源使用完后需要 `UnloadAsset(asset)`。
- `LoadGameObject` 实例化出来的对象通常通过 `Destroy(instance)` 释放。
- 使用 `LoadAssetSyncHandle` 或 `LoadAssetAsyncHandle` 时，调用方负责 `Dispose()` 句柄。
- 异步加载建议传入 `CancellationToken`，对象销毁时取消加载。
- 多包资源要显式传入 `packageName`，否则默认使用 `DefaultPackageName`。

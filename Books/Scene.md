# Scene 模块

Scene 模块是对 YooAsset 场景加载的服务封装，支持主场景加载、附加场景加载、挂起加载、激活场景、卸载附加场景、进度回调以及主场景状态查询。

源码位置：

- `Client/Packages/com.alicizax.unity.framework/Runtime/Scene`

## 使用前提

场景中的框架根节点需要挂载：

- `ResourceComponent`
- `SceneComponent`

`SceneComponent` 会注册 `ISceneService`，并确保 Scene 作用域存在。Scene 模块加载资源依赖 YooAsset 和 `IResourceService`。

```csharp
using AlicizaX;
using AlicizaX.Scene.Runtime;

ISceneService scenes = AppServices.Require<ISceneService>();
```

也可以使用框架快捷入口：

```csharp
ISceneService scenes = GameApp.Scene;
```

## 异步加载主场景

主场景使用 `LoadSceneMode.Single`。加载主场景时，框架会重置 Scene 作用域，并在完成后调用资源服务释放未使用资源。

```csharp
using AlicizaX;
using AlicizaX.Scene.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EnterGameSceneExample : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        ISceneService scenes = AppServices.Require<ISceneService>();

        var scene = await scenes.LoadSceneAsync(
            "Assets/Bundles/Scenes/Game.unity",
            LoadSceneMode.Single,
            progressCallBack: progress =>
            {
                Debug.Log($"Load scene progress: {progress:P0}");
            });

        Debug.Log($"Loaded scene: {scene.name}");
    }
}
```

`gcCollect` 默认为 `true`，会在主场景加载完成后触发资源服务清理未使用资源。

## 回调式加载

```csharp
using AlicizaX;
using AlicizaX.Scene.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneCallbackExample : MonoBehaviour
{
    public void LoadBattleScene()
    {
        ISceneService scenes = GameApp.Scene;

        scenes.LoadScene(
            "Assets/Bundles/Scenes/Battle.unity",
            LoadSceneMode.Single,
            callBack: scene => Debug.Log($"Loaded: {scene.name}"),
            progressCallBack: progress => Debug.Log(progress));
    }
}
```

## 加载附加场景

附加场景使用 `LoadSceneMode.Additive`。适合把光照、玩法区域、临时副本等拆成子场景。

```csharp
using AlicizaX;
using AlicizaX.Scene.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

public sealed class AdditiveSceneExample
{
    public async UniTask LoadWeatherScene()
    {
        ISceneService scenes = GameApp.Scene;

        await scenes.LoadSceneAsync(
            "Assets/Bundles/Scenes/WeatherRain.unity",
            LoadSceneMode.Additive);
    }

    public async UniTask UnloadWeatherScene()
    {
        await GameApp.Scene.UnloadAsync("Assets/Bundles/Scenes/WeatherRain.unity");
    }
}
```

重复加载同一个附加场景会被拦截。异步接口中重复加载会抛出异常，回调式接口会打印警告并返回。

## 挂起加载和激活

`suspendLoad` 为 `true` 时，YooAsset 会挂起场景激活。加载完成后可手动激活或解除挂起。

```csharp
using AlicizaX;
using AlicizaX.Scene.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

public sealed class SuspendedSceneExample
{
    public async UniTask LoadAndActivateLater()
    {
        string location = "Assets/Bundles/Scenes/BossRoom.unity";

        await GameApp.Scene.LoadSceneAsync(
            location,
            LoadSceneMode.Additive,
            suspendLoad: true);

        GameApp.Scene.UnSuspend(location);
        GameApp.Scene.ActivateScene(location);
    }
}
```

注意 API 名称是 `UnSuspend`。

## 查询场景状态

```csharp
using AlicizaX;

string mainScene = GameApp.Scene.CurrentMainSceneName;
bool isMain = GameApp.Scene.IsMainScene(mainScene);
bool contains = GameApp.Scene.IsContainScene("Assets/Bundles/Scenes/WeatherRain.unity");
```

`CurrentMainSceneName` 保存当前主场景地址或启动场景名。启动场景没有 YooAsset 句柄时，框架会用 `SceneManager.GetActiveScene()` 做兜底判断。

## API 速查

| API | 说明 |
| --- | --- |
| `LoadSceneAsync(location, mode, ...)` | 异步加载场景 |
| `LoadScene(location, mode, ...)` | 回调式加载场景 |
| `UnloadAsync(location, progress)` | 异步卸载附加场景 |
| `Unload(location, callback, progress)` | 回调式卸载附加场景 |
| `ActivateScene(location)` | 激活已加载场景 |
| `UnSuspend(location)` | 解除挂起加载 |
| `IsContainScene(location)` | 判断主场景或附加场景是否存在 |
| `IsMainScene(location)` | 判断是否当前主场景 |
| `CurrentMainSceneName` | 当前主场景名或地址 |

## 注意事项

1. 主场景加载会重置 Scene 作用域，场景级服务会被销毁并重新创建。
2. `Unload` 和 `UnloadAsync` 只处理附加场景，主场景应通过加载新的 Single 场景替换。
3. 不要在同一个场景地址仍在加载或卸载时重复操作，框架会拒绝并输出日志。
4. 场景地址需要与 YooAsset 可识别的 location 一致。
5. `progressCallBack` 存在时，框架会每帧回调进度直到操作完成。

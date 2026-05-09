# Service 基础服务

Service 是框架的基础服务容器，负责创建全局服务世界、注册模块服务、按作用域查找服务，并把 `Tick/LateTick/FixedTick/DrawGizmos` 分发给实现了对应接口的服务。大多数模块都通过 `AppServices` 注册和获取。

源码位置：

- `Client/Packages/com.alicizax.unity.framework/Runtime/ABase/Service`
- `Client/Packages/com.alicizax.unity.framework/Runtime/ABase/RootModule.cs`
- `Client/Packages/com.alicizax.unity.framework/Runtime/GameApp.cs`

## RootModule

启动场景需要一个框架根节点挂载 `RootModule`。`RootModule` 继承 `AppServiceRoot`，会在 `Awake()` 中创建 `ServiceWorld`，初始化日志、帧率、游戏速度、后台运行和屏幕休眠设置，并在应用退出时清理框架资源。

常用属性：

```csharp
RootModule root = GameApp.Base;

root.FrameRate = 60;
root.GameSpeed = 1f;
root.RunInBackground = true;
root.NeverSleep = true;
```

暂停和恢复游戏：

```csharp
GameApp.Base.PauseGame();
GameApp.Base.ResumeGame();
GameApp.Base.ResetNormalGameSpeed();
```

## AppServices 获取服务

大多数业务代码使用 `AppServices.Require<T>()` 或 `GameApp` 快捷入口。

```csharp
using AlicizaX;
using AlicizaX.Resource.Runtime;
using AlicizaX.UI.Runtime;

IResourceService resources = AppServices.Require<IResourceService>();
IUIService ui = AppServices.Require<IUIService>();
```

如果服务可能还未注册，使用 `TryGet`：

```csharp
if (!AppServices.TryGet<IUIService>(out var ui))
{
    return;
}

ui.ShowUISync<LoginWindow>();
```

`Require<T>()` 找不到服务会抛异常，适合“服务必须存在”的启动流程；`TryGet<T>()` 适合可选模块。

## GameApp 快捷入口

`GameApp` 缓存了常用模块服务：

```csharp
GameApp.Audio
GameApp.Localization
GameApp.ObjectPool
GameApp.Resource
GameApp.Scene
GameApp.Timer
GameApp.UI
GameApp.GameObjectPool
GameApp.Base
```

示例：

```csharp
GameApp.Timer.AddTimer(() =>
{
    GameApp.UI.ShowUISync<LoginWindow>();
}, 1f);
```

`GameApp` 内部使用 `AppServices.RequireApp<T>()`，因此调用前要保证对应组件已经完成注册。

## 服务作用域

框架内置三种作用域：

- `AppScope`：应用级，生命周期最长，适合资源、音频、UI、计时器等全局服务。
- `SceneScope`：场景级，主场景切换时会重置，适合当前场景状态。
- `GameplayScope`：玩法级，适合一局战斗或一个玩法实例的服务。

全局查询优先级为：

```text
Gameplay > Scene > App
```

也就是说，如果同一个服务契约在 Gameplay 和 App 都注册过，`AppServices.Require<T>()` 会优先返回 Gameplay 中的服务。

Scene 模块加载主场景时会调用 `Context.ResetScene()`，因此 Scene 作用域服务会随主场景切换销毁。

## 自定义普通服务

自定义服务实现 `IService`，推荐继承 `ServiceBase`。

```csharp
using AlicizaX;

public interface IPlayerDataService : IService
{
    int Level { get; }
    void SetLevel(int level);
}

public sealed class PlayerDataService : ServiceBase, IPlayerDataService
{
    public int Level { get; private set; }

    protected override void OnInitialize()
    {
        Level = 1;
    }

    protected override void OnDestroyService()
    {
    }

    public void SetLevel(int level)
    {
        Level = level;
    }
}
```

注册到 App 作用域：

```csharp
AppServices.RegisterApp<IPlayerDataService>(new PlayerDataService());

IPlayerDataService playerData = AppServices.Require<IPlayerDataService>();
```

## 可 Tick 服务

实现 `IServiceTickable` 后，`AppServiceRoot.Update()` 会每帧驱动。

```csharp
using AlicizaX;
using UnityEngine;

public sealed class PlayerDataService :
    ServiceBase,
    IPlayerDataService,
    IServiceTickable,
    IServiceOrder
{
    public int Order => 100;
    public int Level { get; private set; }

    protected override void OnInitialize()
    {
    }

    protected override void OnDestroyService()
    {
    }

    public void Tick(float deltaTime)
    {
        Debug.Log($"Player data tick: {deltaTime}");
    }

    public void SetLevel(int level)
    {
        Level = level;
    }
}
```

相关接口：

- `IServiceTickable`：`Update` 阶段调用。
- `IServiceLateTickable`：`LateUpdate` 阶段调用。
- `IServiceFixedTickable`：`FixedUpdate` 阶段调用。
- `IServiceGizmoDrawable`：`OnDrawGizmos` 阶段调用。
- `IServiceOrder`：控制同一生命周期列表中的执行顺序，数值越小越早执行。

## 自定义 Mono 服务

如果服务本身是 `MonoBehaviour`，可以继承 `MonoServiceBehaviour<TScope>`。

```csharp
using AlicizaX;
using UnityEngine;

public sealed class CameraService : MonoServiceBehaviour<AppScope>, IService
{
    public Camera MainCamera { get; private set; }

    protected override void OnAwake()
    {
        MainCamera = Camera.main;
    }

    protected override void OnInitialize()
    {
        Log.Info("Camera service initialized.");
    }

    protected override void OnDestroyService()
    {
        MainCamera = null;
    }
}
```

`MonoServiceBehaviour<TScope>` 会在 `Start()` 中注册到指定作用域，销毁时自动反注册。

注意：Mono 服务不能实现 `IServiceTickable`、`IServiceLateTickable`、`IServiceFixedTickable` 或 `IServiceGizmoDrawable`。这些生命周期接口只允许普通服务使用。

## 服务内部查找依赖

`ServiceBase` 和 `MonoServiceBehaviour` 内部可以通过 `Context` 查找同作用域或全局服务。

```csharp
public sealed class BattleService : ServiceBase
{
    private IResourceService _resources;

    protected override void OnInitialize()
    {
        _resources = Context.Require<IResourceService>();
    }

    protected override void OnDestroyService()
    {
        _resources = null;
    }
}
```

`Context.Require<T>()` 会优先查当前服务所在作用域，再按全局优先级查找。

## 关闭服务世界

```csharp
AppServices.Shutdown();
```

`Shutdown` 会按反向注册顺序销毁所有作用域和服务。通常由 `RootModule` 在应用退出时处理，业务不要随意调用。

## API 速查

| API | 说明 |
| --- | --- |
| `AppServices.HasWorld` | 服务世界是否已创建 |
| `AppServices.RegisterApp<TContract>(service)` | 注册 App 作用域服务 |
| `AppServices.RegisterAppSelf(service)` | 用服务自身类型注册 |
| `AppServices.TryGet<T>(out service)` | 从所有活动作用域查找服务 |
| `AppServices.Require<T>()` | 查找服务，找不到抛异常 |
| `AppServices.TryGetApp<T>(out service)` | 只从 App 作用域查找 |
| `AppServices.RequireApp<T>()` | 只从 App 作用域获取 |
| `AppServices.Shutdown()` | 销毁整个服务世界 |
| `ServiceBase` | 普通服务基类 |
| `MonoServiceBehaviour<TScope>` | Mono 服务基类 |
| `GameApp.*` | 常用模块快捷入口 |

## 注意事项

1. `RootModule` 或其它 `AppServiceRoot` 必须先创建 `ServiceWorld`，否则注册和获取服务会失败。
2. `RegisterApp<TContract>` 的服务对象必须实现服务生命周期接口，继承 `ServiceBase` 最省心。
3. 同一作用域内不能重复注册同一个契约类型。
4. Scene 作用域会随主场景切换重置，不要把跨场景状态注册到 Scene 作用域。
5. `GameApp` 会缓存服务引用，如果你做服务热替换，需要同步考虑缓存失效问题。

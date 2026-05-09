# Timer 模块

Timer 模块提供全局计时器服务，用于延迟执行、循环执行、暂停恢复计时器，以及查询剩余时间。业务代码通过 `AppServices.Require<ITimerService>()` 获取服务。

源码位置：

- `Client/Packages/com.alicizax.unity.framework/Runtime/Timer`

## 使用前提

场景中的框架根节点需要挂载 `TimerComponent`。`TimerComponent` 会在 `Awake()` 中注册 `ITimerService`：

```csharp
AppServices.RegisterApp<ITimerService>(new TimerService(_initialCapacity));
```

`RootModule` 会驱动 `ServiceWorld.Tick`，Timer 服务在 Tick 中推进计时器。

## 获取服务

```csharp
using AlicizaX;
using AlicizaX.Timer.Runtime;

ITimerService timer = AppServices.Require<ITimerService>();
```

如果代码可能早于框架初始化执行，可以使用 `TryGet`：

```csharp
if (!AppServices.TryGet<ITimerService>(out var timer))
{
    return;
}
```

## 延迟执行一次

```csharp
using AlicizaX;
using AlicizaX.Timer.Runtime;
using UnityEngine;

public sealed class TimerOnceExample : MonoBehaviour
{
    private ulong _timerHandle;

    private void Start()
    {
        ITimerService timer = AppServices.Require<ITimerService>();
        _timerHandle = timer.AddTimer(OnTimer, 2f);
    }

    private void OnDestroy()
    {
        if (AppServices.TryGet<ITimerService>(out var timer))
        {
            timer.RemoveTimer(_timerHandle);
        }
    }

    private void OnTimer()
    {
        Debug.Log("2 seconds later.");
        _timerHandle = 0UL;
    }
}
```

`AddTimer` 返回 `ulong` 类型句柄，后续停止、恢复、移除都通过这个句柄操作。

## 循环计时器

```csharp
using AlicizaX;
using AlicizaX.Timer.Runtime;
using UnityEngine;

public sealed class TimerLoopExample : MonoBehaviour
{
    private ITimerService _timer;
    private ulong _heartbeatTimer;

    private void OnEnable()
    {
        _timer = AppServices.Require<ITimerService>();
        _heartbeatTimer = _timer.AddTimer(OnHeartbeat, 1f, isLoop: true);
    }

    private void OnDisable()
    {
        _timer?.RemoveTimer(_heartbeatTimer);
        _heartbeatTimer = 0UL;
    }

    private void OnHeartbeat()
    {
        Debug.Log("Heartbeat.");
    }
}
```

循环计时器会按创建时的间隔重复触发。对象销毁或逻辑结束时应调用 `RemoveTimer`。

## 传递引用参数

Timer 提供泛型重载，可以给回调传入一个引用类型参数。

```csharp
using System;
using AlicizaX;
using AlicizaX.Timer.Runtime;
using UnityEngine;

public sealed class TimerArgExample : MonoBehaviour
{
    private sealed class RewardContext
    {
        public int PlayerId;
        public int Gold;
    }

    private ulong _timerHandle;

    private void Start()
    {
        var context = new RewardContext
        {
            PlayerId = 1001,
            Gold = 500
        };

        ITimerService timer = AppServices.Require<ITimerService>();
        _timerHandle = timer.AddTimer<RewardContext>(GiveReward, context, 3f);
    }

    private static void GiveReward(RewardContext context)
    {
        Debug.Log($"Give {context.Gold} gold to player {context.PlayerId}.");
    }
}
```

泛型参数约束为 `where T : class`，不能直接传 `int`、`float` 等值类型。如果需要传多个值，建议定义上下文类。

## 暂停、恢复、重启

```csharp
ITimerService timer = AppServices.Require<ITimerService>();

ulong handle = timer.AddTimer(OnTimeout, 10f);

// 暂停，剩余时间会被记录。
timer.Stop(handle);

float leftTime = timer.GetLeftTime(handle);

// 从剩余时间继续。
timer.Resume(handle);

// 从原始 duration 重新开始。
timer.Restart(handle);

bool running = timer.IsRunning(handle);
```

`Stop` 只是暂停计时器，不会移除计时器。彻底移除请调用 `RemoveTimer`。

## 使用不受 Time.timeScale 影响的计时器

默认计时器使用 Unity 缩放时间。暂停游戏或修改 `Time.timeScale` 时，如果仍希望计时器继续推进，可以设置 `isUnscaled: true`。

```csharp
using AlicizaX;
using AlicizaX.Timer.Runtime;

ITimerService timer = AppServices.Require<ITimerService>();

ulong handle = timer.AddTimer(
    callback: OnRealTimeTimeout,
    time: 5f,
    isLoop: false,
    isUnscaled: true);
```

## 预热容量

`TimerComponent` Inspector 中可以设置 `Initial Capacity`。运行时代码也可以通过 `ITimerCapacityService` 扩容。

```csharp
using AlicizaX;
using AlicizaX.Timer.Runtime;

if (AppServices.Require<ITimerService>() is ITimerCapacityService capacityService)
{
    capacityService.Prewarm(2048);
}
```

容量会按内部页大小对齐。大量创建计时器前预热，可以减少运行时扩容。

## 调试信息

Timer 服务实现了 `ITimerDebugService`，可在运行时读取活跃计时器和统计信息。

```csharp
using AlicizaX;
using AlicizaX.Timer.Runtime;
using UnityEngine;

public sealed class TimerDebugExample : MonoBehaviour
{
    private readonly TimerDebugInfo[] _buffer = new TimerDebugInfo[32];

    private void Update()
    {
        ITimerService timer = AppServices.Require<ITimerService>();
        if (timer is not ITimerDebugService debugService)
        {
            return;
        }

        debugService.GetStatistics(out int activeCount, out int poolCapacity, out int peakActiveCount, out int freeCount);
        Debug.Log($"Timer Active={activeCount}, Capacity={poolCapacity}, Peak={peakActiveCount}, Free={freeCount}");

        int count = debugService.GetAllTimers(_buffer);
        for (int i = 0; i < count; i++)
        {
            TimerDebugInfo info = _buffer[i];
            Debug.Log($"Timer={info.TimerHandle}, Left={info.LeftTime:F2}, Duration={info.Duration:F2}");
        }
    }
}
```

## API 速查

```csharp
ulong AddTimer(TimerHandlerNoArgs callback, float time, bool isLoop = false, bool isUnscaled = false);
ulong AddTimer<T>(Action<T> callback, T arg, float time, bool isLoop = false, bool isUnscaled = false) where T : class;

void Stop(ulong timerHandle);
void Resume(ulong timerHandle);
bool IsRunning(ulong timerHandle);
float GetLeftTime(ulong timerHandle);
void Restart(ulong timerHandle);
void RemoveTimer(ulong timerHandle);
```

## 注意事项

- `AddTimer` 返回 `0UL` 表示创建失败，例如回调为空或内部槽位不可用。
- `Stop` 是暂停，不是销毁。对象生命周期结束时优先调用 `RemoveTimer`。
- 不要把场景对象长期捕获在全局 Timer 回调里，场景切换或对象销毁时要移除计时器。
- 需要忽略游戏暂停时使用 `isUnscaled: true`。
- 回调内部可以移除当前计时器，Timer 服务会处理执行中释放的情况。

# Event 模块

Event 模块提供轻量级事件总线，适合模块之间做低耦合通知，例如资源下载进度、语言切换、UI 刷新、战斗结算通知等。事件参数必须是 `struct`，并实现 `IEventArgs`。

源码位置：

- `Client/Packages/com.alicizax.unity.framework/Runtime/Event`

## 使用前提

事件总线是静态 API，不需要在场景中挂组件。业务只需要定义事件结构体，然后通过 `EventBus.Subscribe` 订阅，通过 `EventBus.Publish` 发布。

推荐优先使用 `in` 参数订阅方式，避免较大的结构体事件参数被复制。

## 定义事件

```csharp
using AlicizaX;

[Prewarm(8)]
public readonly struct PlayerLevelUpEvent : IEventArgs
{
    public readonly int PlayerId;
    public readonly int OldLevel;
    public readonly int NewLevel;

    public PlayerLevelUpEvent(int playerId, int oldLevel, int newLevel)
    {
        PlayerId = playerId;
        OldLevel = oldLevel;
        NewLevel = newLevel;
    }
}
```

`PrewarmAttribute` 可以作为事件容量的说明标记。SourceGenerator会自动生产实际预分配调用 


## 订阅和取消订阅

`Subscribe` 会返回 `EventRuntimeHandle`。对象销毁或逻辑结束时必须调用 `Dispose()` 取消订阅。

```csharp
using AlicizaX;
using UnityEngine;

public sealed class PlayerLevelView : MonoBehaviour
{
    private EventRuntimeHandle _levelUpHandle;

    private void OnEnable()
    {
        _levelUpHandle = EventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
    }

    private void OnDisable()
    {
        _levelUpHandle.Dispose();
    }

    private void OnPlayerLevelUp(in PlayerLevelUpEvent evt)
    {
        Debug.Log($"Player {evt.PlayerId}: {evt.OldLevel} -> {evt.NewLevel}");
    }
}
```

如果事件结构体很小，也可以使用普通值传递：

```csharp
private EventRuntimeHandle _handle;

private void Start()
{
    _handle = EventBus.Subscribe<PlayerLevelUpEvent>(OnEvent);
}

private void OnEvent(PlayerLevelUpEvent evt)
{
    // 小结构体可以直接值传递。
}
```

## 发布事件

```csharp
using AlicizaX;
using UnityEngine;

public sealed class PlayerLevelSystem : MonoBehaviour
{
    public void AddLevel(int playerId, int oldLevel, int newLevel)
    {
        var evt = new PlayerLevelUpEvent(playerId, oldLevel, newLevel);
        EventBus.Publish(in evt);
    }
}
```

也可以直接传临时值：

```csharp
EventBus.Publish(new PlayerLevelUpEvent(1001, 9, 10));
```

## UI 内自动管理事件

UI 模块提供 `EventListenerProxy`，在 `UIBase.OnRegisterEvent` 中注册的事件会在窗口销毁时自动移除。

```csharp
using AlicizaX;
using AlicizaX.UI.Runtime;
using Game.UI;

public sealed class PlayerInfoWindow : UIWindow<ui_PlayerInfoWindow>
{
    protected override void OnRegisterEvent(EventListenerProxy proxy)
    {
        proxy.AddUIEvent<PlayerLevelUpEvent>(OnPlayerLevelUp);
    }

    private void OnPlayerLevelUp(in PlayerLevelUpEvent evt)
    {
        baseui.TxtLevel.text = evt.NewLevel.ToString();
    }
}
```

## 查询和清理

```csharp
int count = EventBus.GetSubscriberCount<PlayerLevelUpEvent>();

EventBus.EnsureCapacity<PlayerLevelUpEvent>(16);

EventBus.Clear<PlayerLevelUpEvent>();
```

`Clear<T>()` 会移除某一种事件的所有订阅者，一般只在模块卸载、测试或热重载流程中使用。

## API 速查

| API | 说明 |
| --- | --- |
| `EventBus.Subscribe<T>(Action<T>)` | 订阅值传递事件 |
| `EventBus.Subscribe<T>(InEventHandler<T>)` | 订阅 `in` 参数事件 |
| `EventBus.Publish<T>(in T evt)` | 发布事件 |
| `EventBus.GetSubscriberCount<T>()` | 获取订阅者数量 |
| `EventBus.EnsureCapacity<T[UI.md](UI.md)>(int capacity)` | 预分配事件订阅容量 |
| `EventBus.Clear<T>()` | 清理指定事件所有订阅 |
| `EventRuntimeHandle.Dispose()` | 取消单个订阅 |

## 注意事项

1. 事件参数必须是 `struct`，并实现 `IEventArgs`。
2. 订阅句柄必须保存并在不用时释放，否则会导致对象无法被正常回收。
3. 不要在事件发布回调中订阅、取消订阅、清理或扩容同一个事件类型，框架会拒绝发布中的结构修改。
4. 高频事件建议使用 `in` 参数订阅，并在初始化阶段调用 `EnsureCapacity`。
5. 事件总线只负责通知，不负责状态保存；需要持久状态时应由业务服务或数据模块维护。

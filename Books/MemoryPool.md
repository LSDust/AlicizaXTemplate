# MemoryPool 模块

MemoryPool 是框架提供的轻量级普通 C# 对象缓存池，适合缓存频繁创建、释放的临时数据对象，例如事件参数、加载请求、战斗结算数据、UI 列表项数据等。被池化的对象需要实现 `IMemory`，并在 `Clear()` 中把对象状态恢复到可复用状态。

源码位置：

- `Client/Packages/com.alicizax.unity.framework/Runtime/MemoryPool`

## 使用前提

1. 需要被池化的类型必须是引用类型。
2. 类型必须实现 `IMemory`。
3. 类型必须有无参构造函数。
4. 使用完成后必须调用 `MemoryPool.Release` 归还对象。

```csharp
using AlicizaX;

public sealed class BattleDamageInfo : IMemory
{
    public int AttackerId;
    public int TargetId;
    public int Damage;
    public bool Critical;

    public void Clear()
    {
        AttackerId = 0;
        TargetId = 0;
        Damage = 0;
        Critical = false;
    }
}
```

## 获取和归还对象

最常用的方式是泛型 API。对象从池中取出后可能是复用对象，所以业务字段必须重新赋值。

```csharp
using AlicizaX;
using UnityEngine;

public sealed class MemoryPoolExample : MonoBehaviour
{
    private void Start()
    {
        BattleDamageInfo info = MemoryPool.Acquire<BattleDamageInfo>();
        info.AttackerId = 1001;
        info.TargetId = 2001;
        info.Damage = 350;
        info.Critical = true;

        Debug.Log($"Damage: {info.Damage}");

        MemoryPool.Release(info);
    }
}
```

也可以直接使用类型专属池：

```csharp
BattleDamageInfo info = MemoryPool<BattleDamageInfo>.Acquire();

// 使用对象...

MemoryPool<BattleDamageInfo>.Release(info);
```

## 预热对象

如果某类对象会在短时间内大量使用，可以在加载阶段预热，减少运行时临时分配。

```csharp
using AlicizaX;
using UnityEngine;

public sealed class BattlePrewarm : MonoBehaviour
{
    private void Awake()
    {
        MemoryPool.Add<BattleDamageInfo>(128);
        MemoryPool<BattleDamageInfo>.SetCapacity(256, 1024);
    }
}
```

`SetCapacity(softCapacity, hardCapacity)` 中：

- `softCapacity`：自适应保留对象数量的上限。
- `hardCapacity`：池内最多保留的未使用对象数量。

释放对象时，如果未使用对象数量已经达到硬上限，新归还的对象会在执行 `Clear()` 后被丢弃，不再进入池。

## 动态类型句柄

如果类型在运行时才确定，优先缓存 `MemoryPoolHandle`，不要在热路径里反复调用 `Acquire(Type)`。

```csharp
using System;
using AlicizaX;

public sealed class RuntimeMemoryFactory
{
    private readonly MemoryPoolHandle _handle;

    public RuntimeMemoryFactory(Type memoryType)
    {
        _handle = MemoryPool.GetHandle(memoryType);
    }

    public IMemory Acquire()
    {
        return _handle.Acquire();
    }

    public void Release(IMemory memory)
    {
        _handle.Release(memory);
    }
}
```

## 严格检查

`MemoryPool.EnableStrictCheck` 可以检查重复归还对象。严格检查会额外维护对象集合，性能开销较大，建议只在编辑器或开发包中开启。

项目中也提供了 `MemoryPoolSetting` 组件，可通过 Inspector 配置严格检查策略：

- `AlwaysEnable`：总是开启。
- `OnlyEnableWhenDevelopment`：仅开发包开启。
- `OnlyEnableInEditor`：仅编辑器开启。
- `AlwaysDisable`：总是关闭。

```csharp
#if UNITY_EDITOR
MemoryPool.EnableStrictCheck = true;
#endif
```

## 查看池信息

可以使用 `GetAllMemoryPoolInfos` 获取所有已注册内存池的快照信息。

```csharp
using AlicizaX;
using UnityEngine;

public sealed class MemoryPoolDebugExample : MonoBehaviour
{
    private readonly MemoryPoolInfo[] _infos = new MemoryPoolInfo[64];

    private void Update()
    {
        int count = MemoryPool.GetAllMemoryPoolInfos(_infos);
        for (int i = 0; i < count; i++)
        {
            MemoryPoolInfo info = _infos[i];
            Debug.Log($"{info.Type.Name} Unused={info.UnusedCount}, Using={info.UsingCount}, Created={info.CreateCount}");
        }
    }
}
```

常用字段：

- `UnusedCount`：池内未使用对象数。
- `UsingCount`：已取出未归还对象数。
- `AcquireCount`：累计获取次数。
- `ReleaseCount`：累计归还次数。
- `CreateCount`：累计创建次数。
- `HighWaterMark`：当前自适应保留目标。
- `MaxCapacity`：软容量。
- `PoolArrayLength`：内部数组长度。

## 手动释放和压缩

```csharp
// 移除 BattleDamageInfo 池中 32 个未使用对象。
MemoryPool.Remove<BattleDamageInfo>(32);

// 清空某一类对象池。
MemoryPool.RemoveAll<BattleDamageInfo>();

// 压缩某一类对象池内部数组。
MemoryPool.Compact<BattleDamageInfo>();

// 清空所有内存池。
MemoryPool.ClearAll();

// 压缩所有内存池。
MemoryPool.CompactAll();
```

`RootModule` 在框架关闭时会调用 `MemoryPool.ClearAll()`，通常业务代码不需要在退出游戏时手动清理全部内存池。

## 完整示例：事件参数复用

```csharp
using AlicizaX;
using UnityEngine;

public sealed class PlayerLevelUpEventArgs : IMemory
{
    public int PlayerId;
    public int OldLevel;
    public int NewLevel;

    public static PlayerLevelUpEventArgs Create(int playerId, int oldLevel, int newLevel)
    {
        PlayerLevelUpEventArgs args = MemoryPool.Acquire<PlayerLevelUpEventArgs>();
        args.PlayerId = playerId;
        args.OldLevel = oldLevel;
        args.NewLevel = newLevel;
        return args;
    }

    public void Clear()
    {
        PlayerId = 0;
        OldLevel = 0;
        NewLevel = 0;
    }
}

public sealed class LevelSystem : MonoBehaviour
{
    public void NotifyLevelUp(int playerId, int oldLevel, int newLevel)
    {
        PlayerLevelUpEventArgs args = PlayerLevelUpEventArgs.Create(playerId, oldLevel, newLevel);

        try
        {
            Debug.Log($"Player {args.PlayerId}: {args.OldLevel} -> {args.NewLevel}");
        }
        finally
        {
            MemoryPool.Release(args);
        }
    }
}
```

## 注意事项

- 不要在归还对象后继续持有和访问该对象。
- `Clear()` 只负责重置对象状态，不要在里面写业务派发逻辑。
- 对象取出后要完整重新赋值，不要依赖上一次使用留下的字段。
- 热路径优先使用 `MemoryPool<T>.Acquire()`、`MemoryPool.Acquire<T>()` 或缓存后的 `MemoryPoolHandle`。
- 需要检测重复归还时开启严格检查，但不要在正式性能敏感环境长期启用。

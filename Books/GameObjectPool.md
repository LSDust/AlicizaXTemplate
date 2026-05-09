# GameObjectPool 模块

GameObjectPool 模块用于池化 Unity `GameObject` 实例，适合特效、怪物、建筑、UI 临时对象等频繁创建和回收的预制体。它按 `PoolConfigScriptableObject` 中的规则建立池，支持 AssetBundle 和 Resources 两种加载器，提供预热、异步获取、同步获取、释放和强制清理。

源码位置：

- `Client/Packages/com.alicizax.unity.framework/Runtime/ABase/GameObjectPool`
- 编辑器辅助：`Client/Packages/com.alicizax.unity.framework/Editor/GameObjectPool`

## 使用前提

场景中的框架根节点需要挂载：

- `ResourceComponent`
- `GameObjectPoolComponent`

`GameObjectPoolComponent` 会注册 `IGameObjectPoolService` 和调试服务。池化实例的资源加载依赖 `IResourceService`。

```csharp
using AlicizaX;

IGameObjectPoolService pool = AppServices.Require<IGameObjectPoolService>();
```

快捷入口：

```csharp
IGameObjectPoolService pool = GameApp.GameObjectPool;
```

## 当前接口限制

当前公开接口 `IGameObjectPoolService` 只暴露：

```csharp
bool TryGetPoolAssetId(string assetPath, out PoolAssetId assetId);
GameObject GetGameObject(PoolAssetId assetId, Transform parent = null);
UniTask<GameObject> GetGameObjectAsync(PoolAssetId assetId, Transform parent = null, CancellationToken cancellationToken = default);
UniTask PreloadAsync(PoolAssetId assetId, int count = 1, CancellationToken cancellationToken = default);
void Release(GameObject gameObject);
void ForceCleanup();
```

源码内部存在 `GameObjectPoolService.LoadCatalog(string poolConfigPath)` 用于加载 `PoolConfigScriptableObject`，但 `GameObjectPoolService` 是 `internal`，且该方法未暴露到 `IGameObjectPoolService`。因此业务代码当前应以“框架启动阶段已加载好 PoolConfig”为前提使用；如果需要业务层主动加载配置，需要后续给接口补充公开方法或增加启动封装。

## 创建 PoolConfig

在 Project 面板创建：

```text
Create > GameplaySystem > PoolConfig
```

每条 `PoolEntry` 代表一组池化规则：

| 字段 | 说明 |
| --- | --- |
| `entryName` | 规则名 |
| `group` | 分组名，运行时会创建对应分组根节点 |
| `assetPath` | 资源路径或路径前缀 |
| `loaderType` | `AssetBundle` 或 `Resources` |
| `category` | 分类，内置 `Default/Effect/Monster/Building/UI/Custom` |
| `softCapacity` | 软容量，用于回收保留策略 |
| `hardCapacity` | 硬容量，池中最多实例数 |
| `priority` | 规则优先级，越大越优先匹配 |

路径会被 Normalize：

- AssetBundle 路径会去掉 `Assets/Bundle/` 或 `Assets/Bundles/` 根路径。
- Resources 路径会去掉 `Assets/Resources/` 或中间的 `/Resources/`。
- 文件扩展名会被去掉。

例如：

```text
Assets/Bundles/Effects/Explosion.prefab -> Effects/Explosion
Assets/Resources/UI/DamageText.prefab -> UI/DamageText
```

## 解析资源 ID

业务使用前先把字符串路径解析为 `PoolAssetId`。解析成功后，后续预热和获取都使用这个 ID。

```csharp
using AlicizaX;
using UnityEngine;

public sealed class EffectPoolExample : MonoBehaviour
{
    private PoolAssetId _explosionId;

    private void Awake()
    {
        if (!GameApp.GameObjectPool.TryGetPoolAssetId("Effects/Explosion", out _explosionId))
        {
            Debug.LogError("Explosion is not configured in PoolConfig.");
        }
    }
}
```

如果同一个逻辑路径可能同时匹配 AssetBundle 和 Resources 规则，可以显式加前缀：

```csharp
GameApp.GameObjectPool.TryGetPoolAssetId("ab:Effects/Explosion", out var abId);
GameApp.GameObjectPool.TryGetPoolAssetId("res:UI/DamageText", out var resId);
```

## 预热对象

```csharp
using AlicizaX;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class PoolPreloadExample : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        if (!GameApp.GameObjectPool.TryGetPoolAssetId("Effects/Explosion", out var assetId))
        {
            return;
        }

        await GameApp.GameObjectPool.PreloadAsync(assetId, 10);
    }
}
```

预热会提前加载预制体并创建一定数量的非激活实例，减少战斗中第一次播放特效时的卡顿。

## 同步获取和释放

```csharp
using AlicizaX;
using UnityEngine;

public sealed class ExplosionSpawner : MonoBehaviour
{
    private PoolAssetId _assetId;

    private void Awake()
    {
        GameApp.GameObjectPool.TryGetPoolAssetId("Effects/Explosion", out _assetId);
    }

    public GameObject Spawn(Vector3 position)
    {
        GameObject instance = GameApp.GameObjectPool.GetGameObject(_assetId, transform);
        if (instance == null)
        {
            return null;
        }

        instance.transform.position = position;
        return instance;
    }

    public void Despawn(GameObject instance)
    {
        GameApp.GameObjectPool.Release(instance);
    }
}
```

`Release` 会检查对象上是否有 `GameObjectPoolHandle`。如果有并且仍属于池，就回收到池；否则会直接销毁这个对象。

## 异步获取

```csharp
using System.Threading;
using AlicizaX;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class AsyncMonsterSpawner : MonoBehaviour
{
    private PoolAssetId _monsterId;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        GameApp.GameObjectPool.TryGetPoolAssetId("Enemies/EnemySoldier", out _monsterId);
        _cts = new CancellationTokenSource();
    }

    public async UniTask<GameObject> SpawnAsync()
    {
        GameObject monster = await GameApp.GameObjectPool.GetGameObjectAsync(
            _monsterId,
            transform,
            _cts.Token);

        return monster;
    }

    private void OnDestroy()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
```

当池已达到 `hardCapacity` 且没有空闲对象时，同步获取会返回 `null`，异步获取可能等待后续释放的对象。

## 池化对象生命周期

预制体上的组件可以实现 `IGameObjectPoolable`，接收池化生命周期回调。

```csharp
using AlicizaX;
using UnityEngine;

public sealed class PooledEffect : MonoBehaviour, IGameObjectPoolable
{
    public void OnPoolGet(in PoolSpawnContext context)
    {
        transform.SetParent(context.Parent, false);
        gameObject.SetActive(true);
    }

    public void OnPoolRelease()
    {
        // 停止粒子、清理临时状态。
    }

    public void OnPoolDestroy()
    {
        // 释放额外资源。
    }
}
```

`PoolSpawnContext` 包含：

- `AssetPath`：逻辑资源路径。
- `Group`：池规则分组。
- `Parent`：本次获取时传入的父节点。
- `SpawnFrame`：获取发生的帧。

## 强制清理

```csharp
GameApp.GameObjectPool.ForceCleanup();
```

`ForceCleanup` 会立即执行维护逻辑，尝试裁剪空闲实例和卸载冷预制体。低内存时服务也会主动执行类似清理。

## 调试查看

运行时选中 `GameObjectPoolComponent`，Inspector 会显示：

- 池数量、已加载预制体数量。
- 总实例数、激活实例数、空闲实例数。
- 每条规则的命中、未命中、扩容、销毁、峰值等计数。
- 每个实例的激活状态、空闲时间、生命周期时间和对象引用。

Debugger 面板中也有对象池相关调试窗口。

## API 速查

| API | 说明 |
| --- | --- |
| `TryGetPoolAssetId(path, out id)` | 根据路径解析池化资源 ID |
| `GetGameObject(id, parent)` | 同步获取池化实例 |
| `GetGameObjectAsync(id, parent, token)` | 异步获取池化实例 |
| `PreloadAsync(id, count, token)` | 预热实例 |
| `Release(gameObject)` | 释放或销毁实例 |
| `ForceCleanup()` | 立即执行池维护 |
| `IGameObjectPoolable.OnPoolGet(...)` | 对象取出回调 |
| `IGameObjectPoolable.OnPoolRelease()` | 对象回收回调 |
| `IGameObjectPoolable.OnPoolDestroy()` | 对象销毁回调 |

## 注意事项

1. 当前业务公共接口无法主动调用 `LoadCatalog`，需要启动流程保证 PoolConfig 已加载，或后续扩展接口。
2. 业务不要手动销毁池化对象，统一调用 `Release`。
3. `PoolAssetId` 带有 Catalog 版本，重新加载 Catalog 后旧 ID 会失效。
4. `hardCapacity` 达到上限后，同步获取可能失败，业务要处理 `null`。
5. 资源路径要按 Normalize 后的路径使用，必要时用 `ab:` 或 `res:` 前缀消除歧义。

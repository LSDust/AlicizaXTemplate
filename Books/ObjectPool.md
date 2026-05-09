# ObjectPool 模块

ObjectPool 模块用于管理带生命周期的对象包装器。它和 `MemoryPool` 的职责不同：

- `MemoryPool` 负责普通 C# 临时对象的复用。
- `ObjectPool` 负责注册、Spawn、Unspawn、释放带目标对象的 `ObjectBase`。

资源模块和音频模块内部都会使用 ObjectPool 管理资源对象、AudioSource 包装对象等。

源码位置：

- `Client/Packages/com.alicizax.unity.framework/Runtime/ObjectPool`

## 使用前提

场景中的框架根节点需要挂载 `ObjectPoolComponent`。组件会在 `Awake()` 中注册 `IObjectPoolService`：

```csharp
AppServices.RegisterApp<IObjectPoolService>(new ObjectPoolService());
```

业务代码通过 `AppServices.Require<IObjectPoolService>()` 获取对象池服务。

```csharp
using AlicizaX;
using AlicizaX.ObjectPool;

IObjectPoolService objectPoolService = AppServices.Require<IObjectPoolService>();
```

## 定义池对象

池内对象需要继承 `ObjectBase` 或 `ObjectBase<TTarget>`，并实现 `Release(bool isShutdown)`。

```csharp
using AlicizaX;
using AlicizaX.ObjectPool;
using UnityEngine;

public sealed class BulletObject : ObjectBase<GameObject>
{
    public static BulletObject Create(string name, GameObject target)
    {
        BulletObject obj = MemoryPool.Acquire<BulletObject>();
        obj.Initialize(name, target);
        return obj;
    }

    protected internal override void OnSpawn()
    {
        Target.SetActive(true);
    }

    protected internal override void OnUnspawn()
    {
        Target.SetActive(false);
    }

    protected internal override void Release(bool isShutdown)
    {
        if (Target != null)
        {
            Object.Destroy(Target);
        }
    }
}
```

`Initialize(name, target)` 的 `name` 用于按名称查找和生成对象，`target` 是真正被管理的业务对象。

## 创建对象池

```csharp
using AlicizaX;
using AlicizaX.ObjectPool;
using UnityEngine;

public sealed class BulletPoolExample : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;

    private IObjectPool<BulletObject> _bulletPool;

    private void Awake()
    {
        IObjectPoolService objectPoolService = AppServices.Require<IObjectPoolService>();

        _bulletPool = objectPoolService.CreatePool<BulletObject>(
            new ObjectPoolCreateOptions(
                name: "Bullet",
                allowMultiSpawn: false,
                autoReleaseInterval: 30f,
                capacity: 128,
                expireTime: 60f,
                priority: 0));
    }
}
```

常用创建选项：

- `name`：同一类型可以用不同名称创建多个池。
- `allowMultiSpawn`：是否允许同一个池对象被多次 Spawn。
- `autoReleaseInterval`：自动释放检查间隔。
- `capacity`：池对象容量。
- `expireTime`：未使用对象过期时间。
- `priority`：全局释放时的排序优先级。

也可以使用快捷构造：

```csharp
IObjectPool<BulletObject> singlePool = objectPoolService.CreatePool<BulletObject>(
    ObjectPoolCreateOptions.Single("Bullet"));

IObjectPool<BulletObject> multiPool = objectPoolService.CreatePool<BulletObject>(
    ObjectPoolCreateOptions.Multi("SharedConfig"));
```

## 注册对象

对象池不会自动实例化目标对象。需要业务代码创建目标对象，然后包装成 `ObjectBase` 注册到池里。

```csharp
private void PreloadBullets(int count)
{
    for (int i = 0; i < count; i++)
    {
        GameObject bullet = Instantiate(bulletPrefab);
        bullet.SetActive(false);

        BulletObject bulletObject = BulletObject.Create("NormalBullet", bullet);
        _bulletPool.Register(bulletObject, spawned: false);
    }
}
```

`spawned` 参数：

- `false`：注册后处于未使用状态，可以被 `Spawn` 取出。
- `true`：注册后立即视为使用中，会触发 `OnSpawn()`。

## 取出和归还

```csharp
public GameObject SpawnBullet(Vector3 position)
{
    BulletObject bulletObject = _bulletPool.Spawn("NormalBullet");
    if (bulletObject == null)
    {
        GameObject bullet = Instantiate(bulletPrefab);
        bulletObject = BulletObject.Create("NormalBullet", bullet);
        _bulletPool.Register(bulletObject, spawned: true);
    }

    bulletObject.Target.transform.position = position;
    return bulletObject.Target;
}

public void DespawnBullet(GameObject bullet)
{
    _bulletPool.Unspawn(bullet);
}
```

`Unspawn` 可以传池对象，也可以传目标对象：

```csharp
_bulletPool.Unspawn(bulletObject);
_bulletPool.Unspawn(bulletGameObject);
```

## 检查是否可 Spawn

```csharp
if (_bulletPool.CanSpawn("NormalBullet"))
{
    BulletObject bullet = _bulletPool.Spawn("NormalBullet");
}
```

不传名称时使用空名称：

```csharp
if (_bulletPool.CanSpawn())
{
    BulletObject obj = _bulletPool.Spawn();
}
```

## 单次 Spawn 和多次 Spawn

默认 `allowMultiSpawn` 为 `false`，同一个池对象同一时间只能被取出一次。归还后才能再次取出。

如果创建池时设置 `allowMultiSpawn: true`，同一个对象可以被多次取出，内部会增加 `SpawnCount`。只有 `Unspawn` 到计数归零后，才会重新进入未使用列表。

这个模式适合共享配置、共享资源引用，不适合普通 GameObject 实例。

## 锁定对象和自定义释放

`ObjectBase.Locked` 为 `true` 时，对象不会被自动释放。

```csharp
BulletObject bulletObject = _bulletPool.Spawn("NormalBullet");
bulletObject.Locked = true;
```

也可以重写 `CustomCanReleaseFlag` 控制是否允许释放：

```csharp
public sealed class ConfigObject : ObjectBase<TextAsset>
{
    public bool IsPinned;

    public override bool CustomCanReleaseFlag => !IsPinned;

    protected internal override void Release(bool isShutdown)
    {
    }
}
```

自动释放或 `ReleaseAllUnused()` 只会释放：

- 未使用对象。
- 未锁定对象。
- `CustomCanReleaseFlag == true` 的对象。

## 手动释放

```csharp
// 按容量和过期策略释放。
_bulletPool.Release();

// 请求释放指定数量的未使用对象，实际释放会受预算和可释放条件影响。
_bulletPool.Release(16);

// 立即释放所有当前未使用且允许释放的对象。
_bulletPool.ReleaseAllUnused();

// 释放所有对象池中的未使用对象。
objectPoolService.ReleaseAllUnused();
```

`Release()` 和 `Release(int)` 会标记待释放数量，ObjectPool 服务在 Tick 中按帧预算逐步释放。`ReleaseAllUnused()` 会立即遍历并释放所有可释放的未使用对象。

## 获取和销毁对象池

```csharp
IObjectPoolService objectPoolService = AppServices.Require<IObjectPoolService>();

bool hasPool = objectPoolService.HasObjectPool<BulletObject>("Bullet");
IObjectPool<BulletObject> pool = objectPoolService.GetObjectPool<BulletObject>("Bullet");

objectPoolService.DestroyObjectPool<BulletObject>("Bullet");
```

销毁对象池时会调用池内对象的 `Release(true)`，并把包装对象归还给 `MemoryPool`。

## 查看对象信息

`IObjectPool<T>` 没有暴露调试枚举接口，但具体对象池基类 `ObjectPoolBase` 提供 `GetAllObjectInfos`。如果需要调试，可以在编辑器或内部工具中从组件获取对象池列表，再读取对象信息。

```csharp
using AlicizaX.ObjectPool;
using UnityEngine;

public static class ObjectPoolDebugUtility
{
    public static void Dump(ObjectPoolBase pool)
    {
        ObjectInfo[] infos = new ObjectInfo[pool.Count];
        int count = pool.GetAllObjectInfos(infos);

        for (int i = 0; i < count && i < infos.Length; i++)
        {
            ObjectInfo info = infos[i];
            Debug.Log($"{info.Name}, InUse={info.IsInUse}, SpawnCount={info.SpawnCount}, Locked={info.Locked}");
        }
    }
}
```

## 完整示例：Prefab 对象池

```csharp
using AlicizaX;
using AlicizaX.ObjectPool;
using UnityEngine;

public sealed class EnemyObject : ObjectBase<GameObject>
{
    public static EnemyObject Create(string name, GameObject target)
    {
        EnemyObject obj = MemoryPool.Acquire<EnemyObject>();
        obj.Initialize(name, target);
        return obj;
    }

    protected internal override void OnSpawn()
    {
        Target.SetActive(true);
    }

    protected internal override void OnUnspawn()
    {
        Target.SetActive(false);
    }

    protected internal override void Release(bool isShutdown)
    {
        if (Target != null)
        {
            Object.Destroy(Target);
        }
    }
}

public sealed class EnemyPool : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;

    private IObjectPool<EnemyObject> _pool;

    private void Awake()
    {
        IObjectPoolService objectPoolService = AppServices.Require<IObjectPoolService>();
        _pool = objectPoolService.CreatePool<EnemyObject>(
            new ObjectPoolCreateOptions("Enemy", false, 20f, 64, 30f, 0));
    }

    public GameObject Spawn(Vector3 position)
    {
        EnemyObject enemy = _pool.Spawn("Default");
        if (enemy == null)
        {
            GameObject instance = Instantiate(enemyPrefab);
            enemy = EnemyObject.Create("Default", instance);
            _pool.Register(enemy, spawned: true);
        }

        enemy.Target.transform.position = position;
        return enemy.Target;
    }

    public void Despawn(GameObject enemy)
    {
        _pool.Unspawn(enemy);
    }
}
```

## 注意事项

- `ObjectBase` 对象本身来自 `MemoryPool`，不要用 `new` 后直接注册。
- 注册对象时 `Target` 不能为空。
- 同一个 `Target` 不能重复注册到同一个对象池。
- GameObject 池通常使用 `allowMultiSpawn: false`。
- `Release(bool isShutdown)` 负责真正销毁或释放目标对象。
- 对象归还后不要继续把它当作使用中对象访问。

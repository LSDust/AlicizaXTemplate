# Procedure 模块

Procedure 模块提供流程状态机，适合启动流程、热更新流程、登录流程、战斗加载流程等串行阶段管理。每个流程继承 `ProcedureBase`，通过 `ProcedureBuilder` 初始化和切换。

源码位置：

- `Client/Packages/com.alicizax.unity.framework/Runtime/Procedure`
- 项目示例：`Client/Assets/Scripts/Startup/Procedure`

## 使用前提

Procedure 不需要额外挂场景组件。调用 `ProcedureBuilder.InitializeProcedure` 后，框架会创建一个 `[ProcedureRunner]` 对象并 `DontDestroyOnLoad`，然后驱动当前流程的 `OnUpdate()`。

## 定义流程

```csharp
using AlicizaX;
using UnityEngine;

public sealed class ProcedureCheckVersion : ProcedureBase
{
    protected internal override void OnInit()
    {
        Debug.Log("CheckVersion init");
    }

    protected internal override void OnEnter()
    {
        Debug.Log("Start check version");
        SwitchProcedure<ProcedureDownload>();
    }

    protected internal override void OnLeave()
    {
        Debug.Log("Leave check version");
    }

    protected internal override void OnDestroy()
    {
        Debug.Log("Destroy check version");
    }
}
```

常用生命周期：

- `OnInit()`：流程被加入状态机时调用一次。
- `OnEnter()`：切换到该流程时调用。
- `OnLeave()`：离开该流程时调用。
- `OnUpdate()`：当前流程每帧调用。
- `OnDestroy()`：流程状态机销毁时调用。

## 初始化流程

```csharp
using System;
using System.Collections.Generic;
using AlicizaX;
using UnityEngine;

public sealed class StartupProcedureEntry : MonoBehaviour
{
    private void Start()
    {
        var procedures = new List<ProcedureBase>
        {
            new ProcedureCheckVersion(),
            new ProcedureDownload(),
            new ProcedureEnterGame(),
        };

        ProcedureBuilder.InitializeProcedure(
            procedures,
            typeof(ProcedureCheckVersion));
    }

    private void OnDestroy()
    {
        ProcedureBuilder.DestroyProcedure();
    }
}
```

项目里已有启动示例：

```csharp
ProcedureBuilder.InitializeProcedure(
    new List<ProcedureBase>
    {
        new ProcedureEntryState(),
        new ProcedureInitPackageState(),
        new ProcedureUpdateVersionState(),
        new ProcedureUpdateManifestState(),
        new ProcedureCreateDownloaderState(),
        new ProcedureDownloadBundleState(),
        new ProcedureDownloadOverState(),
        new ProcedureClearCacheBundleState(),
        new ProcedureUpdateFinishState(),
    },
    typeof(ProcedureEntryState));
```

## 切换流程

在流程内部可直接调用受保护方法：

```csharp
protected internal override void OnEnter()
{
    if (NeedDownload())
    {
        SwitchProcedure<ProcedureDownload>();
        return;
    }

    SwitchProcedure<ProcedureEnterGame>();
}
```

在流程外部可调用静态 API：

```csharp
bool succeed = ProcedureBuilder.SwitchProcedure<ProcedureEnterGame>();
```

如果目标流程没有注册，内部会尝试回退到默认流程。默认流程也不存在时返回 `false`。

## 异步流程写法

`ProcedureBase` 生命周期是同步方法。异步任务可以在 `OnEnter()` 中启动，并在完成后切换流程。

```csharp
using AlicizaX;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class ProcedureDownload : ProcedureBase
{
    private bool _running;

    protected internal override void OnEnter()
    {
        _running = true;
        DownloadAsync().Forget();
    }

    protected internal override void OnLeave()
    {
        _running = false;
    }

    private async UniTaskVoid DownloadAsync()
    {
        await UniTask.Delay(1000);

        if (!_running)
        {
            return;
        }

        SwitchProcedure<ProcedureEnterGame>();
    }
}
```

如果异步流程可能被中途离开，建议使用标记位或 `CancellationTokenSource` 自行取消。

## 查询当前流程

```csharp
if (ProcedureBuilder.IsRunning)
{
    Type current = ProcedureBuilder.CurrentProcedureType;
    Debug.Log(current?.Name);
}

bool hasDownload = ProcedureBuilder.ContainsProcedure<ProcedureDownload>();
```

## 销毁流程

```csharp
ProcedureBuilder.DestroyProcedure();
```

销毁时会先让当前流程执行 `OnLeave()`，再让所有流程执行 `OnDestroy()`，最后销毁 `[ProcedureRunner]`。

## API 速查

| API | 说明 |
| --- | --- |
| `ProcedureBuilder.InitializeProcedure(...)` | 初始化流程状态机 |
| `ProcedureBuilder.SwitchProcedure<T>()` | 切换到指定流程 |
| `ProcedureBuilder.SwitchProcedure(Type)` | 通过类型切换流程 |
| `ProcedureBuilder.ContainsProcedure<T>()` | 判断流程是否已注册 |
| `ProcedureBuilder.CurrentProcedureType` | 当前流程类型 |
| `ProcedureBuilder.IsRunning` | 流程状态机是否运行中 |
| `ProcedureBuilder.DestroyProcedure()` | 销毁流程状态机 |
| `ProcedureBase.SwitchProcedure<T>()` | 流程内部切换流程 |

## 注意事项

1. 初始化会先销毁旧的流程状态机，再创建新的 `[ProcedureRunner]`。
2. 每种流程类型只保留一个实例，重复添加同类型流程会替换旧实例。
3. `OnUpdate()` 只会在当前流程上执行。
4. 异步任务不会被 Procedure 自动取消，流程离开时需要业务自己处理。
5. 流程适合管理阶段切换，不建议把大量业务状态都塞进流程类。

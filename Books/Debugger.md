# Debugger 模块

Debugger 模块提供运行时调试面板，基于 UI Toolkit 显示控制台日志、系统信息、输入信息、场景信息、路径信息、Profiler、运行时内存、对象池、引用池、Audio、Timer 等调试窗口，也支持注册自定义调试窗口。

源码位置：

- `Client/Packages/com.alicizax.unity.framework/Runtime/Debugger`

## 使用前提

场景中的框架根节点需要挂载：

- `DebuggerComponent`

`DebuggerComponent` 会注册 `IDebuggerService`，创建运行时 `UIDocument`、`PanelSettings` 和必要的 `EventSystem`。内置窗口会在 `Start()` 中注册。

```csharp
using AlicizaX;
using AlicizaX.Debugger.Runtime;

IDebuggerService debugger = AppServices.Require<IDebuggerService>();
```

## 激活模式

`DebuggerComponent` Inspector 中的激活模式对应：

```csharp
public enum DebuggerActiveWindowType : byte
{
    AlwaysOpen = 0,
    OnlyOpenWhenDevelopment,
    OnlyOpenInEditor,
    AlwaysClose,
}
```

运行时可通过服务或组件控制：

```csharp
using AlicizaX;
using AlicizaX.Debugger.Runtime;

IDebuggerService debugger = AppServices.Require<IDebuggerService>();
debugger.ActiveWindow = true;

DebuggerComponent.Instance.ShowFullWindow = true;
DebuggerComponent.Instance.WindowOpacity = 0.9f;
DebuggerComponent.Instance.WindowScale = 1.2f;
```

面板启用后，悬浮按钮双击可展开完整窗口，拖动按钮可移动位置。

## 内置窗口

Debugger 默认注册以下窗口路径：

- `Console`
- `Information/System`
- `Information/Environment`
- `Information/Screen`
- `Information/Graphics`
- `Information/Input/Summary`
- `Information/Input/Touch`
- `Information/Input/Location`
- `Information/Input/Acceleration`
- `Information/Input/Gyroscope`
- `Information/Input/Compass`
- `Information/Other/Scene`
- `Information/Other/Path`
- `Information/Other/Time`
- `Information/Other/Quality`
- `Information/Other/Web Player`
- `Profiler/Summary`
- `Profiler/Memory/Summary`
- `Profiler/Memory/All`
- `Profiler/Memory/Texture`
- `Profiler/Memory/Mesh`
- `Profiler/Memory/Material`
- `Profiler/Memory/Shader`
- `Profiler/Memory/AnimationClip`
- `Profiler/Memory/AudioClip`
- `Profiler/Memory/Font`
- `Profiler/Memory/TextAsset`
- `Profiler/Memory/ScriptableObject`
- `Profiler/Object Pool`
- `Profiler/Reference Pool`
- `Profiler/Audio`
- `Profiler/Timer`
- `Other/Settings`

可以通过路径直接选中窗口：

```csharp
DebuggerComponent.Instance.SelectDebuggerWindow("Profiler/Timer");
```

## 查看最近日志

Console 窗口会监听 `Application.logMessageReceived`，并缓存最近日志。

```csharp
using System.Collections.Generic;
using AlicizaX.Debugger.Runtime;

public sealed class DebugLogExportExample
{
    private readonly List<DebuggerComponent.LogNode> _logs = new();

    public void DumpRecentLogs()
    {
        DebuggerComponent debugger = DebuggerComponent.Instance;
        if (debugger == null)
        {
            return;
        }

        _logs.Clear();
        debugger.GetRecentLogs(_logs, 50);

        foreach (DebuggerComponent.LogNode log in _logs)
        {
            UnityEngine.Debug.Log($"{log.LogType}: {log.LogMessage}");
        }
    }
}
```

`LogNode` 来自 `MemoryPool`，这里只读取，不需要手动释放。

## 注册自定义窗口

自定义窗口需要实现 `IDebuggerWindow`，并返回一个 UI Toolkit `VisualElement`。

```csharp
using AlicizaX.Debugger.Runtime;
using UnityEngine.UIElements;

public sealed class PlayerDebugWindow : IDebuggerWindow
{
    private Label _label;

    public void Initialize(params object[] args)
    {
    }

    public void Shutdown()
    {
    }

    public void OnEnter()
    {
        Refresh();
    }

    public void OnLeave()
    {
    }

    public void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        Refresh();
    }

    public VisualElement CreateView()
    {
        var root = new VisualElement();
        _label = new Label();
        root.Add(_label);
        Refresh();
        return root;
    }

    private void Refresh()
    {
        if (_label != null)
        {
            _label.text = $"Frame: {UnityEngine.Time.frameCount}";
        }
    }
}
```

注册窗口：

```csharp
using AlicizaX.Debugger.Runtime;
using UnityEngine;

public sealed class RegisterPlayerDebugWindow : MonoBehaviour
{
    private void Start()
    {
        DebuggerComponent.Instance.RegisterDebuggerWindow(
            "Gameplay/Player",
            new PlayerDebugWindow());
    }

    private void OnDestroy()
    {
        if (DebuggerComponent.Instance != null)
        {
            DebuggerComponent.Instance.UnregisterDebuggerWindow("Gameplay/Player");
        }
    }
}
```

路径使用 `/` 分组，会显示为侧边栏树形菜单。

## 布局控制

```csharp
using AlicizaX.Debugger.Runtime;
using UnityEngine;

public sealed class DebuggerLayoutExample : MonoBehaviour
{
    private void Start()
    {
        DebuggerComponent debugger = DebuggerComponent.Instance;
        if (debugger == null)
        {
            return;
        }

        debugger.IconRect = new Rect(24f, 24f, 180f, 56f);
        debugger.WindowRect = new Rect(24f, 96f, 1320f, 760f);
        debugger.EnableFloatingToggleSnap = true;
        debugger.ResetLayout();
    }
}
```

## 动态绑定配置

`ModuleDynamicBind` 会在非编辑器环境读取 `Resources/ServiceDynamicBindInfo`，并将其中的 `DebuggerActiveWindowType` 应用到 `DebuggerComponent`。

```csharp
public struct ServiceDynamicBindInfo
{
    public DebuggerActiveWindowType DebuggerActiveWindowType;
    public int ResMode;
    public string Language;
    public string DecryptionServices;
}
```

这适合不同渠道包控制 Debugger 是否打开。

## API 速查

| API | 说明 |
| --- | --- |
| `IDebuggerService.ActiveWindow` | 是否启用调试器 |
| `IDebuggerService.RegisterDebuggerWindow(...)` | 注册调试窗口 |
| `IDebuggerService.UnregisterDebuggerWindow(path)` | 取消注册窗口 |
| `IDebuggerService.GetDebuggerWindow(path)` | 获取窗口 |
| `IDebuggerService.SelectDebuggerWindow(path)` | 选中窗口 |
| `DebuggerComponent.Instance` | 当前调试器组件 |
| `DebuggerComponent.ShowFullWindow` | 是否显示完整窗口 |
| `DebuggerComponent.WindowOpacity` | 窗口透明度 |
| `DebuggerComponent.WindowScale` | 窗口缩放 |
| `DebuggerComponent.ResetLayout()` | 重置布局 |
| `DebuggerComponent.GetRecentLogs(...)` | 获取最近日志 |

## 注意事项

1. Debugger 使用 UI Toolkit，工程需要支持 `UnityEngine.UIElements`。
2. 生产包建议使用 `OnlyOpenWhenDevelopment`、`OnlyOpenInEditor` 或 `AlwaysClose`。
3. 自定义窗口的 `CreateView()` 应只创建视图结构，动态数据在 `OnEnter()` 或 `OnUpdate()` 中刷新。
4. 注册路径不要与内置窗口重复，否则会造成菜单和窗口覆盖。
5. `DebuggerComponent` 会创建自己的 EventSystem；如果场景已有 EventSystem，框架会复用现有输入环境。

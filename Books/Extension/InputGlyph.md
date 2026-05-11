# InputGlyph 输入图标与重绑定

`InputGlyph` 模块基于 Unity Input System，把当前输入设备和 Action 绑定解析成 UI 图标或文本。它还提供运行时按键重绑定、输入轮询和设备类型变化通知。

源码位置：

- `Client/Packages/com.alicizax.unity.ui.extension/Runtime/InputGlyph`
- 编辑器入口：`AlicizaX/Extension/Input/InputGlyph`
- 示例目录：`Client/Packages/com.alicizax.unity.ui.extension/Samples~/InputGlyph`

## 使用前提

需要安装 Unity Input System，并准备：

- `InputActionAsset`：项目输入配置。
- `InputBindingManager`：挂在启动场景或常驻节点上，配置 `actions`。
- `InputGlyphDatabase`：配置不同设备分类下控制路径到 Sprite 的映射。
- `GlyphService.SetDatabase(...)`：运行时注入图标库。

`InputDeviceWatcher` 会在运行前自动初始化，监听键盘、鼠标、Xbox、PlayStation、Switch 和其他手柄。

## 挂载 InputBindingManager

`InputBindingManager` 继承框架服务组件，建议放在启动场景的框架根节点或常驻输入节点上。

Inspector 配置：

| 字段 | 说明 |
| --- | --- |
| `actions` | 要管理的 `InputActionAsset` |
| `debugMode` | 是否输出重绑定调试日志 |

运行时获取：

```csharp
using AlicizaX;

InputBindingManager input = AppServices.Require<InputBindingManager>();
```

也可以用静态方法按名称找 Action：

```csharp
var submit = InputBindingManager.Action("UI/Submit");
```

如果不同 ActionMap 下有同名 Action，请使用 `MapName/ActionName`，避免名称歧义。

## 创建 InputGlyphDatabase

打开编辑器窗口：

```text
AlicizaX/Extension/Input/InputGlyph
```

常用配置：

| 配置 | 说明 |
| --- | --- |
| `Keyboard` | 键盘和鼠标图标表 |
| `Xbox` | Xbox 或 XInput 手柄图标表 |
| `PlayStation` | DualShock、DualSense 图标表 |
| `Switch` | Switch、Nintendo、Joy-Con 图标表 |
| `Other` | 其他手柄兜底图标表 |
| `placeholderSprite` | 找不到匹配图标时使用的占位图 |

每条 `GlyphEntry` 需要配置：

- `Sprite`：显示图标。
- `action`：用于提取绑定路径的 Input Action。

运行时注入数据库：

```csharp
using UnityEngine;

public sealed class InputGlyphBootstrap : MonoBehaviour
{
    [SerializeField] private InputGlyphDatabase glyphDatabase;

    private void Awake()
    {
        GlyphService.SetDatabase(glyphDatabase);
    }

    private void OnDestroy()
    {
        GlyphService.ClearDatabase();
    }
}
```

## 显示按键图标

在 UI 节点上挂 `InputGlyph`。

`Source` 支持三种来源：

| 模式 | 说明 |
| --- | --- |
| `ActionReference` | 直接指定 `InputActionReference` |
| `HotkeyTrigger` | 从同节点或指定组件上的 `IHotkeyTrigger` 读取 Action |
| `ActionName` | 通过 `InputBindingManager.Action(actionName)` 查找 |

`Output` 支持两种输出：

| 模式 | 说明 |
| --- | --- |
| `Image` | 把匹配 Sprite 设置到 `targetImage` |
| `Text` | 把图标 TMP Sprite Tag 或显示名填入 `targetText` |

图片模式：

```csharp
// Inspector 中配置：
// actionSourceMode = ActionReference
// actionReference = UI/Submit
// outputMode = Image
// targetImage = 当前 Image
```

文本模式要求文本里预留 `{0}`：

```text
Press {0} to confirm
```

设备切换或按键重绑定后，`InputGlyph` 会自动刷新。

## 手动查询图标

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class ActionGlyphView : MonoBehaviour
{
    [SerializeField] private InputActionReference action;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI label;

    private void OnEnable()
    {
        InputDeviceWatcher.OnDeviceChanged += OnDeviceChanged;
        InputBindingManager.BindingsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        InputDeviceWatcher.OnDeviceChanged -= OnDeviceChanged;
        InputBindingManager.BindingsChanged -= Refresh;
    }

    private void OnDeviceChanged(InputDeviceWatcher.InputDeviceCategory category)
    {
        Refresh();
    }

    private void Refresh()
    {
        var device = InputDeviceWatcher.CurrentCategory;

        if (GlyphService.TryGetUISpriteForActionPath(action, null, device, out Sprite sprite))
        {
            icon.sprite = sprite;
        }

        label.text = GlyphService.GetDisplayNameFromInputAction(action.action, null, device);
    }
}
```

复合绑定要传 `compositePartName`，例如 `Up`、`Down`、`Left`、`Right`：

```csharp
GlyphService.TryGetUISpriteForActionPath(moveAction, "Up", device, out Sprite upSprite);
```

## 按键重绑定

开始重绑定：

```csharp
InputBindingManager input = AppServices.Require<InputBindingManager>();
input.StartRebind("UI/Submit");
```

复合键重绑定：

```csharp
input.StartRebind("Player/Move", "Up");
```

确认或丢弃：

```csharp
await input.ConfirmApply();

// 或丢弃当前暂存修改。
input.DiscardPrepared();
```

恢复默认：

```csharp
await input.ResetToDefaultAsync();
```

事件订阅：

```csharp
protected override void OnInitialize()
{
    InputBindingManager input = AppServices.Require<InputBindingManager>();
    input.OnRebindStart += OnRebindStart;
    input.OnRebindEnd += OnRebindEnd;
    input.OnRebindConflict += OnRebindConflict;
    input.OnApply += OnRebindApply;
}

private void OnRebindStart()
{
    baseui.TxtTips.text = "Press a key...";
}

private void OnRebindEnd(bool success, InputBindingManager.RebindContext context)
{
    baseui.TxtTips.text = success ? "Rebind ready" : "Rebind canceled";
}

private void OnRebindConflict(
    InputBindingManager.RebindContext prepared,
    InputBindingManager.RebindContext conflict)
{
    Log.Warning($"Input conflict: {prepared} conflicts with {conflict}");
}

private void OnRebindApply(bool success, InputBindingManager.RebindContext[] contexts)
{
    Log.Info(success ? "Input binding saved." : "Input binding discarded.");
}
```

保存位置：

| 环境 | 文件 |
| --- | --- |
| Unity Editor | `Assets/input_bindings.json` |
| Player | `Application.persistentDataPath/input_bindings.json` |

## 输入读取工具

`InputActionReader` 适合在业务逻辑里轮询 Action。

```csharp
using UnityEngine;

public sealed class BattleInput : MonoBehaviour
{
    private void Update()
    {
        Vector2 move = InputActionReader.ReadValue<Vector2>("Player/Move");

        if (InputActionReader.ReadButtonOnce(this, "Player/Interact"))
        {
            Interact();
        }

        bool mapOpened = InputActionReader.ReadButtonToggle(this, "UI/OpenMap");
        SetMapVisible(mapOpened);
    }

    private void Interact()
    {
    }

    private void SetMapVisible(bool visible)
    {
    }
}
```

常用 API：

| API | 说明 |
| --- | --- |
| `ReadValue<T>(string)` | 读取 Action 当前值 |
| `TryReadValue<T>(string, out T)` | 仅按下时读取值 |
| `TryReadValueOnce<T>(Object, string, out T)` | 只在本次按下的第一帧读取值 |
| `ReadButton(string)` | 读取按钮是否按下 |
| `ReadButtonOnce(Object, string)` | 只在按钮按下第一帧返回 true |
| `ReadButtonToggle(Object, string)` | 每次新的按下沿切换开关状态 |
| `ResetToggledButton(string)` | 重置某个 Action 的切换状态 |
| `ResetToggledButtons()` | 清空全部切换状态 |

## 设备切换

监听当前设备分类：

```csharp
private void OnEnable()
{
    InputDeviceWatcher.OnDeviceContextChanged += OnDeviceContextChanged;
}

private void OnDisable()
{
    InputDeviceWatcher.OnDeviceContextChanged -= OnDeviceContextChanged;
}

private void OnDeviceContextChanged(InputDeviceWatcher.DeviceContext context)
{
    Log.Info($"Device: {context.Category}, {context.DeviceName}");
}
```

设备分类：

```csharp
public enum InputDeviceCategory
{
    Keyboard,
    Xbox,
    PlayStation,
    Other,
    Switch
}
```

## API 速查

| API | 说明 |
| --- | --- |
| `GlyphService.SetDatabase(InputGlyphDatabase)` | 注入图标数据库 |
| `GlyphService.TryGetUISpriteForActionPath(...)` | 获取当前 Action 对应 Sprite |
| `GlyphService.TryGetTMPTagForActionPath(...)` | 获取 TMP Sprite Tag |
| `GlyphService.GetDisplayNameFromInputAction(...)` | 获取可读按键名 |
| `InputBindingManager.Action(string)` | 按名称查找 Action |
| `InputBindingManager.StartRebind(string, string)` | 开始交互式重绑定 |
| `InputBindingManager.ConfirmApply(bool)` | 应用并保存暂存重绑定 |
| `InputBindingManager.DiscardPrepared()` | 丢弃暂存重绑定 |
| `InputBindingManager.ResetToDefaultAsync()` | 恢复默认绑定 |
| `InputDeviceWatcher.CurrentCategory` | 当前设备分类 |
| `InputActionReader.ReadButtonOnce(...)` | 读取单次按钮触发 |

## 注意事项

1. `InputGlyph` 不会自动加载 `InputGlyphDatabase`，必须由项目启动逻辑注入。
2. 文本输出模式会把 `{0}` 替换为 TMP Sprite Tag 或显示名，目标文本建议预留一个占位符。
3. `StartRebind` 当前优先寻找键盘绑定，并排除了鼠标移动和滚轮，适合按键设置界面。
4. 同名 Action 如果存在多个 ActionMap，使用 `MapName/ActionName`。
5. 重绑定结果先进入暂存区，只有调用 `ConfirmApply` 后才会写入磁盘并触发 `BindingsChanged`。

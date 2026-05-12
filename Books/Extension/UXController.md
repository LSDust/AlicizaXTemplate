# UXController 和 UXBinding

`UXController` 适合管理同一个 UI 节点内的多状态表现，例如页签选中、按钮禁用、详情展开、品质状态等。`UXBinding` 负责把某个 Controller 的索引映射到目标对象属性。

源码位置：

- `Client/Packages/com.alicizax.unity.ui.extension/Runtime/UXComponent`

## 配置流程

1. 在 UI 根节点或局部节点上挂 `UXController`。
2. 新增 Controller，例如 `ctlLevel`，设置 `Length` 为状态数量。
3. 在需要响应状态的子节点上挂 `UXBinding`。
4. 在 `UXBinding` 中选择 Controller、状态索引和属性，例如 `GameObjectActive`、`GraphicColor`、`ImageSprite`、`TextContent`。
5. 运行时设置 Controller 索引。

## 代码控制

```csharp
using UnityEngine.UI;

public sealed class LevelStatePresenter
{
    private readonly UXController _controller;

    public LevelStatePresenter(UXController controller)
    {
        _controller = controller;
    }

    public void SetLevel(int level)
    {
        int stateIndex = level >= 10 ? 2 : level >= 5 ? 1 : 0;
        _controller.SetControllerIndexByName("ctlLevel", stateIndex);
    }
}
```

如果 Holder 生成器已经识别了 Controller，可以在 Holder 中缓存定义：

```csharp
public UXController.ControllerDefinition CtlLevel { get; private set; }

public override void Awake()
{
    base.Awake();
    var controller = gameObject.GetComponent<UXController>();
    CtlLevel = controller.GetControllerByName("ctlLevel");
}
```

## 支持绑定的属性

| `UXBindingProperty` | 目标组件要求 | 值类型 | 常见用途 |
| --- | --- | --- | --- |
| `GameObjectActive` | 任意 GameObject | `bool` | 页签页面显示/隐藏、状态节点开关 |
| `CanvasGroupAlpha` | `CanvasGroup` | `float` | 淡入淡出、置灰透明度 |
| `CanvasGroupInteractable` | `CanvasGroup` | `bool` | 整块 UI 是否可交互 |
| `CanvasGroupBlocksRaycasts` | `CanvasGroup` | `bool` | 是否阻挡点击 |
| `GraphicColor` | `Graphic` | `Color` | 图片、文本、按钮背景颜色 |
| `GraphicMaterial` | `Graphic` | `Material` | 特效材质切换 |
| `ImageSprite` | `Image` | `Sprite` | 图标、品质框、状态图切换 |
| `TextContent` | `Text` 或 `TextMeshProUGUI` | `string` | 固定状态文案 |
| `TextColor` | `Text` 或 `TextMeshProUGUI` | `Color` | 文案颜色 |
| `RectTransformAnchoredPosition` | `RectTransform` | `Vector2` | 红点、角标、展开位置 |
| `TransformLocalScale` | `Transform` | `Vector3` | 放大选中项、缩放动画初始状态 |
| `TransformLocalEulerAngles` | `Transform` | `Vector3` | 箭头旋转、状态角度 |

## 回退模式

`UXBinding` 的回退模式决定"当前 Controller 索引没有命中这条规则"时怎么处理：

| 回退模式 | 说明 |
| --- | --- |
| `KeepCurrent` | 保持当前值，不做任何改动 |
| `RestoreCapturedDefault` | 恢复绑定时捕获到的默认值 |
| `UseCustomValue` | 使用配置的 fallback 值 |

`GameObjectActive` 比较特殊：编辑器会把它的回退值处理成 `false`，用来做页签页面开关最直接。

## 页签状态完整示例

节点结构：

```text
RoleWindowRoot (UXController)
├── BtnInfo
│   ├── Icon      (UXBinding: Controller=Tab, GraphicColor)
│   └── Label     (UXBinding: Controller=Tab, TextColor)
├── BtnSkill
│   ├── Icon      (UXBinding: Controller=Tab, GraphicColor)
│   └── Label     (UXBinding: Controller=Tab, TextColor)
├── PageInfo      (UXBinding: Controller=Tab, GameObjectActive, Index=0)
├── PageSkill     (UXBinding: Controller=Tab, GameObjectActive, Index=1)
└── PageEquip     (UXBinding: Controller=Tab, GameObjectActive, Index=2)
```

`UXController` 配置：

| 配置 | 值 |
| --- | --- |
| `Name` | `Tab` |
| `Length` | `3` |
| `Default Index` | `0` |

页面节点的 `UXBinding`：

| 节点 | Property | Controller Index | Value | Fallback |
| --- | --- | --- | --- | --- |
| `PageInfo` | `GameObjectActive` | `0` | `true` | `false` |
| `PageSkill` | `GameObjectActive` | `1` | `true` | `false` |
| `PageEquip` | `GameObjectActive` | `2` | `true` | `false` |

窗口代码只负责切换 Controller 索引：

```csharp
using AlicizaX.UI.Runtime;
using Game.UI;
using UnityEngine.UI;

[Window(UILayer.UI)]
public sealed class RoleWindow : UIWindow<ui_RoleWindow>
{
    private UXController _controller;

    protected override void OnInitialize()
    {
        _controller = baseui.GetComponent<UXController>();

        baseui.BtnInfo.onClick.AddListener(() => SetTab(0));
        baseui.BtnSkill.onClick.AddListener(() => SetTab(1));
        baseui.BtnEquip.onClick.AddListener(() => SetTab(2));

        SetTab(0);
    }

    private void SetTab(int index)
    {
        _controller.SetControllerIndexByName("Tab", index);
    }
}
```

## 品质状态示例

```csharp
public sealed class ItemQualityPresenter
{
    private readonly UXController _controller;

    public ItemQualityPresenter(UXController controller)
    {
        _controller = controller;
    }

    public void SetQuality(int quality)
    {
        int index = Mathf.Clamp(quality - 1, 0, 4);
        _controller.SetControllerIndexByName("Quality", index);
    }
}
```

## API 速查

| API | 说明 |
| --- | --- |
| `UXController.GetControllerByName(string)` | 按名称获取 Controller 定义 |
| `UXController.SetControllerIndexByName(string, int)` | 按 Controller 名称切换状态 |
| `UXController.ResetAllControllers()` | 重置所有 Controller 到默认索引 |
| `UXBinding.CaptureDefaults()` | 捕获当前属性值为默认值 |
| `UXBinding.ResetToDefaults()` | 恢复已捕获的默认值 |
| `UXBinding.PreviewEntry(int)` | 编辑器或调试时预览某条绑定 |

## 注意事项

1. Controller 名称应使用业务语义，例如 `Tab`、`Quality`、`Expand`，不要用 `Controller1`。
2. Controller 的 `Length` 变化后要检查相关 `UXBinding`，编辑器会尝试夹紧越界索引，但业务含义仍然需要确认。
3. `UXBinding` 目标缺少对应组件时不会生效，例如 `CanvasGroupAlpha` 必须挂 `CanvasGroup`，`ImageSprite` 必须挂 `Image`。
4. 运行时优先使用 `SetControllerIndexByName`，只有需要稳定引用时再缓存 `ControllerDefinition`。

# UXComponent 扩展组件

`UXComponent` 提供一组基于 UGUI 的增强控件。它们主要解决按钮音效、子节点状态切换、Toggle 分组、渐变图片、状态绑定、快捷键触发和本地化文本等问题。

源码位置：

- `Client/Packages/com.alicizax.unity.ui.extension/Runtime/UXComponent`
- 编辑器入口：`Client/Packages/com.alicizax.unity.ui.extension/Editor/UX`

## UXButton

`UXButton` 继承 `UXSelectable`，用法接近 Unity `Button`，仍然通过 `onClick` 注册点击事件。额外支持：

- 指针悬停和点击音效。
- 非鼠标选择时播放悬停音效。
- 子节点随按钮状态同步颜色或图片。
- Input System 导航选中通知。

```csharp
using AlicizaX;
using AlicizaX.UI.Runtime;
using Game.UI;
using UnityEngine.UI;

[Window(UILayer.UI)]
public sealed class LoginWindow : UIWindow<ui_LoginWindow>
{
    protected override void OnInitialize()
    {
        baseui.BtnLogin.onClick.AddListener(OnLoginClick);
        baseui.BtnClose.onClick.AddListener(CloseSelf);
    }

    private void OnLoginClick()
    {
        // 执行登录逻辑。
    }
}
```

如果要接入项目自己的 UI 音效播放，可以在启动时注入音频适配器：

```csharp
using AlicizaX;
using AlicizaX.Audio.Runtime;
using UnityEngine;

public sealed class UXAudioAdapter : IUXAudioHelper
{
    public void PlayAudio(AudioClip clip)
    {
        GameApp.Audio.Play(AudioType.UISound, clip);
    }

    public void PlayAudio(string clipName)
    {
        GameApp.Audio.Play(AudioType.UISound, clipName);
    }
}

UXComponentExtensionsHelper.SetAudioHelper(new UXAudioAdapter());
```

Inspector 中常用配置：

| 配置 | 说明 |
| --- | --- |
| `Hover Audio Clip` | 指针进入按钮、键盘或手柄导航选中按钮时播放 |
| `Click Audio Clip` | 左键点击或提交按钮时播放 |
| `Transition` | 沿用 `Selectable` 的状态过渡，通常用 `ColorTint` 或 `SpriteSwap` |
| `Navigation` | 沿用 UGUI 导航配置，可与 `UXNavigationScope` 配合 |
| `Child Transitions` | 继承自 `UXSelectable`，让图标、文本等子节点跟随状态变化 |

`UXButton` 的点击行为和 Unity `Button` 基本一致，但有几个差异需要注意：

1. 只响应左键点击，右键不会触发 `onClick`。
2. `OnSubmit` 会触发 `onClick`，所以键盘、手柄和 `HotkeyComponent` 都可以复用同一套按钮逻辑。
3. `onClick` 的类型仍然是 `Button.ButtonClickedEvent`，旧代码里 `baseui.BtnConfirm.onClick.AddListener(...)` 的写法可以保留。
4. 按钮禁用或节点未激活时，`Press()` 会直接返回，不会触发业务回调。
5. 音效播放依赖 `UXComponentExtensionsHelper.AudioHelper`，没有注入音频适配器时不会播放，也不会报错。

一个常见的确认弹窗写法如下：

```csharp
using AlicizaX.UI.Runtime;
using Game.UI;

[Window(UILayer.Popup)]
public sealed class DeleteConfirmWindow : UIWindow<ui_DeleteConfirmWindow>
{
    private int _targetId;

    public void SetTarget(int targetId)
    {
        _targetId = targetId;
    }

    protected override void OnInitialize()
    {
        baseui.BtnConfirm.onClick.AddListener(OnConfirmClick);
        baseui.BtnCancel.onClick.AddListener(CloseSelf);
    }

    private void OnConfirmClick()
    {
        GameApp.Inventory.DeleteItem(_targetId);
        CloseSelf();
    }
}
```

如果同一个按钮需要临时禁用，直接设置 `interactable`：

```csharp
baseui.BtnConfirm.interactable = CanDeleteItem(_targetId);
```

## UXSelectable 子节点状态

`UXSelectable` 继承 `Selectable`，并增加子节点过渡列表。适合一个按钮包含图标、文本、背景多层视觉时统一处理状态。

Inspector 中为按钮配置：

| 配置 | 说明 |
| --- | --- |
| `Transition` | 沿用 UGUI 的 `ColorTint` 或 `SpriteSwap` |
| `Child Transitions` | 需要跟随按钮状态变化的子 `Graphic` |
| `TransitionData.colors` | 子节点各状态颜色 |
| `TransitionData.spriteState` | 子节点各状态图片 |

常见用法：

1. 根节点挂 `UXButton`。
2. 背景、图标、文本分别作为子节点。
3. 在 `Child Transitions` 中加入图标或文本。
4. 点击逻辑仍然使用 `baseui.BtnXxx.onClick`。

例如一个带图标和文字的页签按钮：

```text
BtnBag (UXButton)
├── Bg      (Image，作为按钮根节点 Target Graphic 或普通背景)
├── Icon    (Image，加入 Child Transitions)
└── Label   (TextMeshProUGUI，加入 Child Transitions)
```

推荐配置方式：

| 节点 | 配置 |
| --- | --- |
| `BtnBag` | `Transition = ColorTint`，根背景使用按钮自己的 `Colors` |
| `Icon` | 在 `Child Transitions` 里配置普通、悬停、按下、选中、禁用颜色 |
| `Label` | 在 `Child Transitions` 里配置同一套文本颜色 |

这样业务代码只需要处理点击：

```csharp
protected override void OnInitialize()
{
    baseui.BtnBag.onClick.AddListener(() => SwitchTab(0));
    baseui.BtnEquip.onClick.AddListener(() => SwitchTab(1));
}
```

如果根节点 `Transition` 选择 `SpriteSwap`，子节点也会按 `SpriteState` 切图；如果根节点选择 `ColorTint`，子节点会按 `ColorBlock` 变色。当前实现里子节点列表中的 `TransitionData.transition` 字段不会单独决定过渡类型，最终使用的是根 `UXSelectable.transition`。因此同一个控件内不要混用“根节点颜色过渡、子节点图片过渡”这类配置；需要混用时，建议拆成两个控件或用 `UXController + UXBinding` 控制额外状态。

导航行为也做了增强：当 `Navigation.Mode` 是 `Explicit` 时，如果显式配置的目标不可交互，`FindSelectableOnLeft/Right/Up/Down` 会返回 `null`，避免焦点跳到禁用按钮上；当是 `Horizontal` 或 `Vertical` 自动导航时，则沿用 UGUI 的方向查找逻辑。

## UXToggle 和 UXGroup

`UXToggle` 类似 Unity `Toggle`，通过 `isOn` 和 `onValueChanged` 控制状态。`UXGroup` 管理一组选项，支持默认选中、禁止全部关闭、切换到上一项或下一项。

```csharp
using AlicizaX;
using AlicizaX.UI.Runtime;
using Game.UI;
using UnityEngine.UI;

[Window(UILayer.UI)]
public sealed class SettingWindow : UIWindow<ui_SettingWindow>
{
    protected override void OnInitialize()
    {
        baseui.TglMusic.onValueChanged.AddListener(OnMusicChanged);
        baseui.TglSfx.onValueChanged.AddListener(OnSfxChanged);
    }

    private void OnMusicChanged(bool isOn)
    {
        GameApp.Audio.SetCategoryEnable(AlicizaX.Audio.Runtime.AudioType.Music, isOn);
    }

    private void OnSfxChanged(bool isOn)
    {
        GameApp.Audio.SetCategoryEnable(AlicizaX.Audio.Runtime.AudioType.UISound, isOn);
    }
}
```

手动控制分组：

```csharp
UXGroup group = baseui.TabGroup;

group.allowSwitchOff = false;
group.Next();
group.Previous();
group.SetAllTogglesOff(sendCallback: false);
```

`UXToggle` 的核心配置：

| 配置 | 说明 |
| --- | --- |
| `Is On` | 当前是否选中 |
| `Graphic` | 选中标记，例如勾选图、页签底线、高亮框 |
| `Toggle Transition` | `None` 立即切换，`Fade` 使用透明度淡入淡出 |
| `Group` | 所属 `UXGroup`，有分组时自动互斥 |
| `Hover Audio Clip` / `Click Audio Clip` | 与 `UXButton` 一样使用注入的音频适配器 |

`UXToggle` 选中后会把自身视觉状态强制当作 `Selected`，所以 `UXSelectable` 的子节点状态也能用于“选中页签文字变色、图标变亮”这类效果。`SetIsOnWithoutNotify` 适合初始化或同步服务端状态，避免触发业务回调。

`UXGroup` 的核心配置：

| 配置 | 说明 |
| --- | --- |
| `Allow Switch Off` | 是否允许组内没有任何 Toggle 选中 |
| `Default Toggle` | 没有选中项时默认恢复到哪个 Toggle |
| `Toggles` | 当前组管理的 Toggle 数组 |

编辑器里 `UXGroup` 提供三个辅助按钮：

| 按钮 | 作用 |
| --- | --- |
| `Collect Children` | 收集当前节点子树下的 `UXToggle` |
| `Clean Nulls` | 清理数组里的空引用 |
| `Sort By Hierarchy` | 按层级顺序排序，影响 `Next()` 和 `Previous()` 的切换顺序 |

页签窗口的完整示例：

```csharp
using AlicizaX.UI.Runtime;
using Game.UI;
using UnityEngine.UI;

[Window(UILayer.UI)]
public sealed class RoleWindow : UIWindow<ui_RoleWindow>
{
    private UXGroup _tabGroup;

    protected override void OnInitialize()
    {
        _tabGroup = baseui.TabGroup;
        _tabGroup.allowSwitchOff = false;

        baseui.TglInfo.onValueChanged.AddListener(isOn =>
        {
            if (isOn) SwitchTab(0);
        });
        baseui.TglSkill.onValueChanged.AddListener(isOn =>
        {
            if (isOn) SwitchTab(1);
        });
        baseui.TglEquip.onValueChanged.AddListener(isOn =>
        {
            if (isOn) SwitchTab(2);
        });

        baseui.TglInfo.SetIsOnWithoutNotify(true);
        SwitchTab(0);
    }

    private void SwitchTab(int index)
    {
        baseui.PageInfo.SetActive(index == 0);
        baseui.PageSkill.SetActive(index == 1);
        baseui.PageEquip.SetActive(index == 2);
    }

    private void SelectNextTab()
    {
        _tabGroup.Next();
    }

    private void SelectPreviousTab()
    {
        _tabGroup.Previous();
    }
}
```

如果一个 Toggle 在运行时被禁用，`UXGroup.Next()` 和 `UXGroup.Previous()` 会跳过不可用或不可交互的项。

## UXImage

`UXImage` 继承 `Image`，支持纯色、渐变和镜像绘制。适合做进度、背景块、左右或四角对称图。

```csharp
using UnityEngine;
using UnityEngine.UI;

public static class UXImageExample
{
    public static void SetGradient(UXImage image)
    {
        image.m_ColorType = UXImage.ColorType.Gradient_Color;
        image.Direction = UXImage.GradientDirection.Horizontal;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.red, 0f),
                new GradientColorKey(Color.yellow, 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            });

        image.gradient = gradient;
    }

    public static void SetMirror(UXImage image)
    {
        image.flipMode = UXImage.FlipMode.Horziontal;
        image.flipWithCopy = true;
        image.flipEdgeHorizontal = UXImage.FlipEdgeHorizontal.Right;
    }
}
```

`UXImage` 仍然保留 `Image` 的 `sprite`、`type`、`fillAmount`、`preserveAspect` 等能力，只是在顶点生成阶段额外处理渐变和镜像。

颜色模式：

| 模式 | 说明 |
| --- | --- |
| `Solid_Color` | 与普通 `Image` 一样使用 `color` |
| `Gradient_Color` | 使用 `gradient` 生成顶点色，方向由 `Direction` 决定 |

渐变方向：

| 方向 | 效果 |
| --- | --- |
| `Vertical` | 从下到上采样渐变 |
| `Horizontal` | 从左到右采样渐变 |

适合用渐变的场景：

1. 经验条、血条、加载条，不想额外出渐变贴图。
2. 品质背景、按钮底色，需要在同一 Sprite 上换不同渐变。
3. 纯色块背景，希望减少美术贴图数量。

运行时更新进度条颜色和进度：

```csharp
using UnityEngine;
using UnityEngine.UI;

public sealed class HpBarPresenter
{
    private readonly UXImage _fillImage;

    public HpBarPresenter(UXImage fillImage)
    {
        _fillImage = fillImage;
        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.m_ColorType = UXImage.ColorType.Gradient_Color;
        _fillImage.Direction = UXImage.GradientDirection.Horizontal;
    }

    public void SetValue(float current, float max)
    {
        float ratio = max <= 0f ? 0f : Mathf.Clamp01(current / max);
        _fillImage.fillAmount = ratio;
        _fillImage.gradient = ratio < 0.3f ? BuildLowHpGradient() : BuildNormalGradient();
    }

    private static Gradient BuildNormalGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.2f, 0.8f, 0.35f), 0f),
                new GradientColorKey(new Color(0.85f, 1f, 0.35f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            });
        return gradient;
    }

    private static Gradient BuildLowHpGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.8f, 0.1f, 0.1f), 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.15f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            });
        return gradient;
    }
}
```

镜像模式：

| 配置 | 说明 |
| --- | --- |
| `flipMode = None` | 不镜像 |
| `flipMode = Horziontal` | 水平方向镜像，枚举名里 `Horziontal` 是源码中的实际拼写 |
| `flipMode = Vertical` | 垂直方向镜像 |
| `flipMode = FourCorner` | 四角镜像，适合四角对称装饰 |
| `flipWithCopy = true` | 复制一份顶点后镜像，适合用半张图生成完整对称图 |
| `flipWithCopy = false` | 不复制，只把当前图翻转 |

镜像不是简单修改 `RectTransform` 尺寸，而是在 `OnPopulateMesh` 里复制或重映射顶点。也就是说，控件的布局尺寸仍然由 RectTransform 决定；如果你想让“半张图复制成左右完整图”占用更多布局空间，需要自己设置 RectTransform 宽高。

对称标题底纹示例：

```csharp
public static void SetupTitleDecoration(UXImage image)
{
    image.type = Image.Type.Simple;
    image.flipMode = UXImage.FlipMode.Horziontal;
    image.flipWithCopy = true;
    image.flipEdgeHorizontal = UXImage.FlipEdgeHorizontal.Right;
}
```

## UXTextMeshPro

`UXTextMeshPro` 继承 `TextMeshProUGUI`，可以通过本地化 key 自动刷新文本。项目需要先注入本地化适配器。

```csharp
using AlicizaX;

public sealed class UXLocalizationAdapter : IUXLocalizationHelper
{
    public string GetString(string key)
    {
        return GameApp.Localization.GetString(key);
    }
}

UXComponentExtensionsHelper.SetLocalizationHelper(new UXLocalizationAdapter());
```

运行时切换文本：

```csharp
baseui.TxtTitle.SetLocalization("ui.login.title");
```

`UXTextMeshPro` 需要工程启用 `TEXTMESHPRO_SUPPORT` 宏，否则该类不会参与编译。它适合固定 UI 文案，例如标题、按钮文字、提示标签；动态拼接的数值文本可以继续直接设置 `text`。

Inspector 中配置 `Localization Key` 后，运行时 `Start()` 会调用本地化适配器刷新一次文本。编辑器下 `OnValidate()` 会通过预览接口尝试显示 key 对应的预览文本，方便在 Prefab 里检查文案长度。

登录窗口示例：

```csharp
using AlicizaX.UI.Runtime;
using Game.UI;

[Window(UILayer.UI)]
public sealed class LoginWindow : UIWindow<ui_LoginWindow>
{
    protected override void OnInitialize()
    {
        baseui.TxtTitle.SetLocalization("ui.login.title");
        baseui.TxtLogin.SetLocalization("ui.login.button");
        baseui.TxtVersion.text = $"v{GameApp.Version}";
    }
}
```

切换语言时，如果窗口仍然显示，需要重新调用 `SetLocalization` 或让项目的语言切换事件统一刷新当前 UI。`UXTextMeshPro` 当前只在 `Start()` 和 `SetLocalization()` 时主动刷新，不会自动订阅项目语言变化事件。

## UXController 和 UXBinding

`UXController` 适合管理同一个 UI 节点内的多状态表现，例如页签选中、按钮禁用、详情展开、品质状态等。`UXBinding` 负责把某个 Controller 的索引映射到目标对象属性。

常见配置流程：

1. 在 UI 根节点或局部节点上挂 `UXController`。
2. 新增 Controller，例如 `ctlLevel`，设置 `Length` 为状态数量。
3. 在需要响应状态的子节点上挂 `UXBinding`。
4. 在 `UXBinding` 中选择 Controller、状态索引和属性，例如 `GameObjectActive`、`GraphicColor`、`ImageSprite`、`TextContent`。
5. 运行时设置 Controller 索引。

代码控制：

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

如果 Holder 生成器已经识别了 Controller，也可以像项目示例一样在 Holder 中缓存：

```csharp
public UXController.ControllerDefinition CtlLevel { get; private set; }

public override void Awake()
{
    base.Awake();
    var controller = gameObject.GetComponent<UXController>();
    CtlLevel = controller.GetControllerByName("ctlLevel");
}
```

支持绑定的属性如下：

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

`UXBinding` 的回退模式决定“当前 Controller 索引没有命中这条规则”时怎么处理：

| 回退模式 | 说明 |
| --- | --- |
| `KeepCurrent` | 保持当前值，不做任何改动 |
| `RestoreCapturedDefault` | 恢复绑定时捕获到的默认值 |
| `UseCustomValue` | 使用配置的 fallback 值 |

`GameObjectActive` 比较特殊：它通常用于“命中时显示，不命中时隐藏”。编辑器会把它的回退值处理成 `false`，所以用来做页签页面开关最直接。

页签状态的完整配置示例：

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

`UXController` 上新增 Controller：

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

页签按钮文字颜色可以为同一个节点配置多个索引值：

| 节点 | Property | Index 0 | Index 1 | Index 2 |
| --- | --- | --- | --- | --- |
| `BtnInfo/Label` | `TextColor` | 选中色 | 未选中色 | 未选中色 |
| `BtnSkill/Label` | `TextColor` | 未选中色 | 选中色 | 未选中色 |
| `BtnEquip/Label` | `TextColor` | 未选中色 | 未选中色 | 选中色 |

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

品质状态也适合用 `UXController`，例如 `Quality` 长度为 5，绑定背景颜色、边框 Sprite、品质文字：

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

使用建议：

1. 状态名用业务语义，例如 `Tab`、`Quality`、`Expand`，不要用 `Controller1`。
2. Controller 的 `Length` 变化后要检查相关 `UXBinding`，编辑器会尝试夹紧越界索引，但业务含义仍然需要确认。
3. 绑定目标缺少对应组件时，这条绑定不会生效。例如 `CanvasGroupAlpha` 必须挂 `CanvasGroup`。
4. `CaptureDefaults()` 用于把当前属性保存成默认值；`ResetToDefaults()` 可以恢复捕获的默认值。
5. 运行时优先使用 `SetControllerIndexByName`，只有需要稳定引用时再缓存 `ControllerDefinition`。

## UXDraggable

`UXDraggable` 把 UGUI 的拖拽接口转成 UnityEvent，适合不想单独写 `IDragHandler` 的简单拖拽。

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DragPanel : MonoBehaviour
{
    [SerializeField] private UXDraggable draggable;
    [SerializeField] private RectTransform target;

    private Vector2 _startPosition;

    private void Awake()
    {
        draggable.onBeginDrag.AddListener(OnBeginDrag);
        draggable.onDrag.AddListener(OnDrag);
        draggable.onEndDrag.AddListener(OnEndDrag);
    }

    private void OnDestroy()
    {
        draggable.onBeginDrag.RemoveListener(OnBeginDrag);
        draggable.onDrag.RemoveListener(OnDrag);
        draggable.onEndDrag.RemoveListener(OnEndDrag);
    }

    private void OnBeginDrag(PointerEventData eventData)
    {
        _startPosition = target.anchoredPosition;
    }

    private void OnDrag(PointerEventData eventData)
    {
        target.anchoredPosition += eventData.delta;
    }

    private void OnEndDrag(PointerEventData eventData)
    {
        SavePanelPosition(target.anchoredPosition);
    }
}
```

`eventData.delta` 是屏幕空间像素增量。如果 Canvas 使用了缩放，直接把 `delta` 加到 `anchoredPosition` 可能会偏快或偏慢，建议除以 Canvas 的 `scaleFactor`。

可拖拽弹窗头部示例：

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DraggableWindowPresenter : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private UXDraggable titleBar;
    [SerializeField] private RectTransform panel;
    [SerializeField] private RectTransform limitRoot;

    private void Awake()
    {
        titleBar.onDrag.AddListener(OnTitleDrag);
    }

    private void OnDestroy()
    {
        titleBar.onDrag.RemoveListener(OnTitleDrag);
    }

    private void OnTitleDrag(PointerEventData eventData)
    {
        float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        Vector2 next = panel.anchoredPosition + eventData.delta / scale;
        panel.anchoredPosition = ClampToRoot(next);
    }

    private Vector2 ClampToRoot(Vector2 position)
    {
        Rect rootRect = limitRoot.rect;
        Rect panelRect = panel.rect;

        float halfWidth = panelRect.width * 0.5f;
        float halfHeight = panelRect.height * 0.5f;

        return new Vector2(
            Mathf.Clamp(position.x, rootRect.xMin + halfWidth, rootRect.xMax - halfWidth),
            Mathf.Clamp(position.y, rootRect.yMin + halfHeight, rootRect.yMax - halfHeight));
    }
}
```

`UXDraggable` 只负责转发事件，不会自动移动对象，也不会自动限制范围。需要吸附、边界、保存位置时，都应在监听函数里完成。

## HotkeyComponent

`HotkeyComponent` 依赖 Unity Input System。它会把配置的 `InputActionReference` 转换为目标组件的 `ISubmitHandler.OnSubmit` 调用。常见目标是 `UXButton` 或 `UXToggle`。

配置方式：

1. 给按钮节点挂 `HotkeyComponent`。
2. `Component` 指向同节点上的 `UXButton`。
3. `Holder` 会自动查找父级 `UIHolderObjectBase`。
4. `HotkeyAction` 指向 Input Action。
5. 窗口显示时快捷键生效，窗口关闭或节点禁用时自动解绑。

示例：

```csharp
using AlicizaX.UI.Runtime;
using Game.UI;

[Window(UILayer.Popup)]
public sealed class ConfirmWindow : UIWindow<ui_ConfirmWindow>
{
    protected override void OnInitialize()
    {
        baseui.BtnConfirm.onClick.AddListener(OnConfirm);
        baseui.BtnCancel.onClick.AddListener(CloseSelf);
    }

    private void OnConfirm()
    {
        CloseSelf();
    }
}
```

按钮上的 `HotkeyComponent` 会直接触发 `BtnConfirm` 的提交逻辑，不需要窗口额外轮询输入。

`HotkeyComponent` 的字段含义：

| 字段 | 说明 |
| --- | --- |
| `Component` | 实际接收提交事件的组件，必须实现 `ISubmitHandler` |
| `Holder` | 所属 `UIHolderObjectBase`，用于判断窗口生命周期和优先级 |
| `HotkeyAction` | `InputActionReference`，例如 Confirm、Cancel、Submit |
| `Hotkey Press Type` | `Started` 或 `Performed`，决定在 Input Action 哪个阶段触发 |

运行时生命周期：

1. `Awake` 和 `OnEnable` 会自动查找同节点上的 `ISubmitHandler`，并查找父级 `UIHolderObjectBase`。
2. `OnEnable` 注册快捷键，`OnDisable` 和 `OnDestroy` 解绑快捷键。
3. 如果 `InputAction` 原本没有启用，快捷键系统会临时启用；最后一个注册者解绑后会恢复禁用。
4. 触发快捷键时，组件会调用目标组件的 `OnSubmit(BaseEventData)`，不会直接调用 `onClick`。

因为 `UXButton.OnSubmit` 会触发 `onClick`，所以按钮快捷键通常只要这样配：

```text
BtnConfirm (UXButton + HotkeyComponent)
├── Component: BtnConfirm 上的 UXButton
├── Holder: 自动找到 ConfirmWindow 的 UIHolderObjectBase
├── HotkeyAction: UI/Submit
└── HotkeyPressType: Performed
```

快捷键优先级由窗口作用域决定，不是所有打开窗口都会同时响应。系统会在当前可见 UI 中选择最上层叶子窗口：

| 优先规则 | 说明 |
| --- | --- |
| `Canvas.sortingOrder` 更高 | 优先响应 |
| 层级更深 | 当 sortingOrder 相同，子窗口优先 |
| 更晚激活 | 当前两项仍相同，后显示的窗口优先 |
| Canvas 层必须等于 `UIComponent.UIShowLayer` | 不在 UI 显示层的窗口不会成为快捷键作用域 |

同一个窗口内多个控件绑定同一个 `InputAction` 时，后注册的触发器优先。通常不建议在一个窗口里让多个按钮共用同一个确认快捷键，除非你明确希望最近启用的控件接管该快捷键。

带取消快捷键的弹窗示例：

```csharp
using AlicizaX.UI.Runtime;
using Game.UI;

[Window(UILayer.Popup)]
public sealed class ConfirmWindow : UIWindow<ui_ConfirmWindow>
{
    protected override void OnInitialize()
    {
        baseui.BtnConfirm.onClick.AddListener(OnConfirm);
        baseui.BtnCancel.onClick.AddListener(OnCancel);
    }

    private void OnConfirm()
    {
        GameApp.Save.ApplyPendingChange();
        CloseSelf();
    }

    private void OnCancel()
    {
        GameApp.Save.DiscardPendingChange();
        CloseSelf();
    }
}
```

Prefab 配置：

```text
BtnConfirm: HotkeyAction = UI/Submit
BtnCancel : HotkeyAction = UI/Cancel
```

如果快捷键没有触发，优先检查这些项：

1. 工程是否启用了 `INPUTSYSTEM_SUPPORT`。
2. `HotkeyAction` 是否为空，Input Action 是否能正常触发。
3. `Component` 是否实现 `ISubmitHandler`。
4. 按钮是否处于激活和可交互状态。
5. 当前窗口的 Canvas 是否在 `UIComponent.UIShowLayer`。
6. 是否有更上层弹窗拦截了同一个快捷键。

## UXNavigationScope 和 UXNavigationSkip

`UXNavigationScope` 依赖 Unity Input System，并且需要同时启用 `INPUTSYSTEM_SUPPORT` 和 `UX_NAVIGATION` 宏。它用于管理键盘、手柄导航时“当前应该在哪个 UI 范围内移动焦点”，适合主菜单、弹窗、列表窗口和多层 UI。

它解决的问题是：当多个窗口同时存在时，手柄焦点不应该跳到下层窗口；当用户切到键盘或手柄输入时，当前窗口应该有一个可用选中项；当弹窗关闭后，导航焦点应该回到合适的上层作用域。

常用字段：

| 字段 | 说明 |
| --- | --- |
| `Default Selectable` | 进入该导航域时优先选中的控件 |
| `Holder` | 所属 `UIHolderObjectBase`，用于窗口显示/关闭生命周期 |
| `Baked Selectables` | 编辑器烘焙的可导航控件列表 |
| `Runtime Selectable Capacity` | 运行时动态注册控件容量，例如虚拟列表项 |
| `Remember Last Selection` | 回到该作用域时是否优先恢复上次选中 |
| `Require Selection When Gamepad` | 键盘/手柄模式下是否强制保持一个选中项 |
| `Block Lower Scopes` | 当前作用域在上层时，是否禁用下层作用域导航 |
| `Auto Select First Available` | 没有默认项时是否自动选择第一个可用控件 |

基础配置流程：

1. 在窗口根节点或导航范围根节点挂 `UXNavigationScope`。
2. 设置 `Holder` 为当前窗口的 `UIHolderObjectBase`；编辑器按钮也可以自动绑定。
3. 设置 `Default Selectable`，例如默认按钮或第一个页签。
4. 点击 `Refresh` 烘焙当前作用域下的 `Selectable`。
5. 打开窗口后，键盘或手柄输入会让该作用域成为候选导航域。

弹窗示例：

```text
ConfirmWindowRoot (Canvas + UIHolderObjectBase + UXNavigationScope)
├── TxtMessage
├── BtnConfirm (UXButton)
└── BtnCancel  (UXButton)
```

推荐配置：

| 字段 | 值 |
| --- | --- |
| `Default Selectable` | `BtnConfirm` |
| `Remember Last Selection` | `true` |
| `Require Selection When Gamepad` | `true` |
| `Block Lower Scopes` | `true` |
| `Auto Select First Available` | `true` |

当这个弹窗位于顶层时，下层窗口的导航会被压制，`Selectable.navigation` 会临时切到 `None`；弹窗关闭或作用域不可用后会恢复之前的导航配置。

运行时动态控件可以注册到当前作用域：

```csharp
using UnityEngine.UI;

public sealed class RuntimeOption : MonoBehaviour
{
    [SerializeField] private UXNavigationScope scope;
    [SerializeField] private UXButton button;

    private void OnEnable()
    {
        scope.RegisterSelectable(button);
    }

    private void OnDisable()
    {
        scope.UnregisterSelectable(button);
    }
}
```

如果某个子树不希望参与导航，可以在该节点上挂 `UXNavigationSkip`。例如隐藏调试按钮、装饰性按钮、临时禁用区域：

```text
SettingsWindowRoot (UXNavigationScope)
├── NormalOptions
│   ├── BtnAudio
│   └── BtnVideo
└── DebugOnlyArea (UXNavigationSkip)
    └── BtnClearCache
```

`UXNavigationRuntime` 会根据输入设备自动切换输入模式：

| 输入模式 | 来源 |
| --- | --- |
| `Pointer` | 鼠标移动、滚轮、鼠标按钮 |
| `Keyboard` | 任意键盘输入 |
| `Gamepad` | 手柄按钮、摇杆、十字键 |
| `Touch` | 触摸按下或移动 |

如果项目需要根据输入模式显示或隐藏鼠标光标，可以实现 `IUXNavigationCursorPolicy` 并注入：

```csharp
using UnityEngine.UI;

public sealed class GameCursorPolicy : IUXNavigationCursorPolicy
{
    public void OnNavigationContextChanged(
        UXInputMode mode,
        UXNavigationScope previousTopScope,
        UXNavigationScope currentTopScope)
    {
        bool pointerMode = mode == UXInputMode.Pointer || mode == UXInputMode.Touch;
        Cursor.visible = pointerMode;
    }
}

UXNavigationRuntime.SetCursorPolicy(new GameCursorPolicy());
```

## API 速查

| API | 说明 |
| --- | --- |
| `UXButton.onClick` | 按钮点击事件 |
| `UXSelectable.navigation` | 配置方向导航 |
| `UXToggle.isOn` | 读取或设置 Toggle 状态 |
| `UXToggle.SetIsOnWithoutNotify(bool)` | 不触发回调地设置 Toggle |
| `UXGroup.Next()` | 选中下一项 |
| `UXGroup.Previous()` | 选中上一项 |
| `UXGroup.GetFirstActiveToggle()` | 获取当前选中的 Toggle |
| `UXGroup.SetAllTogglesOff(bool)` | 清空选中状态 |
| `UXImage.gradient` | 设置渐变 |
| `UXImage.Direction` | 设置渐变方向 |
| `UXImage.flipMode` | 设置镜像模式 |
| `UXImage.flipWithCopy` | 设置镜像时是否复制原图 |
| `UXTextMeshPro.SetLocalization(string)` | 设置本地化 key 并刷新文本 |
| `UXController.GetControllerByName(string)` | 按名称获取 Controller 定义 |
| `UXController.SetControllerIndexByName(string, int)` | 按 Controller 名称切换状态 |
| `UXController.ResetAllControllers()` | 重置所有 Controller 到默认索引 |
| `UXBinding.CaptureDefaults()` | 捕获默认值 |
| `UXBinding.ResetToDefaults()` | 恢复已捕获的默认值 |
| `UXBinding.PreviewEntry(int)` | 编辑器或调试时预览某条绑定 |
| `UXDraggable.onBeginDrag` | 开始拖拽事件 |
| `UXDraggable.onDrag` | 拖拽事件 |
| `UXDraggable.onEndDrag` | 结束拖拽事件 |
| `HotkeyComponent.HotkeyAction` | 设置快捷键 Input Action |
| `UXHotkeyExtension.BindHotKey()` | 手动注册快捷键触发器 |
| `UXHotkeyExtension.UnBindHotKey()` | 手动解绑快捷键触发器 |
| `UXNavigationScope.RegisterSelectable(Selectable)` | 注册运行时生成的可导航控件 |
| `UXNavigationScope.UnregisterSelectable(Selectable)` | 注销运行时生成的可导航控件 |
| `UXNavigationScope.NotifySelectableStateChanged()` | 可交互状态变化后通知导航域刷新 |
| `UXNavigationRuntime.SetCursorPolicy(...)` | 设置输入模式和导航域变化时的光标策略 |
| `UXComponentExtensionsHelper.SetAudioHelper(...)` | 注入 UI 音效播放 |
| `UXComponentExtensionsHelper.SetLocalizationHelper(...)` | 注入本地化查询 |

## 注意事项

1. `UXButton` 不继承 Unity `Button`，但保留了 `Button.ButtonClickedEvent` 类型的 `onClick`，业务调用方式基本一致。
2. `UXSelectable` 的子节点状态使用当前控件的 `transition` 类型决定是颜色还是图片切换。
3. `UXTextMeshPro` 只有在注入 `IUXLocalizationHelper` 后才会根据 key 刷新文本。
4. `HotkeyComponent` 只转发到 `ISubmitHandler`，目标组件必须实现提交接口，例如 `UXButton`、`UXToggle`。
5. `UXImage.FlipMode.Horziontal` 的拼写来自源码枚举，代码里需要使用这个实际名称。
6. `UXController` 的 Controller 名称应保持唯一；编辑器会尝试处理重复名称，但业务代码按名称查找时仍建议使用清晰命名。
7. `UXBinding` 目标缺少对应组件时不会生效，例如绑定 `ImageSprite` 的节点必须有 `Image`。
8. `UXNavigationScope`、`UXNavigationRuntime` 和 `UXNavigationSkip` 只有在 `INPUTSYSTEM_SUPPORT && UX_NAVIGATION` 下编译。

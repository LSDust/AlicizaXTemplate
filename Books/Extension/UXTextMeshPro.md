# UXTextMeshPro

`UXTextMeshPro` 继承 `TextMeshProUGUI`，可以通过本地化 key 自动刷新文本。

源码位置：

- `Client/Packages/com.alicizax.unity.ui.extension/Runtime/UXComponent`

编译条件：需要工程启用 `TEXTMESHPRO_SUPPORT` 宏，否则该类不会参与编译。

## 注入本地化适配器

项目需要先注入本地化适配器，`UXTextMeshPro` 才会根据 key 刷新文本：

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

## 基础用法

```csharp
baseui.TxtTitle.SetLocalization("ui.login.title");
```

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

适合固定 UI 文案，例如标题、按钮文字、提示标签；动态拼接的数值文本可以继续直接设置 `text`。

## 刷新时机

- 运行时 `Start()` 会调用本地化适配器刷新一次文本。
- 调用 `SetLocalization(key)` 时立即刷新。
- 编辑器下 `OnValidate()` 会通过预览接口尝试显示 key 对应的预览文本，方便在 Prefab 里检查文案长度。

## API 速查

| API | 说明 |
| --- | --- |
| `UXTextMeshPro.SetLocalization(string)` | 设置本地化 key 并立即刷新文本 |
| `UXComponentExtensionsHelper.SetLocalizationHelper(...)` | 注入本地化查询适配器 |

## 注意事项

1. 只有注入 `IUXLocalizationHelper` 后才会根据 key 刷新文本，未注入时 `SetLocalization` 不会改变显示内容。
2. 切换语言时，如果窗口仍然显示，需要重新调用 `SetLocalization` 或让项目的语言切换事件统一刷新当前 UI。`UXTextMeshPro` 不会自动订阅项目语言变化事件。
3. 需要启用 `TEXTMESHPRO_SUPPORT` 宏，否则该类不参与编译。

# UXComponent 扩展组件

`UXComponent` 提供一组基于 UGUI 的增强控件。它们主要解决按钮音效、子节点状态切换、Toggle 分组、渐变图片、状态绑定、快捷键触发和本地化文本等问题。

源码位置：

- `Client/Packages/com.alicizax.unity.ui.extension/Runtime/UXComponent`
- 编辑器入口：`Client/Packages/com.alicizax.unity.ui.extension/Editor/UX`

## 模块划分

| 文档 | 内容 |
| --- | --- |
| [UXButton](UXButton.md) | `UXButton`、`UXSelectable` 子节点状态、音效适配器 |
| [UXToggle](UXToggle.md) | `UXToggle`、`UXGroup` 分组、页签切换 |
| [UXImage](UXImage.md) | 渐变绘制、镜像模式、进度条 |
| [UXTextMeshPro](UXTextMeshPro.md) | 本地化 key 绑定、本地化适配器注入 |
| [UXController](UXController.md) | `UXController` 多状态管理、`UXBinding` 属性绑定 |
| [UXDraggable](UXDraggable.md) | 拖拽事件转发、可拖拽弹窗 |
| [HotkeyComponent](HotkeyComponent.md) | Input System 快捷键绑定、优先级规则 |

## 适配器注入

部分控件依赖项目注入适配器才能工作：

| 适配器 | 注入方法 | 影响控件 |
| --- | --- | --- |
| `IUXAudioHelper` | `UXComponentExtensionsHelper.SetAudioHelper(...)` | `UXButton`、`UXToggle` 音效 |
| `IUXLocalizationHelper` | `UXComponentExtensionsHelper.SetLocalizationHelper(...)` | `UXTextMeshPro` 本地化 |

建议在项目启动流程中统一注入，例如在 `RootModule` 初始化完成后：

```csharp
UXComponentExtensionsHelper.SetAudioHelper(new UXAudioAdapter());
UXComponentExtensionsHelper.SetLocalizationHelper(new UXLocalizationAdapter());
```

## 编译条件

| 宏 | 影响范围 |
| --- | --- |
| `TEXTMESHPRO_SUPPORT` | `UXTextMeshPro` |
| `INPUTSYSTEM_SUPPORT` | `HotkeyComponent`、`UXNavigationScope`、`UXNavigationRuntime` |
| `INPUTSYSTEM_SUPPORT && UX_NAVIGATION` | Navigation 模块全部类型 |

## 注意事项

1. `UXButton`、`UXToggle` 等类型在 `UnityEngine.UI` 命名空间下，和 Unity UGUI 控件同一套使用方式。
2. `UXButton` 不继承 Unity `Button`，但保留了 `Button.ButtonClickedEvent` 类型的 `onClick`，业务调用方式基本一致。
3. `HotkeyComponent` 只转发到 `ISubmitHandler`，目标组件必须实现提交接口，例如 `UXButton`、`UXToggle`。
4. `UXImage.FlipMode.Horziontal` 的拼写来自源码枚举，代码里需要使用这个实际名称。

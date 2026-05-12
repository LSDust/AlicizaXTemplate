# UXImage

`UXImage` 继承 `Image`，支持纯色、渐变和镜像绘制。适合做进度条、背景块、左右或四角对称图。

源码位置：

- `Client/Packages/com.alicizax.unity.ui.extension/Runtime/UXComponent`

## 基础用法

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

`UXImage` 保留 `Image` 的 `sprite`、`type`、`fillAmount`、`preserveAspect` 等能力，只是在顶点生成阶段额外处理渐变和镜像。

## 颜色模式

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

## 进度条示例

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

## 镜像模式

| 配置 | 说明 |
| --- | --- |
| `flipMode = None` | 不镜像 |
| `flipMode = Horziontal` | 水平方向镜像，枚举名里 `Horziontal` 是源码中的实际拼写 |
| `flipMode = Vertical` | 垂直方向镜像 |
| `flipMode = FourCorner` | 四角镜像，适合四角对称装饰 |
| `flipWithCopy = true` | 复制一份顶点后镜像，适合用半张图生成完整对称图 |
| `flipWithCopy = false` | 不复制，只把当前图翻转 |

镜像在 `OnPopulateMesh` 里复制或重映射顶点，控件的布局尺寸仍然由 RectTransform 决定。如果想让"半张图复制成左右完整图"占用更多布局空间，需要自己设置 RectTransform 宽高。

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

## API 速查

| API | 说明 |
| --- | --- |
| `UXImage.m_ColorType` | 颜色模式：`Solid_Color` 或 `Gradient_Color` |
| `UXImage.gradient` | 设置渐变 |
| `UXImage.Direction` | 设置渐变方向 |
| `UXImage.flipMode` | 设置镜像模式 |
| `UXImage.flipWithCopy` | 镜像时是否复制原图顶点 |
| `UXImage.flipEdgeHorizontal` | 水平镜像的对称轴位置 |

## 注意事项

1. `UXImage.FlipMode.Horziontal` 的拼写来自源码枚举（少了一个 `i`），代码里需要使用这个实际名称。
2. 镜像不影响布局尺寸，RectTransform 宽高需要手动设置。
3. 渐变颜色在 `OnPopulateMesh` 时写入顶点色，频繁修改 `gradient` 会触发网格重建，避免每帧更新。

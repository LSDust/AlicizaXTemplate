# UXDraggable

`UXDraggable` 把 UGUI 的拖拽接口转成 UnityEvent，适合不想单独写 `IDragHandler` 的简单拖拽场景。

源码位置：

- `Client/Packages/com.alicizax.unity.ui.extension/Runtime/UXComponent`

## 基础用法

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

## 可拖拽弹窗头部示例

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

## API 速查

| API | 说明 |
| --- | --- |
| `UXDraggable.onBeginDrag` | 开始拖拽事件，参数为 `PointerEventData` |
| `UXDraggable.onDrag` | 拖拽中事件，参数为 `PointerEventData` |
| `UXDraggable.onEndDrag` | 结束拖拽事件，参数为 `PointerEventData` |

## 注意事项

1. `UXDraggable` 只负责转发事件，不会自动移动对象，也不会自动限制范围。吸附、边界、保存位置等逻辑都应在监听函数里完成。
2. Canvas 有缩放时，`eventData.delta` 需要除以 `canvas.scaleFactor` 才能正确映射到 `anchoredPosition`。

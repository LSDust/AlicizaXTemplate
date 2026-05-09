using AlicizaX;
using AlicizaX.UI;
using GameLogic.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ImageScrollItemRender : ItemRender<TestData, ImageViewHolder>
{
    public override ItemInteractionFlags InteractionFlags => ItemInteractionFlags.PointerNavigation;


    protected override void OnBind(TestData data, int index)
    {
        // baseui.Text.text = data.Name;
    }

    protected override void OnPointerClick(PointerEventData eventData)
    {
        Log.Info(CurrentData.Name + " item clicked");
    }

    protected override void OnPointerEnter(PointerEventData eventData)
    {
        baseui.backgroundImage.color = Color.green;
        // Log.Info(CurrentData.Name + " Pointer Enter");
    }

    protected override void OnPointerExit(PointerEventData eventData)
    {
        baseui.backgroundImage.color = Color.white;
    }

    protected override void OnItemSelected(BaseEventData eventData)
    {
        baseui.backgroundImage.color = Color.green;
    }

    protected override void OnItemDeselected(BaseEventData eventData)
    {
        baseui.backgroundImage.color = Color.white;
    }

    protected override void OnSubmit(BaseEventData eventData)
    {
        Log.Info(CurrentData.Name + " submitted");
    }

    protected override void OnSelectionChanged(bool selected)
    {
        baseui.iconImage.color=selected ? Color.blue : Color.white;
    }

    protected override bool OnMove(AxisEventData eventData)
    {
        if (eventData.moveDir == MoveDirection.Down || eventData.moveDir == MoveDirection.Up) return false;
        Debug.Log($"{CurrentData.Name} Moved {eventData.moveDir}");
        return true;
    }

    protected override void OnClear()
    {
        base.OnClear();
        baseui.backgroundImage.color = Color.white;
    }

    private void OnInnerButtonClick()
    {
        Log.Info(CurrentData.Name + " button clicked");
    }
}

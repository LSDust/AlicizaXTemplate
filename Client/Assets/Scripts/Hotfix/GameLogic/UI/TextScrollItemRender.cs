using AlicizaX;
using AlicizaX.UI;
using GameLogic.UI;

public sealed class TextScrollItemRender : ItemRender<TestData, TextViewHolder>
{
    protected override void OnBind(TestData data, int index)
    {
        baseui.text.text = data.Name;
    }
}

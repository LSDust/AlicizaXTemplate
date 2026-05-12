using AlicizaX.UI.Runtime;
using UnityEngine;

namespace Game.UI
{
    [UIRes(ui_UITipsWindow.ResTag, EUIResLoadType.AssetBundle)]
    public class ui_UITipsWindow : UIHolderObjectBase
    {
        public const string ResTag = "UITipsWindow";

        [SerializeField] private RectTransform mRectTextTipsRoot;
        public RectTransform RectTextTipsRoot => mRectTextTipsRoot;

        [SerializeField] private RectTransform mRectIconTipsRoot;
        public RectTransform RectIconTipsRoot => mRectIconTipsRoot;
    }
}

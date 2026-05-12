using AlicizaX.UI.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [UIRes(ui_UITextTipsWidget.ResTag, EUIResLoadType.AssetBundle)]
    public class ui_UITextTipsWidget : UIHolderObjectBase
    {
        public const string ResTag = "UITextTipsWidget";

        [SerializeField] private Image mImgBackGround;
        public Image ImgBackGround => mImgBackGround;

        [SerializeField] private TextMeshProUGUI mTextContent;
        public TextMeshProUGUI TextContent => mTextContent;
    }
}

using AlicizaX.UI.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [UIRes(ui_UIIconTipsWidget.ResTag, EUIResLoadType.AssetBundle)]
    public class ui_UIIconTipsWidget : UIHolderObjectBase
    {
        public const string ResTag = "UIIconTipsWidget";

        [SerializeField] private Image mImgBackGround;
        public Image ImgBackGround => mImgBackGround;

        [SerializeField] private Image mImgIcon;
        public Image ImgIcon => mImgIcon;

        [SerializeField] private TextMeshProUGUI mTextContent;
        public TextMeshProUGUI TextContent => mTextContent;
    }
}

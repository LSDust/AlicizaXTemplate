using AlicizaX;
using AlicizaX.UI.Runtime;
using Game.UI;

namespace GameLogic.UI
{
    [Window(UILayer.UI, false, 3)]
    public class UIHomeWindow : UITabWindow<ui_UIHomeWindow>
    {
        protected override void OnInitialize()
        {
            baseui.BtnShop.onClick.AddListener(OnBtnShopClick);
            baseui.BtnBag.onClick.AddListener(OnBtnBagClick);
            baseui.BtnRole.onClick.AddListener(OnBtnRoleClick);
            baseui.BtnTestTips.onClick.AddListener(OnBtnTestTipsClick);
            baseui.BtnExit.onClick.AddListener(OnBtnExitClick);
        }

        private void OnBtnShopClick()
        {
            GameApp.UI.ShowUISync<UIShopWindow>();
        }

        private void OnBtnBagClick()
        {
            Log.Info("点击背包按钮");
        }

        private void OnBtnRoleClick()
        {
            Log.Info("点击角色属性按钮");
        }

        private void OnBtnTestTipsClick()
        {
            Log.Info("点击测试提示按钮");
        }

        private void OnBtnExitClick()
        {
            Log.Info("点击退出按钮");
            CloseSelf();
        }
    }
}

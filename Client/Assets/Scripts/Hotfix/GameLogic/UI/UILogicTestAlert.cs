using AlicizaX;
using AlicizaX.UI.Runtime;
using Game.UI;

namespace GameLogic.UI
{
    [Window(UILayer.Tips, false, 3)]
    public class UILogicTestAlert : UITabWindow<ui_UILogicTestAlert>
    {
        protected override void OnInitialize()
        {
            baseui.BtnEscTest.onClick.AddListener(OnBtnEscTestClick);

            baseui.BtnGTest.onClick.AddListener(OnBtnGTestClick);
        }

        private void OnBtnGTestClick()
        {
            Log.Info("Alert G Click");
        }

        private void OnBtnEscTestClick()
        {
            Log.Info("Alert ESC Click");
            CloseSelf();
        }
    }
}

using AlicizaX;
using AlicizaX.UI.Runtime;
using Game.UI;
using GameLogic.Player;
using TMPro;
using UnityEngine;

namespace GameLogic.UI
{
    [Window(UILayer.Popup, false, 3)]
    public class UIBuyAlertWindow : UITabWindow<ui_UIBuyAlertWindow>
    {
        private ShopGoodsData _goodsData;
        private IFakePlayerDataService _playerDataService;
        private int _unitPrice;
        private int _quantity = 1;

        protected override void OnInitialize()
        {
            baseui.BtnClose.onClick.AddListener(OnBtnCloseClick);
            baseui.BtnConfirm.onClick.AddListener(OnBtnConfirmClick);
            baseui.BtnMinus.onClick.AddListener(OnBtnMinusClick);
            baseui.BtnPlus.onClick.AddListener(OnBtnPlusClick);
            baseui.InputQuantity.onValueChanged.AddListener(OnQuantityInputChanged);
            _playerDataService = AppServices.Require<IFakePlayerDataService>();
        }

        protected override void OnOpen()
        {
            _goodsData = UserData as ShopGoodsData;
            _unitPrice = _goodsData.Price;
            _quantity = 1;
            RefreshView();
        }

        private void OnBtnCloseClick()
        {
            CloseSelf();
        }

        private void OnBtnConfirmClick()
        {
            if (_goodsData == null)
            {
                CloseSelf();
                return;
            }

            if (_playerDataService.TryBuy(_goodsData, _quantity, out int totalPrice))
            {
                Log.Info($"确认购买：{_goodsData.Name} x{_quantity}，总价 {totalPrice} 信用点");
                CloseSelf();
                return;
            }

            Log.Info($"信用点不足，无法购买：{_goodsData.Name} x{_quantity}");
        }

        private void OnBtnMinusClick()
        {
            SetQuantity(_quantity - 1);
        }

        private void OnBtnPlusClick()
        {
            SetQuantity(_quantity + 1);
        }

        private void OnQuantityInputChanged(string value)
        {
            if (!int.TryParse(value, out int quantity))
            {
                return;
            }

            SetQuantity(quantity, false);
        }

        private void SetQuantity(int quantity, bool syncInput = true)
        {
            int clamped = Mathf.Clamp(quantity, 1, 999);
            if (_quantity == clamped && (!syncInput || baseui.InputQuantity.text == clamped.ToString()))
            {
                return;
            }

            _quantity = clamped;
            RefreshTotalPrice();

            if (syncInput)
            {
                baseui.InputQuantity.SetTextWithoutNotify(_quantity.ToString());
            }
        }

        private void RefreshView()
        {
            baseui.TextTitle.text = "购买确认";
            baseui.TextItemName.text = _goodsData != null ? _goodsData.Name : "未知道具";
            baseui.ImgItemIcon.color = _goodsData != null ? _goodsData.AccentColor : Color.white;
            baseui.InputQuantity.contentType = TMP_InputField.ContentType.IntegerNumber;
            baseui.InputQuantity.SetTextWithoutNotify(_quantity.ToString());
            RefreshTotalPrice();
        }

        private void RefreshTotalPrice()
        {
            baseui.TextTotalPrice.text = $"总价格：{_unitPrice * _quantity} 信用点";
        }
    }
}

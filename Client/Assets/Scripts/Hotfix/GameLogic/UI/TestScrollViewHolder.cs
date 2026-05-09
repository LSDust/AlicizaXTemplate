using AlicizaX.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestScrollViewHolder : ViewHolder
{
    [SerializeField] private TextMeshProUGUI text;

    public TextMeshProUGUI Text => text;
    [SerializeField] private UXButton _btnTest;
    public UXButton BtnTest => _btnTest;
}

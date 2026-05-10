using AlicizaX.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.UI
{
    public class ImageViewHolder:ViewHolder
    {
        public Image backgroundImage;
        public Image iconImage;

        [SerializeField] private TextMeshProUGUI text;
        public TextMeshProUGUI Text => text;
    }
}

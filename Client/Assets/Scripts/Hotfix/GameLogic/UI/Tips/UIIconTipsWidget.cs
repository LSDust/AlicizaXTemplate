using System.Threading;
using AlicizaX.UI.Runtime;
using Cysharp.Threading.Tasks;
using Game.UI;
using UnityEngine;

namespace GameLogic.UI
{
    public sealed class UIIconTipsWidget : UIWidget<ui_UIIconTipsWidget>
    {
        private int _playVersion;

        public async UniTaskVoid Show(string content, Sprite icon, float duration)
        {
            int playVersion = ++_playVersion;
            baseui.TextContent.text = content;
            baseui.ImgIcon.sprite = icon;
            baseui.ImgIcon.enabled = icon != null;
            Open();

            int milliseconds = Mathf.RoundToInt(Mathf.Max(0.1f, duration) * 1000f);
            await UniTask.Delay(milliseconds, DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, CancellationToken.None);
            if (playVersion == _playVersion)
            {
                Close();
            }
        }
    }
}

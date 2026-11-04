using System;
using System.Linq;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    // Level won: shows the coin reward over the won board. Continue (wired in the prefab) closes it and hands the coin
    // picture's screen point to the win flow (ApplicationController), which flies the rewards from there.
    public class LevelCompletePopup : BasePopup<LevelCompletePopupParameters>
    {
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private Image coinImage;
        [SerializeField] private Button continueButton;
        [SerializeField] private Canvas canvas;

        private CurrencyData[] Rewards => Parameters.Rewards ?? Array.Empty<CurrencyData>();

        public override void ShowPopup()
        {
            var coins = Rewards.Where(r => r.currencyType == CurrencyType.Coin).Sum(r => r.currencyValue);
            coinsText.SetText("{0}", coins);
        }

        public void Continue()
        {
            continueButton.interactable = false;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var start = RectTransformUtility.WorldToScreenPoint(camera, coinImage.transform.position);
            ClosePopup();
            Parameters.Continued?.Invoke(start);
        }
    }
}

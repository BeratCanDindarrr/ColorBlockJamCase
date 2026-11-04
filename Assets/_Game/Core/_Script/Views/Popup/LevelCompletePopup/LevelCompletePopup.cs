using System;
using System.Linq;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Enums;
using ColorBlockJam.GameTask;
using ColorBlockJam.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    // Level won: shows the coin reward over the won board; Continue (wired in the prefab) closes it, the queued win flow
    // goes back to the menu, then the rewards fly from the coin picture onto the main screen
    // (CurrencyUpdateAnimationGameTask waits for the main screen).
    public class LevelCompletePopup : BasePopup<LevelCompletePopupParameters>
    {
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private Image coinImage;
        [SerializeField] private Button continueButton;
        [SerializeField] private Canvas canvas;

        private ITaskService _taskService;

        [Inject]
        public void Construct(ITaskService taskService)
        {
            _taskService = taskService;
        }

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
            var rewards = Rewards;
            var flyTask = new CurrencyUpdateAnimationGameTask();
            flyTask.Init(rewards, rewards.Select(_ => start).ToArray());
            ClosePopup();
            _taskService.AddTask(flyTask);
        }
    }
}

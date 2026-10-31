using System;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Signals;
using ColorBlockJam.Utils;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    // Out of lives: shows the life count and the time to the next refill, and sells a full refill for coins.
    // Closes by itself once the lives are full again.
    public class MoreLivesPopup : BasePopup<MoreLivesPopupParameters>, ITimerOwner
    {
        [SerializeField] private TextMeshProUGUI lifeLabel;
        [SerializeField] private TextMeshProUGUI timerLabel;
        [SerializeField] private TextMeshProUGUI refillCostLabel;
        [SerializeField] private Button refillButton;
        [SerializeField] private Button closeButton;

        private LifeController _lifeController;
        private CurrencyController _currencyController;
        private LifeSettings _lifeSettings;
        private ITimerSubscriber _timerSubscriber;
        private ISubscriber<LifeModelSync> _lifeModelSyncSubscriber;
        private IDisposable _lifeModelSyncSubscription;

        [Inject]
        public void Construct(LifeController lifeController
            , CurrencyController currencyController
            , LifeSettings lifeSettings
            , ITimerSubscriber timerSubscriber
            , ISubscriber<LifeModelSync> lifeModelSyncSubscriber)
        {
            _lifeController = lifeController;
            _currencyController = currencyController;
            _lifeSettings = lifeSettings;
            _timerSubscriber = timerSubscriber;
            _lifeModelSyncSubscriber = lifeModelSyncSubscriber;
        }

        public override void ShowPopup()
        {
            refillCostLabel.SetText(_lifeSettings.RefillCoinCost.ToString());
            refillButton.onClick.AddListener(RefillButtonClicked);
            closeButton.onClick.AddListener(ClosePopup);
            _timerSubscriber.Subscribe(this);
            _lifeModelSyncSubscription = _lifeModelSyncSubscriber.Subscribe(_ => UpdateTime(0));
            UpdateTime(0);
        }

        protected override void Dispose()
        {
            refillButton.onClick.RemoveListener(RefillButtonClicked);
            closeButton.onClick.RemoveListener(ClosePopup);
            _timerSubscriber.UnSubscribe(this);
            _lifeModelSyncSubscription?.Dispose();
            _lifeModelSyncSubscription = null;
        }

        public void UpdateTime(float intervalTime)
        {
            var (life, secondsLeft) = _lifeController.GetLifeTime();
            lifeLabel.SetText(life);
            timerLabel.SetText(secondsLeft < 0 ? "Full" : $"{secondsLeft / 60:00}:{secondsLeft % 60:00}");

            if (_lifeController.IsLimitless() || !_lifeController.IsTimerActive()) ClosePopup();
        }

        public void ResumeTime(float passedRealtime)
        {
            UpdateTime(passedRealtime);
        }

        private void RefillButtonClicked()
        {
            var price = new CurrencyData(CurrencyType.Coin, _lifeSettings.RefillCoinCost);
            if (_currencyController.TryConsume(price) != CurrencySpendResult.Success)
            {
                // TODO: open ShopPopup and reopen this popup after a purchase (not in this project yet).
                DebugExtension.Log("Not enough coins to refill lives");
                return;
            }

            _lifeController.RefillLives();
            _lifeController.SyncModel();
            ClosePopup();
        }

        // TODO: knit_Out also gives one life for a rewarded ad (AdService is not in this project yet).
    }
}

using System;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Enums;
using ColorBlockJam.Signals;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    public class MainScreenPage2View : MainScreenBasePageView
    {
        // Page 0 of the main screen is the shop.
        private const int ShopPage = 0;

        [SerializeField] private Button playButton;
        [SerializeField] private MainScreenHeaderView header;

        private ApplicationController _applicationController;
        private ScreenController _screenController;
        private CurrencyController _currencyController;
        private LifeController _lifeController;
        private ISubscriber<CurrencyUpdateSignal> _currencyUpdateSubscriber;
        private ISubscriber<LifeModelSync> _lifeModelSyncSubscriber;
        private IDisposable _subscriptions;

        [Inject]
        public void Construct(ApplicationController applicationController
            , ScreenController screenController
            , CurrencyController currencyController
            , LifeController lifeController
            , ISubscriber<CurrencyUpdateSignal> currencyUpdateSubscriber
            , ISubscriber<LifeModelSync> lifeModelSyncSubscriber)
        {
            _applicationController = applicationController;
            _screenController = screenController;
            _currencyController = currencyController;
            _lifeController = lifeController;
            _currencyUpdateSubscriber = currencyUpdateSubscriber;
            _lifeModelSyncSubscriber = lifeModelSyncSubscriber;
        }

        public override void Initialize()
        {
            playButton.onClick.AddListener(OnPlayClicked);
            header.Initialize();
            header.CoinClicked += OnCoinClicked;
            header.LifeClicked += OnLifeClicked;

            _subscriptions = DisposableBag.Create(
                _currencyUpdateSubscriber.Subscribe(_ => RefreshHeader()),
                _lifeModelSyncSubscriber.Subscribe(_ => RefreshHeader()));
            RefreshHeader();
        }

        public override void Dispose()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
            if (header != null)
            {
                header.CoinClicked -= OnCoinClicked;
                header.LifeClicked -= OnLifeClicked;
                header.Dispose();
            }
            _subscriptions?.Dispose();
            _subscriptions = null;
        }

        private void RefreshHeader()
        {
            header.SetCoin(_currencyController.GetCurrency(CurrencyType.Coin));
            header.SetLife(_lifeController.GetLifeTime().Item1);
        }

        private void OnCoinClicked()
        {
            _screenController.MainScreenView.SnapToPage(ShopPage);
        }

        private void OnLifeClicked()
        {
            _applicationController.OpenMoreLivesPopup();
        }

        private void OnPlayClicked()
        {
            _applicationController.PlayLevel();
        }
    }
}

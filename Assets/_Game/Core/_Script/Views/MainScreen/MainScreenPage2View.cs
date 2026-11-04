using System;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Enums;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Signals;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    public class MainScreenPage2View : MainScreenBasePageView
    {
        // Page 0 of the main screen is the shop.
        private const int ShopPage = 0;
        private const string PlayLabelFormat = "Level {0}";

        [SerializeField] private Button playButton;
        [SerializeField] private TextMeshProUGUI playLabel;
        [SerializeField] private MainScreenHeaderView header;

        private ApplicationController _applicationController;
        private ScreenController _screenController;
        private IPopupService _popupService;
        private CurrencyController _currencyController;
        private LifeController _lifeController;
        private PlayerController _playerController;
        private ISubscriber<CurrencyUpdateSignal> _currencyUpdateSubscriber;
        private ISubscriber<LifeModelSync> _lifeModelSyncSubscriber;
        private ISubscriber<LevelSuccessSignal> _levelSuccessSubscriber;
        private ISubscriber<LevelUpSignal> _levelUpSubscriber;
        private IDisposable _subscriptions;

        [Inject]
        public void Construct(ApplicationController applicationController
            , ScreenController screenController
            , IPopupService popupService
            , CurrencyController currencyController
            , LifeController lifeController
            , PlayerController playerController
            , ISubscriber<CurrencyUpdateSignal> currencyUpdateSubscriber
            , ISubscriber<LifeModelSync> lifeModelSyncSubscriber
            , ISubscriber<LevelSuccessSignal> levelSuccessSubscriber
            , ISubscriber<LevelUpSignal> levelUpSubscriber)
        {
            _applicationController = applicationController;
            _screenController = screenController;
            _popupService = popupService;
            _currencyController = currencyController;
            _lifeController = lifeController;
            _playerController = playerController;
            _currencyUpdateSubscriber = currencyUpdateSubscriber;
            _lifeModelSyncSubscriber = lifeModelSyncSubscriber;
            _levelSuccessSubscriber = levelSuccessSubscriber;
            _levelUpSubscriber = levelUpSubscriber;
        }

        public override void Initialize()
        {
            playButton.onClick.AddListener(OnPlayClicked);
            header.Initialize();
            header.CoinClicked += OnCoinClicked;
            header.LifeClicked += OnLifeClicked;
            header.SettingsClicked += OnSettingsClicked;

            _subscriptions = DisposableBag.Create(
                _currencyUpdateSubscriber.Subscribe(_ => RefreshHeader()),
                _lifeModelSyncSubscriber.Subscribe(_ => RefreshHeader()),
                _levelSuccessSubscriber.Subscribe(_ => RefreshPlayLabel()),
                _levelUpSubscriber.Subscribe(_ => RefreshPlayLabel()));
            RefreshHeader();
            RefreshPlayLabel();
            _screenController.MainScreenView.SetCurrencyTargets(header, playButton.transform);
        }

        public override void Dispose()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
            if (header != null)
            {
                header.CoinClicked -= OnCoinClicked;
                header.LifeClicked -= OnLifeClicked;
                header.SettingsClicked -= OnSettingsClicked;
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

        // The Play button names the level it starts.
        private void RefreshPlayLabel()
        {
            playLabel.SetText(PlayLabelFormat, _playerController.GetVisualLevel());
        }

        private void OnCoinClicked()
        {
            _screenController.MainScreenView.SnapToPage(ShopPage);
        }

        private void OnLifeClicked()
        {
            _applicationController.OpenMoreLivesPopup();
        }

        private void OnSettingsClicked()
        {
            _popupService.AddPopup(new MenuSettingsPopupParameters());
        }

        private void OnPlayClicked()
        {
            _applicationController.PlayLevel();
        }
    }
}

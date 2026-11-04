using System;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Enums;
using ColorBlockJam.Interfaces;
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
        [SerializeField] private LevelButtonView levelButton;
        [SerializeField] private MainScreenHeaderView header;

        private ApplicationController _applicationController;
        private ScreenController _screenController;
        private IPopupService _popupService;
        private CurrencyController _currencyController;
        private LifeController _lifeController;
        private PlayerController _playerController;
        private ISubscriber<CurrencyUpdateSignal> _currencyUpdateSubscriber;
        private ISubscriber<LifeModelSync> _lifeModelSyncSubscriber;
        private ISubscriber<LevelUpSignal> _levelUpSubscriber;
        private IAudioService _audioService;
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
            , ISubscriber<LevelUpSignal> levelUpSubscriber
            , IAudioService audioService)
        {
            _audioService = audioService;
            _applicationController = applicationController;
            _screenController = screenController;
            _popupService = popupService;
            _currencyController = currencyController;
            _lifeController = lifeController;
            _playerController = playerController;
            _currencyUpdateSubscriber = currencyUpdateSubscriber;
            _lifeModelSyncSubscriber = lifeModelSyncSubscriber;
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
                // A set level shows at once; a won level waits for LevelButtonUpdateGameTask's animation.
                _levelUpSubscriber.Subscribe(_ => levelButton.SetLevel(_playerController.GetVisualLevel())));
            RefreshHeader();
            levelButton.SetLevel(_playerController.GetVisualLevel());
            _screenController.MainScreenView.SetCurrencyTargets(header, playButton.transform);
            _screenController.MainScreenView.SetLevelButton(levelButton);
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
            _audioService.PlaySfx(SfxName.Buttons);
            _applicationController.PlayLevel();
        }
    }
}

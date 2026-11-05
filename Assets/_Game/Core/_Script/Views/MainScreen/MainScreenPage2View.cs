using System;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Signals;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    public class MainScreenPage2View : MainScreenBasePageView, ITimerOwner
    {
        // Page 0 of the main screen is the shop.

        [SerializeField] private Button playButton;
        [SerializeField] private LevelButtonView levelButton;
        [SerializeField] private LevelPathView levelPath;
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
        private ITimerSubscriber _timerSubscriber;
        private LevelSettings _levelSettings;
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
            , IAudioService audioService
            , ITimerSubscriber timerSubscriber
            , LevelSettings levelSettings)
        {
            _levelSettings = levelSettings;
            _timerSubscriber = timerSubscriber;
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
                _levelUpSubscriber.Subscribe(_ => ShowLevel(_playerController.GetVisualLevel())));
            // The limitless-life countdown ticks every second.
            _timerSubscriber.Subscribe(this);
            RefreshHeader();
            ShowLevel(_playerController.GetVisualLevel());
            _screenController.MainScreenView.SetCurrencyTargets(header, playButton.transform);
            _screenController.MainScreenView.SetLevelButton(levelButton);
            _screenController.MainScreenView.SetLevelPath(levelPath);
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
            _timerSubscriber?.UnSubscribe(this);
        }

        public void UpdateTime(float intervalTime) => RefreshLife();

        public void ResumeTime(float passedRealtime) => RefreshLife();

        private void RefreshHeader()
        {
            header.SetCoin(_currencyController.ShownCoin);
            RefreshLife();
        }

        private void ShowLevel(int level)
        {
            levelButton.SetLevel(level);
            levelPath.SetLevel(level, _levelSettings);
        }

        private void RefreshLife()
        {
            var (life, secondsLeft) = _lifeController.GetLifeTime();
            header.SetLife(life, _lifeController.IsLimitless(), secondsLeft);
        }

        private void OnCoinClicked()
        {
            _screenController.MainScreenView.SnapToPage(MainScreenView.ShopPage);
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

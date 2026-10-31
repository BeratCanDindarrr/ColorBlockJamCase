using System;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Core.Service;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.GameTask;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Signals;
using ColorBlockJam.Views;
using ColorBlockJam.Views.TestPopup;
using ColorBlockJam.Utils;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace ColorBlockJam.Core.Controller
{
    public class ApplicationController : IInitializable,ITimerOwner,IDisposable
    {
        private readonly ApplicationSettings _applicationSettings;
        private readonly TaskService _taskService;
        private readonly PopupService _popupService;
        private readonly ScreenController _screenController;
        private readonly ITimerSubscriber _timerSubscriber;
        private readonly StorageService _storageService;
        private readonly PlayerController _playerController;
        private readonly LifeController _lifeController;
        private readonly CurrencyController _currencyController;
        private readonly SettingsController _settingsController;
        private readonly SceneLoaderController _sceneLoaderController;
        private readonly LevelSettings _levelSettings;
        private readonly ISubscriber<LevelEndedSignal> _levelEndedSubscriber;

        private IDisposable _levelEndedSubscription;
        
        public ApplicationController(ApplicationSettings applicationSettings
        , TaskService taskService
        , PopupService popupService
        , ScreenController screenController
        , ITimerSubscriber timerSubscriber
        , StorageService storageService
        , PlayerController playerController
        , LifeController lifeController
        , CurrencyController currencyController
        , SettingsController settingsController
        , SceneLoaderController sceneLoaderController
        , LevelSettings levelSettings
        , ISubscriber<LevelEndedSignal> levelEndedSubscriber)
        {
            _applicationSettings = applicationSettings;
            _taskService = taskService;
            _popupService = popupService;
            _screenController = screenController;
            _timerSubscriber = timerSubscriber;
            _storageService = storageService;
            _playerController = playerController;
            _lifeController = lifeController;
            _currencyController = currencyController;
            _settingsController = settingsController;
            _sceneLoaderController = sceneLoaderController;
            _levelSettings = levelSettings;
            _levelEndedSubscriber = levelEndedSubscriber;
        }
        public void Initialize()
        {
            DebugExtension.Log("ApplicationController initialized " + _applicationSettings.ApplicationName);
            // First: everything after it may read the save.
            _storageService.Init();
            _playerController.Init();
            _lifeController.Init();
            _currencyController.Init();
            _settingsController.Init();
            _taskService.Init();
            _popupService.Init();
            _screenController.Init();
            _timerSubscriber.Subscribe(this);
            _levelEndedSubscription = _levelEndedSubscriber.Subscribe(OnLevelEnded);
            Application.focusChanged += OnFocusChanged;
            Application.quitting += _storageService.SaveStorage;
            
            
            //loading and play 
            _screenController.SetScreen(ScreenType.LoadingScreen);
            
            var waitForSeconds = new WaitForSecondsGameTask();
            waitForSeconds.Init(2f);
            _taskService.AddTask(waitForSeconds);
            CallBackGameTask callBackGameTask = new CallBackGameTask();
            callBackGameTask.Init(() =>
            {
                DebugExtension.Log("CallBackGameTask " + _applicationSettings.ApplicationName);
            });
            _taskService.AddTask(callBackGameTask);
            _popupService.AddPopup(new TestPopupParameter());
        }

        // Starting a level costs a life; it comes back if the level is won.
        public void PlayLevel()
        {
            if (_sceneLoaderController.IsBusy || _sceneLoaderController.IsGamePlaySceneLoaded()) return;

            if (!_lifeController.HasLife())
            {
                OpenMoreLivesPopup();
                return;
            }

            _lifeController.RemoveLife();
            _sceneLoaderController.OpenGamePlayScene().Forget();
        }

        // Only while lives are refilling; the popup closes itself once they are full.
        public void OpenMoreLivesPopup()
        {
            if (_lifeController.IsLimitless() || !_lifeController.IsTimerActive()) return;

            _popupService.AddPopup(new MoreLivesPopupParameters());
        }

        private void OnLevelEnded(LevelEndedSignal signal)
        {
            if (signal.Won) PlayWinFlow();
            else OpenOutOfMovesPopup();
        }

        // The won board stays on screen for WinLevelDelaySeconds with input locked, then the flow returns to the menu.
        private void PlayWinFlow()
        {
            _taskService.AddTask(new LockInputGameTask(), true);
            var waitTask = new WaitForSecondsGameTask();
            waitTask.Init(_levelSettings.WinLevelDelaySeconds);
            _taskService.AddTask(waitTask);
            WinLevel();
        }

        public void WinLevel()
        {
            if (!_sceneLoaderController.IsGamePlaySceneLoaded()) return;

            _currencyController.AddCurrency(new CurrencyData(CurrencyType.Coin, _levelSettings.LevelReward), false);
            _playerController.LevelSuccess();
            _lifeController.GiveLife();
            _lifeController.SyncModel();
            _storageService.SaveStorage();

            // TODO: LevelWinPopup, LevelCompleteRewardPopup and the coin fly animation (not in this project yet).

            var backToMenuTask = new CallBackGameTask();
            backToMenuTask.Init(() => _sceneLoaderController.CloseGamePlayScene().Forget());
            _taskService.AddTask(backToMenuTask);
            _taskService.AddTask(new UnlockInputGameTask());
        }

        // Gave up after running out of time; the life spent on starting the level is not returned.
        public void LoseLevel()
        {
            if (!_sceneLoaderController.IsGamePlaySceneLoaded()) return;

            _lifeController.SyncModel();
            _storageService.SaveStorage();
            _sceneLoaderController.CloseGamePlayScene().Forget();
            // TODO: post-level flow (FlowController.LevelUpFlowStart in knit_Out; not in this project yet).
        }

        public void OpenOutOfMovesPopup()
        {
            _popupService.AddPopup(new OutOfMovesPopupParameters
            {
                AddTimeCoinValue = _levelSettings.PlayOnCoinCost,
                AddTimeSeconds = _levelSettings.PlayOnSeconds
            });
        }

        private void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus) _storageService.SaveStorage();
        }

        public void Dispose()
        {
            _levelEndedSubscription?.Dispose();
            Application.focusChanged -= OnFocusChanged;
            Application.quitting -= _storageService.SaveStorage;
        }

        public void UpdateTime(float intervalTime)
        {
            DebugExtension.Log("update time " + intervalTime);
        }

        public void ResumeTime(float passedRealTime)
        {
            DebugExtension.Log("resume time " + passedRealTime);
        }
    }
}
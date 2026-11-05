using System;
using System.Linq;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Core.Service;
using ColorBlockJam.Enums;
using ColorBlockJam.GameTask;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace ColorBlockJam.Core.Controller.Mock
{
    // MockMessageController: a scene object whose Inspector (MockMessageControllerEditor) has buttons that
    // drive the running game's controllers, popups and tasks, for checking flows by hand in Play Mode. The fields are
    // the buttons' inputs. Optional: ControllerLifeTimeScope injects it when the scene has one.
    public class MockMessageController : BaseView
    {
        [Header("Currency")]
        public CurrencyType currencyType = CurrencyType.Coin;
        public long currencyAmount = 100;

        [Header("Rewards")]
        public long rewardCoins = 20;
        public long rewardTimeSeconds = 1800;
        public long rewardBooster1 = 1;

        [Header("Life")]
        public long limitlessSeconds = 1800;

        [Header("Level")]
        public int level = 1;

        private CurrencyController _currencyController;
        private LifeController _lifeController;
        private PlayerController _playerController;
        private SceneLoaderController _sceneLoaderController;
        private ApplicationController _applicationController;
        private IPopupService _popupService;
        private ITaskService _taskService;
        private StorageService _storageService;

        [Inject]
        public void Construct(CurrencyController currencyController
            , LifeController lifeController
            , PlayerController playerController
            , SceneLoaderController sceneLoaderController
            , ApplicationController applicationController
            , IPopupService popupService
            , ITaskService taskService
            , StorageService storageService)
        {
            _currencyController = currencyController;
            _lifeController = lifeController;
            _playerController = playerController;
            _sceneLoaderController = sceneLoaderController;
            _applicationController = applicationController;
            _popupService = popupService;
            _taskService = taskService;
            _storageService = storageService;
        }

        public bool IsReady => _currencyController != null;
        public bool InLevel => _sceneLoaderController.IsGamePlaySceneLoaded();

        #region Currency

        public string CurrencySummary => string.Join("   ", ((CurrencyType[])Enum.GetValues(typeof(CurrencyType)))
            .Where(t => t != CurrencyType.Time)
            .Select(t => $"{t}: {_currencyController.GetCurrency(t)}"));

        public void AddCurrency() => _currencyController.AddCurrency(new CurrencyData(currencyType, currencyAmount), true);

        public void ConsumeCurrency() => Debug.Log($"Mock: consume {currencyType} " +
                                                   _currencyController.TryConsume(new CurrencyData(currencyType, currencyAmount)));

        public void ResetCurrency() => _currencyController.ResetCurrency(currencyType);

        #endregion

        #region Rewards / Win

        private CurrencyData[] Rewards() => new[]
        {
            new CurrencyData(CurrencyType.Coin, rewardCoins),
            new CurrencyData(CurrencyType.Time, rewardTimeSeconds),
            new CurrencyData(CurrencyType.Booster1, rewardBooster1)
        }.Where(r => r.currencyValue > 0).ToArray();

        // The rewards are added, then fly onto the main screen.
        public void AddRewardsAndFly()
        {
            var rewards = Rewards();
            foreach (var reward in rewards) _currencyController.AddCurrency(reward, false);
            _lifeController.SyncModel();

            var task = new CurrencyUpdateAnimationGameTask();
            task.Init(rewards);
            _taskService.AddTask(task);
        }

        public void OpenCollectPopup() => _popupService.AddPopup(new CollectPopupParameters { Rewards = Rewards() });

        public void WinLevel() => _applicationController.WinLevel();

        public void LoseLevel() => _applicationController.LoseLevel();

        #endregion

        #region Life

        public string LifeSummary
        {
            get
            {
                var (time, _) = _lifeController.GetLifeTime();
                return $"Lives: {_lifeController.GetCurrentLife()}   Limitless: {_lifeController.IsLimitless()}   Timer: {time}";
            }
        }

        public void AddLimitlessLife() => WithLifeSync(() => _lifeController.AddLimitlessLife(limitlessSeconds));
        public void GiveLife() => WithLifeSync(_lifeController.GiveLife);
        public void RemoveLife() => WithLifeSync(_lifeController.RemoveLife);
        public void RefillLives() => WithLifeSync(_lifeController.RefillLives);
        public void ResetLives() => WithLifeSync(_lifeController.ResetLives);

        private void WithLifeSync(Action change)
        {
            change();
            _lifeController.SyncModel();
        }

        #endregion

        #region Popups

        public void OpenMoreLivesPopup() => _applicationController.OpenMoreLivesPopup();
        public void OpenOutOfTimePopup() => _applicationController.OpenOutOfTimePopup();
        public void OpenLevelFailedPopup() => _popupService.AddPopup(new LevelFailedPopupParameters());
        public void OpenMenuSettingsPopup() => _popupService.AddPopup(new MenuSettingsPopupParameters());
        public void OpenInGameSettingsPopup() => _popupService.AddPopup(new InGameSettingsPopupParameters());

        #endregion

        #region Level

        public string LevelSummary => $"Visual level: {_playerController.GetVisualLevel()}   " +
                                      $"Data level: {_playerController.GetDataLevel() + 1}   In level: {InLevel}";

        public void SetLevel() => _playerController.SetLevel(level, true);
        public void PlayLevel() => _applicationController.PlayLevel();
        public void OpenLevel() => _sceneLoaderController.OpenGamePlayScene().Forget();
        public void CloseLevel() => _sceneLoaderController.CloseGamePlayScene().Forget();

        #endregion

        #region UI Tasks / Storage

        public void LockInput() => _taskService.AddTask(new LockInputGameTask());
        public void UnlockInput() => _taskService.AddTask(new UnlockInputGameTask());
        public void SaveStorage() => _storageService.SaveStorage();

        #endregion
    }
}

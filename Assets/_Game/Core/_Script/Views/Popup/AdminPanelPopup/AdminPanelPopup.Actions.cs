using System;
using System.Linq;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Enums;
using ColorBlockJam.GameTask;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ColorBlockJam.Views
{
    // The admin panel's tabs and what each button does (the Mock Messenger's actions, for the device).
    public partial class AdminPanelPopup
    {
        // Developer amounts, as the Mock Messenger's defaults.
        private const long CoinAmount = 1000;
        private const long BoosterAmount = 1;
        private const long RewardCoins = 20;
        private const long RewardTimeSeconds = 1800;
        private const long LimitlessSeconds = 1800;

        private static readonly CurrencyType[] Boosters =
            { CurrencyType.Booster1, CurrencyType.Booster2, CurrencyType.Booster3, CurrencyType.Booster4 };

        private AdminTab[] Tabs() => new[]
        {
            new AdminTab("Level",
                new AdminAction("Win Level", WinLevel),
                new AdminAction("Lose Level", LoseLevel, true),
                new AdminAction("Play Level", _applicationController.PlayLevel),
                new AdminAction("Retry Level", () => _applicationController.RetryLevel().Forget()),
                new AdminAction("Close Level", () => _sceneLoaderController.CloseGamePlayScene().Forget(), true),
                new AdminAction("Reset Progress", () => GoToLevel(1).Forget(), true)),
            new AdminTab("Complete",
                new AdminAction("Level Complete Popup",
                    () => _popupService.AddPopup(new LevelCompletePopupParameters { Rewards = Rewards() })),
                new AdminAction("Win Flow (in level)", _applicationController.WinLevel),
                new AdminAction("Rewards Fly", AddRewardsAndFly),
                new AdminAction("Collect Popup",
                    () => _popupService.AddPopup(new CollectPopupParameters { Rewards = Rewards() }))),
            new AdminTab("Fail",
                new AdminAction("Out Of Time Popup", _applicationController.OpenOutOfMovesPopup),
                new AdminAction("Quit Level Popup", () => _popupService.AddPopup(new QuitLevelPopupParameters())),
                new AdminAction("Level Failed Popup", () => _popupService.AddPopup(new LevelFailedPopupParameters())),
                new AdminAction("Lose Level", LoseLevel, true)),
            new AdminTab("Currency",
                new AdminAction($"+{CoinAmount} Coins", () => Add(CurrencyType.Coin, CoinAmount)),
                new AdminAction("+1 Each Booster", () => Array.ForEach(Boosters, b => Add(b, BoosterAmount))),
                new AdminAction("Reset Coins", () => _currencyController.ResetCurrency(CurrencyType.Coin), true),
                new AdminAction("Reset Boosters", () => Array.ForEach(Boosters, _currencyController.ResetCurrency), true)),
            new AdminTab("Life",
                new AdminAction("Give Life", () => WithLifeSync(_lifeController.GiveLife)),
                new AdminAction("Remove Life", () => WithLifeSync(_lifeController.RemoveLife), true),
                new AdminAction("Refill Lives", () => WithLifeSync(_lifeController.RefillLives)),
                new AdminAction("Limitless 30 min",
                    () => WithLifeSync(() => _lifeController.AddLimitlessLife(LimitlessSeconds))),
                new AdminAction("Reset Lives", () => WithLifeSync(_lifeController.ResetLives), true)),
            new AdminTab("Popups",
                new AdminAction("More Lives", _applicationController.OpenMoreLivesPopup),
                new AdminAction("Menu Settings", () => _popupService.AddPopup(new MenuSettingsPopupParameters())),
                new AdminAction("In-Game Settings", () => _popupService.AddPopup(new InGameSettingsPopupParameters())),
                new AdminAction("Test Popup", () => _popupService.AddPopup(new TestPopup.TestPopupParameter()))),
            new AdminTab("Other",
                new AdminAction("Reset Tutorials", ResetTutorials, true),
                new AdminAction("Lock Input", () => _taskService.AddTask(new LockInputGameTask())),
                new AdminAction("Unlock Input", () => _taskService.AddTask(new UnlockInputGameTask())),
                new AdminAction("Save Storage", _storageService.SaveStorage))
        };

        // Leaves a running level, then sets the level from the field (or the given one) and saves.
        private async UniTaskVoid GoToLevel(int? level = null)
        {
            var target = level ?? ParseLevel();
            if (target <= 0) return;

            ClosePopup();
            if (_sceneLoaderController.IsGamePlaySceneLoaded()) await _sceneLoaderController.CloseGamePlayScene();
            _playerController.SetLevel(target, true);
            _storageService.SaveStorage();
        }

        private int ParseLevel()
        {
            if (int.TryParse(levelInput.text, out var level) && level > 0) return level;

            Debug.LogWarning($"{nameof(AdminPanelPopup)}: \"{levelInput.text}\" is no level number");
            return 0;
        }

        // In a level: the real win (LevelComplete popup over the board). On the menu: the level counts as won and the
        // Play button moves on.
        private void WinLevel()
        {
            ClosePopup();
            if (_sceneLoaderController.IsGamePlaySceneLoaded())
            {
                _applicationController.WinLevel();
                return;
            }

            _playerController.LevelSuccess();
            _storageService.SaveStorage();
            _taskService.AddTask(new LevelButtonUpdateGameTask());
        }

        // Gives the level up like Quit Level does (the life stays spent), then shows Level Failed.
        private void LoseLevel()
        {
            if (!_sceneLoaderController.IsGamePlaySceneLoaded()) return;

            ClosePopup();
            _applicationController.LoseLevel();
            _popupService.AddPopup(new LevelFailedPopupParameters());
        }

        private CurrencyData[] Rewards() => new[]
        {
            new CurrencyData(CurrencyType.Coin, RewardCoins),
            new CurrencyData(CurrencyType.Time, RewardTimeSeconds),
            new CurrencyData(CurrencyType.Booster1, BoosterAmount)
        }.Where(r => r.currencyValue > 0).ToArray();

        // The Mock Messenger's "Add Rewards & Fly": added, then they fly onto the main screen.
        private void AddRewardsAndFly()
        {
            ClosePopup();
            var rewards = Rewards();
            foreach (var reward in rewards) _currencyController.AddCurrency(reward, false);
            _lifeController.SyncModel();
            var task = new CurrencyUpdateAnimationGameTask();
            task.Init(rewards);
            _taskService.AddTask(task);
        }

        private void Add(CurrencyType type, long amount) => _currencyController.AddCurrency(new CurrencyData(type, amount), true);

        private void WithLifeSync(Action change)
        {
            change();
            _lifeController.SyncModel();
        }

        // Every booster tutorial and the first-level drag tutorial show again.
        private void ResetTutorials()
        {
            _storageService.StorageModel.boosterTutorialModel = null;
            _storageService.SaveStorage();
        }
    }
}

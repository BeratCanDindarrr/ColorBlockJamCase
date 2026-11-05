using System;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Enums;
using ColorBlockJam.GameTask;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ColorBlockJam.Views
{
    // The admin panel's tabs (level, currency, life) and what each button does.
    public partial class AdminPanelPopup
    {
        // Developer amounts, as the Mock Messenger's defaults.
        private const long CoinAmount = 1000;
        private const long BoosterAmount = 1;
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
                new AdminAction("Reset Lives", () => WithLifeSync(_lifeController.ResetLives), true))
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

        private void Add(CurrencyType type, long amount) => _currencyController.AddCurrency(new CurrencyData(type, amount), true);

        private void WithLifeSync(Action change)
        {
            change();
            _lifeController.SyncModel();
        }
    }
}

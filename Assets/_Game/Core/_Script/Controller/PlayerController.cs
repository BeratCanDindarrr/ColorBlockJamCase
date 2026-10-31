using System;
using ColorBlockJam.Const;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Core.Service;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Signals;
using ColorBlockJam.Utils;
using MessagePipe;
using UnityEngine;

namespace ColorBlockJam.Core.Controller
{
    // Owns the saved player profile and level progress (StorageModel.playerModel / levelModel) plus the device's
    // user id and install time. Init after StorageService.Init.
    public class PlayerController
    {
        private const string DefaultPlayerName = "Player";

        public string UserId { get; private set; }
        public long InstallTime { get; private set; }

        private readonly StorageService _storageService;
        private readonly LevelSettings _levelSettings;
        private readonly IPublisher<LevelUpSignal> _levelUpPublisher;
        private readonly IPublisher<LevelSuccessSignal> _levelSuccessPublisher;

        private PlayerModel _model;
        private LevelModel _levelModel;

        public PlayerController(StorageService storageService
            , LevelSettings levelSettings
            , IPublisher<LevelUpSignal> levelUpPublisher
            , IPublisher<LevelSuccessSignal> levelSuccessPublisher)
        {
            _storageService = storageService;
            _levelSettings = levelSettings;
            _levelUpPublisher = levelUpPublisher;
            _levelSuccessPublisher = levelSuccessPublisher;
        }

        public void Init()
        {
            _storageService.StorageModel.playerModel ??= new PlayerModel
            {
                pictureId = 0,
                playerName = DefaultPlayerName
            };

            _storageService.StorageModel.levelModel ??= new LevelModel
            {
                currentLevel = 1,
                visualLevel = 1,
                lastMaxLevel = _levelSettings.LevelCount
            };

            _model = _storageService.StorageModel.playerModel;
            _levelModel = _storageService.StorageModel.levelModel;

            SyncLevelData();
            InitUserIdAndInstallTime();
        }

        // New levels were added since the last sync: a player already looping the old end moves on to the first new one.
        private void SyncLevelData()
        {
            if (_levelSettings.LevelCount <= _levelModel.lastMaxLevel) return;

            if (_levelModel.visualLevel > _levelModel.lastMaxLevel)
            {
                _levelModel.currentLevel = _levelModel.lastMaxLevel + 1;
                _levelModel.loopResetCount = 0;
            }

            _levelModel.lastMaxLevel = _levelSettings.LevelCount;

            DebugExtension.Log($"SyncLevelData VisualLevel : {_levelModel.visualLevel} CurrentLevel : " +
                               $"{_levelModel.currentLevel} LastMaxLevel : {_levelModel.lastMaxLevel}");
        }

        // Kept in files, not in the save, so they survive a reset of the save data.
        private void InitUserIdAndInstallTime()
        {
            UserId = FileUtils.ReadFromFile(GlobalConst.Storage.UserIdFile) ?? "";
            if (string.IsNullOrEmpty(UserId)) UserId = Guid.NewGuid().ToString("N").ToUpperInvariant();

            var firstOpen = FileUtils.ReadFromFile(GlobalConst.Storage.FirstOpenTimeFile);
            InstallTime = long.TryParse(firstOpen, out var installTime)
                ? installTime
                : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            FileUtils.SaveToFile(GlobalConst.Storage.FirstOpenTimeFile, InstallTime.ToString());
            FileUtils.SaveToFile(GlobalConst.Storage.UserIdFile, UserId);

            DebugExtension.Log("UserId : " + UserId);
            DebugExtension.Log("InstallTime : " + InstallTime);
        }

        public void SetPlayerName(string newName)
        {
            _model.playerName = newName;
        }

        public string GetPlayerName()
        {
            return string.IsNullOrEmpty(_model.playerName) ? DefaultPlayerName : _model.playerName;
        }

        public void SetPictureId(int index)
        {
            _model.pictureId = index;
        }

        public int GetPictureId()
        {
            return _model.pictureId;
        }

        public int GetVisualLevel()
        {
            return _levelModel.visualLevel;
        }

        // 0-based index into LevelSettings.Levels.
        public int GetDataLevel()
        {
            return _levelModel.currentLevel - 1;
        }

        public void SetLevel(int newLevel, bool fireSignal)
        {
            _levelModel.lastMaxLevel = _levelSettings.LevelCount;
            _levelModel.visualLevel = Mathf.Clamp(newLevel, 1, _levelModel.lastMaxLevel);
            _levelModel.currentLevel = Mathf.Clamp(newLevel, 1, _levelModel.lastMaxLevel);

            if (fireSignal) _levelUpPublisher.Publish(new LevelUpSignal());
        }

        public void LevelSuccess(bool fireSignal = false)
        {
            _levelModel.visualLevel++;
            _levelModel.currentLevel++;

            if (_levelModel.currentLevel > _levelModel.lastMaxLevel)
            {
                _levelModel.currentLevel = Mathf.Max(1, _levelModel.currentLevel - _levelSettings.EndlessLoopCount);
                _levelModel.loopResetCount++;
            }

            if (fireSignal) _levelUpPublisher.Publish(new LevelUpSignal());
            _levelSuccessPublisher.Publish(new LevelSuccessSignal());

            DebugExtension.Log($"LevelSuccess VisualLevel : {_levelModel.visualLevel} CurrentLevel : " +
                               $"{_levelModel.currentLevel} LastMaxLevel : {_levelModel.lastMaxLevel}");
        }
    }
}

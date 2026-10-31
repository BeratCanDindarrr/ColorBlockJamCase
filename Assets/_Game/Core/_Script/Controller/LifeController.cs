using ColorBlockJam.Core.Model;
using ColorBlockJam.Core.Service;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Signals;
using ColorBlockJam.Utils;
using MessagePipe;
using UnityEngine;

namespace ColorBlockJam.Core.Controller
{
    // Owns the saved lives (StorageModel.lifeModel): one is spent per level played, one refills every
    // LifeRefillTime seconds up to MaxLife, and limitless life skips both until it runs out.
    public class LifeController : ITimerOwner
    {
        private readonly StorageService _storageService;
        private readonly TimerService _timerService;
        private readonly LifeSettings _lifeSettings;
        private readonly IPublisher<LifeModelSync> _lifeModelSyncPublisher;

        private LifeModel _model;
        // Limitless life bought mid-level; applied by SyncModel when the level flow is over.
        private LifeModel _storageModel;

        public LifeController(StorageService storageService
            , TimerService timerService
            , LifeSettings lifeSettings
            , IPublisher<LifeModelSync> lifeModelSyncPublisher)
        {
            _storageService = storageService;
            _timerService = timerService;
            _lifeSettings = lifeSettings;
            _lifeModelSyncPublisher = lifeModelSyncPublisher;
        }

        public void Init()
        {
            _storageService.StorageModel.lifeModel ??= new LifeModel
            {
                currentLife = _lifeSettings.MaxLife,
                isLimitless = false,
                limitlessFinishTime = -1,
                lastChangeTime = _timerService.TotalSeconds()
            };

            _model = _storageService.StorageModel.lifeModel;

            CalculateCurrentLife();
            _timerService.Subscribe(this);
        }

        public void SyncModel()
        {
            if (_storageModel != null)
            {
                _storageService.StorageModel.lifeModel = _storageModel;
                _model = _storageModel;
                _storageModel = null;
            }

            CalculateCurrentLife();
            _lifeModelSyncPublisher.Publish(new LifeModelSync());
        }

        public void AddLimitlessLife(long addedSeconds)
        {
            var now = _timerService.TotalSeconds();
            _storageModel = _model.Clone();
            _storageModel.isLimitless = true;
            _storageModel.currentLife = _lifeSettings.MaxLife;
            _storageModel.lastChangeTime = now;

            if (_storageModel.limitlessFinishTime < now)
                _storageModel.limitlessFinishTime = now + addedSeconds;
            else
                _storageModel.limitlessFinishTime += addedSeconds;
        }

        public void RefillLives()
        {
            if (IsLimitlessActive()) return;

            if (_model.currentLife >= _lifeSettings.MaxLife)
            {
                _model.lastChangeTime = _timerService.TotalSeconds();
                return;
            }

            _model.isLimitless = false;
            _model.currentLife = _lifeSettings.MaxLife;

            CalculateCurrentLife();
        }

        public void GiveLife()
        {
            if (IsLimitlessActive()) return;

            if (_model.currentLife >= _lifeSettings.MaxLife)
            {
                _model.lastChangeTime = _timerService.TotalSeconds();
                return;
            }

            _model.isLimitless = false;
            _model.currentLife = Mathf.Max(0, _model.currentLife + 1);

            CalculateCurrentLife();
        }

        public void RemoveLife()
        {
            if (IsLimitlessActive()) return;

            // The refill timer starts from the moment the lives drop below max.
            if (_model.currentLife >= _lifeSettings.MaxLife)
                _model.lastChangeTime = _timerService.TotalSeconds();

            _model.isLimitless = false;
            _model.currentLife = Mathf.Max(0, _model.currentLife - 1);

            CalculateCurrentLife();
        }

        public void ResetLives()
        {
            var now = _timerService.TotalSeconds();
            _model.isLimitless = false;
            _model.limitlessFinishTime = now;
            _model.lastChangeTime = now;
            _model.currentLife = 0;

            CalculateCurrentLife();
        }

        public bool HasLife()
        {
            return _model.currentLife > 0;
        }

        public int GetCurrentLife()
        {
            return _model.currentLife;
        }

        // Life count as text ("∞" while limitless) and the seconds until it next changes (-1: no timer).
        public (string, long) GetLifeTime()
        {
            if (_model.isLimitless)
                return ("∞", _timerService.SecondsLeft(_model.limitlessFinishTime));

            if (_model.currentLife >= _lifeSettings.MaxLife)
                return (_model.currentLife.ToString(), -1);

            return (_model.currentLife.ToString(),
                _timerService.SecondsLeft(_model.lastChangeTime + _lifeSettings.LifeRefillTime));
        }

        public bool IsTimerActive()
        {
            return _model.isLimitless || _model.currentLife < _lifeSettings.MaxLife;
        }

        public bool IsLimitless()
        {
            return _model.isLimitless;
        }

        public void UpdateTime(float intervalTime)
        {
            var life = _model.currentLife;
            var isLimitless = _model.isLimitless;
            CalculateCurrentLife();
            // Refills happen on the clock, not on a player action, so the header is told here.
            if (life != _model.currentLife || isLimitless != _model.isLimitless)
                _lifeModelSyncPublisher.Publish(new LifeModelSync());
        }

        public void ResumeTime(float passedRealtime)
        {
            UpdateTime(passedRealtime);
        }

        private bool IsLimitlessActive()
        {
            return _model.isLimitless && _model.limitlessFinishTime >= _timerService.TotalSeconds();
        }

        private void CalculateCurrentLife()
        {
            var now = _timerService.TotalSeconds();
            if (_model.isLimitless)
            {
                if (_model.limitlessFinishTime > now) return;

                _model.isLimitless = false;
                _model.currentLife = _lifeSettings.MaxLife;
                _model.lastChangeTime = now;
                DebugExtension.Log($"Limitless life ended, life : {_model.currentLife}");
                return;
            }

            if (_model.currentLife >= _lifeSettings.MaxLife) return;

            var addedLifeCount = (int)((now - _model.lastChangeTime) / _lifeSettings.LifeRefillTime);
            if (addedLifeCount <= 0) return;

            _model.currentLife = Mathf.Min(_lifeSettings.MaxLife, _model.currentLife + addedLifeCount);
            _model.lastChangeTime += addedLifeCount * _lifeSettings.LifeRefillTime;

            DebugExtension.Log($"addedLifeCount : {addedLifeCount} life : {_model.currentLife} " +
                               $"lastChangeTime : {_model.lastChangeTime}");
        }
    }
}

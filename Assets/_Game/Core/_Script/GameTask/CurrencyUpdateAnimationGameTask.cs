using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Pool;
using ColorBlockJam.Views;
using ColorBlockJam.Views.Currency;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using VContainer;

namespace ColorBlockJam.GameTask
{
    // Rewards already added to the player fly onto the main screen, from the screen centre or given screen points (knit_Out's MainViewCoinAnimationTask): coins to the
    // header coin (its label counting up as they land), limitless life time to the life icon, boosters to Play.
    public class CurrencyUpdateAnimationGameTask : IGameTask
    {
        private CurrencyData[] _updatedArrayValue;
        // Screen points the rewards fly from, one per reward; none: the screen centre.
        private Vector2[] _startScreenPoints;

        private ScreenController _screenController;
        private CurrencyController _currencyController;
        private CurrencySettings _settings;
        private IMemoryPool<CoinCurrencyView> _coinPool;
        private IMemoryPool<LifeTimeCurrencyView> _lifeTimePool;
        private IMemoryPool<BoosterCurrencyView> _boosterPool;
        private bool _coinCounting;

        public void Init(CurrencyData[] currencyDataArray, Vector2[] startScreenPoints = null)
        {
            _updatedArrayValue = currencyDataArray;
            _startScreenPoints = startScreenPoints;
        }

        [Inject]
        public void Construct(ScreenController screenController
            , CurrencyController currencyController
            , CurrencySettings settings
            , IMemoryPool<CoinCurrencyView> coinPool
            , IMemoryPool<LifeTimeCurrencyView> lifeTimePool
            , IMemoryPool<BoosterCurrencyView> boosterPool)
        {
            _screenController = screenController;
            _currencyController = currencyController;
            _settings = settings;
            _coinPool = coinPool;
            _lifeTimePool = lifeTimePool;
            _boosterPool = boosterPool;
        }

        public async UniTask RunTask(CancellationToken cancellationToken)
        {
            if (_updatedArrayValue == null || _updatedArrayValue.Length == 0) return;

            // Queued while leaving a level: the icons fly once the main screen is up.
            await UniTask.WaitUntil(() => _screenController.ScreenStatus == ScreenType.MainScreen,
                cancellationToken: cancellationToken);
            var screen = _screenController.MainScreenView;
            if (screen.FlyLayer == null)
            {
                Debug.LogWarning($"{nameof(CurrencyUpdateAnimationGameTask)}: the main screen has no currency targets set.");
                return;
            }

            // The coins are in the save already; the label starts from before them and counts up as they land.
            var coins = _updatedArrayValue.Where(c => c.currencyType == CurrencyType.Coin).Sum(c => c.currencyValue);
            if (coins > 0) screen.Header.SetCoin(_currencyController.GetCurrency(CurrencyType.Coin) - coins);

            var flights = new List<UniTask>();
            for (var i = 0; i < _updatedArrayValue.Length; i++)
                flights.Add(FlyReward(screen, _updatedArrayValue[i], StartOf(screen, i), coins, cancellationToken));
            await UniTask.WhenAll(flights);
        }

        // Where reward `index` appears, on the fly layer's plane.
        private Vector3 StartOf(MainScreenView screen, int index)
        {
            var layer = (RectTransform)screen.FlyLayer;
            if (_startScreenPoints == null || index >= _startScreenPoints.Length) return layer.position;

            var camera = layer.GetComponent<Canvas>().worldCamera;
            return RectTransformUtility.ScreenPointToWorldPointInRectangle(layer, _startScreenPoints[index], camera, out var point)
                ? point
                : layer.position;
        }

        private async UniTask FlyReward(MainScreenView screen, CurrencyData reward, Vector3 origin, long coins,
            CancellationToken token)
        {
            var target = TargetOf(screen, reward.currencyType);
            if (target == null) return;

            var count = reward.currencyType == CurrencyType.Coin
                ? (int)Mathf.Clamp(reward.currencyValue, 1, _settings.MaxCoinViews)
                : 1;
            var unit = screen.FlyLayer.lossyScale.x;
            var flights = new List<UniTask>();
            for (var i = 0; i < count; i++)
            {
                var start = origin + (Vector3)(Random.insideUnitCircle * _settings.SpawnSpread * unit);
                flights.Add(FlyOne(screen, reward, start, target, coins, token));
                await UniTask.Delay(Mathf.RoundToInt(_settings.SpawnInterval * 1000f), cancellationToken: token);
            }

            await UniTask.WhenAll(flights);
        }

        private async UniTask FlyOne(MainScreenView screen, CurrencyData reward, Vector3 start, Transform target,
            long coins, CancellationToken token)
        {
            var view = Spawn(reward.currencyType);
            view.transform.SetParent(screen.FlyLayer, false);
            try
            {
                view.CreateView(reward.currencyType, start, reward.currencyValue);
                await view.FlyView(target.position, token);
                Land(screen, target, reward.currencyType, coins);
            }
            finally
            {
                Despawn(view, reward.currencyType);
            }
        }

        private void Land(MainScreenView screen, Transform target, CurrencyType currencyType, long coins)
        {
            target.DOKill(true);
            target.DOPunchScale(Vector3.one * _settings.TargetPunch, _settings.TargetPunchDuration, 1, 0f)
                .SetLink(target.gameObject);

            if (currencyType != CurrencyType.Coin || _coinCounting) return;
            _coinCounting = true;
            var now = _currencyController.GetCurrency(CurrencyType.Coin);
            screen.Header.CountCoin(now - coins, now, _settings.CoinCountDuration);
        }

        private static Transform TargetOf(MainScreenView screen, CurrencyType currencyType)
        {
            switch (currencyType)
            {
                case CurrencyType.Coin: return screen.CoinImageTransform;
                case CurrencyType.Time: return screen.LifeImageTransform;
                case CurrencyType.Booster1:
                case CurrencyType.Booster2:
                case CurrencyType.Booster3: return screen.PlayButtonTransform;
                default: return null;
            }
        }

        private CurrencyFlyView Spawn(CurrencyType currencyType)
        {
            switch (currencyType)
            {
                case CurrencyType.Coin: return _coinPool.Spawn();
                case CurrencyType.Time: return _lifeTimePool.Spawn();
                default: return _boosterPool.Spawn();
            }
        }

        private void Despawn(CurrencyFlyView view, CurrencyType currencyType)
        {
            switch (currencyType)
            {
                case CurrencyType.Coin: _coinPool.Despawn((CoinCurrencyView)view); break;
                case CurrencyType.Time: _lifeTimePool.Despawn((LifeTimeCurrencyView)view); break;
                default: _boosterPool.Despawn((BoosterCurrencyView)view); break;
            }
        }

        public void Dispose()
        {
        }
    }
}

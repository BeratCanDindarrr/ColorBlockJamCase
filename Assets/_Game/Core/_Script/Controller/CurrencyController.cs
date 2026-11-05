using ColorBlockJam.Core.Model;
using ColorBlockJam.Core.Service;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.Signals;
using MessagePipe;

namespace ColorBlockJam.Core.Controller
{
    // Owns the saved currencies (StorageModel.currencyModel). Time is not stored here: it buys limitless life.
    public class CurrencyController
    {
        private readonly StorageService _storageService;
        private readonly LifeController _lifeController;
        private readonly CurrencySettings _currencySettings;
        private readonly IPublisher<CurrencyUpdateSignal> _currencyUpdatePublisher;
        private readonly IPublisher<CoinUpdateSignal> _coinUpdatePublisher;

        private CurrencyModel _model;
        // Coins added for a reward fly animation that has not counted them into the header yet.
        private long _coinsInFlight;

        // The coin total a label should show now: the flying coins are counted in by the animation.
        public long ShownCoin => _model.coin - _coinsInFlight;

        public CurrencyController(StorageService storageService
            , LifeController lifeController
            , CurrencySettings currencySettings
            , IPublisher<CurrencyUpdateSignal> currencyUpdatePublisher
            , IPublisher<CoinUpdateSignal> coinUpdatePublisher)
        {
            _storageService = storageService;
            _lifeController = lifeController;
            _currencySettings = currencySettings;
            _currencyUpdatePublisher = currencyUpdatePublisher;
            _coinUpdatePublisher = coinUpdatePublisher;
        }

        public void Init()
        {
            _storageService.StorageModel.currencyModel ??= new CurrencyModel
            {
                coin = _currencySettings.StartCoin,
                booster1 = _currencySettings.StartBoosterCount,
                booster2 = _currencySettings.StartBoosterCount,
                booster3 = _currencySettings.StartBoosterCount,
                booster4 = _currencySettings.StartBoosterCount,
                removeAdCount = 0
            };

            _model = _storageService.StorageModel.currencyModel;
        }

        public long GetCurrency(CurrencyType currencyType)
        {
            switch (currencyType)
            {
                case CurrencyType.Coin:
                    return _model.coin;
                case CurrencyType.Booster1:
                    return _model.booster1;
                case CurrencyType.Booster2:
                    return _model.booster2;
                case CurrencyType.Booster3:
                    return _model.booster3;
                case CurrencyType.Booster4:
                    return _model.booster4;
                case CurrencyType.RemoveAds:
                    return _model.removeAdCount;
                default:
                    return 0;
            }
        }

        public bool HasEnoughCurrency(CurrencyData currencyData)
        {
            switch (currencyData.currencyType)
            {
                case CurrencyType.Coin:
                case CurrencyType.Booster1:
                case CurrencyType.Booster2:
                case CurrencyType.Booster3:
                case CurrencyType.Booster4:
                case CurrencyType.RemoveAds:
                    return currencyData.currencyValue <= GetCurrency(currencyData.currencyType);
                default:
                    return false;
            }
        }

        public bool IsAdsRemoved()
        {
            return GetCurrency(CurrencyType.RemoveAds) > 0;
        }

        public void UpdateCurrency(CurrencyData currencyData)
        {
            switch (currencyData.currencyType)
            {
                case CurrencyType.Coin:
                    _model.coin = currencyData.currencyValue;
                    break;
                case CurrencyType.Booster1:
                    _model.booster1 = currencyData.currencyValue;
                    break;
                case CurrencyType.Booster2:
                    _model.booster2 = currencyData.currencyValue;
                    break;
                case CurrencyType.Booster3:
                    _model.booster3 = currencyData.currencyValue;
                    break;
                case CurrencyType.Booster4:
                    _model.booster4 = currencyData.currencyValue;
                    break;
                case CurrencyType.RemoveAds:
                    _model.removeAdCount = currencyData.currencyValue;
                    break;
            }
        }

        public void AddCurrency(CurrencyData currencyData, bool fireSignal)
        {
            if (currencyData.currencyType == CurrencyType.Time)
            {
                _lifeController.AddLimitlessLife(currencyData.currencyValue);
                return;
            }

            var newValue = GetCurrency(currencyData.currencyType) + currencyData.currencyValue;
            UpdateCurrency(new CurrencyData(currencyData.currencyType, newValue));

            // Without fireSignal a reward fly animation shows the gain (CurrencyUpdateAnimationGameTask).
            if (currencyData.currencyType == CurrencyType.Coin && !fireSignal) _coinsInFlight += currencyData.currencyValue;
            if (currencyData.currencyType == CurrencyType.Coin && fireSignal)
                _coinUpdatePublisher.Publish(new CoinUpdateSignal(currencyData.currencyValue));

            _currencyUpdatePublisher.Publish(new CurrencyUpdateSignal(currencyData, CurrencyUpdateType.Gain));
        }

        // The fly animation starts counting these coins into the header.
        public void LandCoins(long coins) => _coinsInFlight = System.Math.Max(0, _coinsInFlight - coins);

        public CurrencySpendResult TryConsume(CurrencyData currencyData)
        {
            if (!HasEnoughCurrency(currencyData)) return CurrencySpendResult.NotEnough;

            var newValue = GetCurrency(currencyData.currencyType) - currencyData.currencyValue;
            UpdateCurrency(new CurrencyData(currencyData.currencyType, newValue));

            _currencyUpdatePublisher.Publish(new CurrencyUpdateSignal(currencyData, CurrencyUpdateType.Consume));

            return CurrencySpendResult.Success;
        }

        public void ResetCurrency(CurrencyType currencyType)
        {
            if (currencyType == CurrencyType.Time)
            {
                _lifeController.ResetLives();
                return;
            }

            var currencyData = new CurrencyData(currencyType, 0);
            UpdateCurrency(currencyData);

            _currencyUpdatePublisher.Publish(new CurrencyUpdateSignal(currencyData, CurrencyUpdateType.Consume));
        }
    }
}

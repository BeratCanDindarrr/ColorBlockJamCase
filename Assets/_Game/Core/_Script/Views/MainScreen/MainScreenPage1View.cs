using System;
using System.Collections.Generic;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Utils;
using ColorBlockJam.Signals;
using MessagePipe;
using TMPro;
using UnityEngine;
using VContainer;

namespace ColorBlockJam.Views
{
    // The shop page: the coins the player has, the coin packs and the bundles of ShopSettings. Buying an offer goes to
    // the home page and hands its rewards to the Collect popup, whose Collect adds them and flies them in.
    public class MainScreenPage1View : MainScreenBasePageView
    {
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private Transform coinPackRoot;
        [SerializeField] private ShopCoinPackView coinPackTemplate;
        [SerializeField] private Transform bundleRoot;
        [SerializeField] private ShopBundleView bundleTemplate;

        private readonly List<BaseView> _offers = new List<BaseView>();
        private ShopSettings _shopSettings;
        private CurrencySettings _currencySettings;
        private CurrencyController _currencyController;
        private ScreenController _screenController;
        private IPopupService _popupService;
        private ISubscriber<CurrencyUpdateSignal> _currencyUpdateSubscriber;
        private IDisposable _subscription;

        [Inject]
        public void Construct(ShopSettings shopSettings, CurrencySettings currencySettings,
            CurrencyController currencyController, ScreenController screenController, IPopupService popupService,
            ISubscriber<CurrencyUpdateSignal> currencyUpdateSubscriber)
        {
            _shopSettings = shopSettings;
            _currencySettings = currencySettings;
            _currencyController = currencyController;
            _screenController = screenController;
            _popupService = popupService;
            _currencyUpdateSubscriber = currencyUpdateSubscriber;
        }

        public override void Initialize()
        {
            coinPackTemplate.gameObject.SetActive(false);
            bundleTemplate.gameObject.SetActive(false);
            foreach (var offer in _shopSettings.CoinPacks)
            {
                var view = Instantiate(coinPackTemplate, coinPackRoot);
                view.gameObject.SetActive(true);
                view.Set(offer, OnBought);
                _offers.Add(view);
            }

            foreach (var offer in _shopSettings.Bundles)
            {
                var view = Instantiate(bundleTemplate, bundleRoot);
                view.gameObject.SetActive(true);
                view.Set(offer, _currencySettings, OnBought);
                _offers.Add(view);
            }

            _subscription = _currencyUpdateSubscriber.Subscribe(_ => RefreshCoins());
            RefreshCoins();
        }

        public override void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
            foreach (var offer in _offers)
            {
                if (offer != null) offer.Dispose();
            }

            _offers.Clear();
        }

        private void RefreshCoins() => coinText.SetText(CurrencyText.Grouped(_currencyController.GetCurrency(CurrencyType.Coin)));

        private void OnBought(ShopSettings.ShopOffer offer)
        {
            _screenController.MainScreenView.SnapToPage(MainScreenView.HomePage);
            _popupService.AddPopup(new CollectPopupParameters { Rewards = offer.Rewards });
        }
    }
}

using ColorBlockJam.Core.Controller;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Enums;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Signals;
using ColorBlockJam.Utils;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    // Time ran out: buy more time with coins ("Play On") or give up (asks again in QuitLevelPopup).
    public class OutOfMovesPopup : BasePopup<OutOfMovesPopupParameters>
    {
        [SerializeField] private Button playOnButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI costText;

        private CurrencyController _currencyController;
        private IPopupService _popupService;
        private ApplicationController _applicationController;
        private SceneLoaderController _sceneLoaderController;
        private IPublisher<TimePurchasedSignal> _timePurchasedPublisher;

        [Inject]
        public void Construct(CurrencyController currencyController
            , IPopupService popupService
            , ApplicationController applicationController
            , SceneLoaderController sceneLoaderController
            , IPublisher<TimePurchasedSignal> timePurchasedPublisher)
        {
            _sceneLoaderController = sceneLoaderController;
            _currencyController = currencyController;
            _popupService = popupService;
            _applicationController = applicationController;
            _timePurchasedPublisher = timePurchasedPublisher;
        }

        public override void ShowPopup()
        {
            costText.SetText(Parameters.AddTimeCoinValue.ToString());
            playOnButton.onClick.AddListener(PlayOnButtonClicked);
            closeButton.onClick.AddListener(CloseButtonClicked);
        }

        protected override void Dispose()
        {
            playOnButton.onClick.RemoveListener(PlayOnButtonClicked);
            closeButton.onClick.RemoveListener(CloseButtonClicked);
        }

        private void PlayOnButtonClicked()
        {
            if (CloseIfLevelLeft()) return;

            var price = new CurrencyData(CurrencyType.Coin, Parameters.AddTimeCoinValue);
            if (_currencyController.TryConsume(price) != CurrencySpendResult.Success)
            {
                // TODO: open ShopPopup and reopen this popup when it closes (not in this project yet).
                DebugExtension.Log("Not enough coins for Play On");
                return;
            }

            _timePurchasedPublisher.Publish(new TimePurchasedSignal(Parameters.AddTimeSeconds));
            ClosePopup();
        }

        private void CloseButtonClicked()
        {
            if (CloseIfLevelLeft()) return;

            _popupService.AddPopup(new QuitLevelPopupParameters
            {
                OnCloseAction = _applicationController.OpenOutOfMovesPopup
            });
            ClosePopup();
        }

        // The level was already left (e.g. quit from the settings popup queued before this one): nothing to play on or give up.
        private bool CloseIfLevelLeft()
        {
            if (_sceneLoaderController.IsGamePlaySceneLoaded()) return false;

            ClosePopup();
            return true;
        }
    }
}

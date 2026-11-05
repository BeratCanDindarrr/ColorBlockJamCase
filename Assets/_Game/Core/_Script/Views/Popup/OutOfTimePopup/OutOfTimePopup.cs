using ColorBlockJam.Core.Controller;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Enums;
using ColorBlockJam.Signals;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    // Out of Time: the clock with the seconds on offer, and Continue buys them with coins; the X gives up the level
    // (Level Failed). The price turns red when the player has too few coins (Continue then does nothing).
    public class OutOfTimePopup : BasePopup<OutOfTimePopupParameters>
    {
        private const string BonusFormat = "+{0}";
        private const string MessageFormat = "Get {0} seconds to keep playing!";

        [SerializeField] private Button playOnButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI bonusText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Color notEnoughCoinsColor = new Color(0.93f, 0.16f, 0.16f);

        private CurrencyController _currencyController;
        private ApplicationController _applicationController;
        private SceneLoaderController _sceneLoaderController;
        private IPublisher<TimePurchasedSignal> _timePurchasedPublisher;

        [Inject]
        public void Construct(CurrencyController currencyController
            , ApplicationController applicationController
            , SceneLoaderController sceneLoaderController
            , IPublisher<TimePurchasedSignal> timePurchasedPublisher)
        {
            _sceneLoaderController = sceneLoaderController;
            _currencyController = currencyController;
            _applicationController = applicationController;
            _timePurchasedPublisher = timePurchasedPublisher;
        }

        public override void ShowPopup()
        {
            var seconds = Mathf.RoundToInt(Parameters.AddTimeSeconds);
            bonusText.SetText(BonusFormat, seconds);
            messageText.SetText(MessageFormat, seconds);
            costText.SetText(Parameters.AddTimeCoinValue.ToString());
            var affordable = _currencyController.HasEnoughCurrency(new CurrencyData(CurrencyType.Coin, Parameters.AddTimeCoinValue));
            if (!affordable) costText.color = notEnoughCoinsColor;
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
            if (_currencyController.TryConsume(price) != CurrencySpendResult.Success) return;

            _timePurchasedPublisher.Publish(new TimePurchasedSignal(Parameters.AddTimeSeconds));
            ClosePopup();
        }

        private void CloseButtonClicked()
        {
            if (CloseIfLevelLeft()) return;

            ClosePopup();
            _applicationController.QuitLevel();
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

using System;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.Signals;
using DG.Tweening;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    // One HUD booster button: the owned count, or the coin price when none is left,
    // or a lock with the unlock level. Clicked (wired in the prefab's onClick): with one owned it asks the board to start
    // the booster (the board spends it only when used); with none it buys one for coins.
    public class InGameBoosterView : BaseView
    {
        private const string LockFormat = "Lvl {0}";
        // A refused purchase shakes the button this far (canvas units) for this long.
        private const float RefuseShakeStrength = 12f;
        private const float RefuseShakeDuration = 0.3f;

        [SerializeField] private CurrencyType boosterType;
        [SerializeField] private Button button;
        [SerializeField] private GameObject countBadge;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private GameObject priceTag;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private GameObject lockedView;
        [SerializeField] private TextMeshProUGUI lockText;
        // Shown while the booster's effect lasts (Timer: the frozen clock over the normal one); may be empty.
        [SerializeField] private GameObject activeEffect;

        private CurrencyController _currencyController;
        private PlayerController _playerController;
        private BoosterSettings _boosterSettings;
        private IPublisher<BoosterRequestedSignal> _requestedPublisher;
        private bool _blocked;
        private bool _unavailable;

        public CurrencyType BoosterType => boosterType;

        public void Construct(CurrencyController currencyController, PlayerController playerController,
            BoosterSettings boosterSettings, IPublisher<BoosterRequestedSignal> requestedPublisher)
        {
            _currencyController = currencyController;
            _playerController = playerController;
            _boosterSettings = boosterSettings;
            _requestedPublisher = requestedPublisher;
        }

        private bool IsLocked => _playerController.GetVisualLevel() < _boosterSettings.Get(boosterType).UnlockLevel;

        // Blocked while any booster runs (and, for a booster that needs it, until the level clock runs).
        public void SetBlocked(bool blocked)
        {
            _blocked = blocked;
            Refresh();
        }

        // The board cannot take this booster now (Timer: the clock is not running yet, or is still frozen).
        public void SetUnavailable(bool unavailable)
        {
            if (_unavailable == unavailable) return;
            _unavailable = unavailable;
            Refresh();
        }

        public void SetEffectActive(bool active)
        {
            if (activeEffect != null) activeEffect.SetActive(active);
        }

        public void Refresh()
        {
            var data = _boosterSettings.Get(boosterType);
            var locked = IsLocked;
            var count = _currencyController.GetCurrency(boosterType);

            lockedView.SetActive(locked);
            lockText.SetText(LockFormat, data.UnlockLevel);
            countBadge.SetActive(!locked && count > 0);
            countText.SetText("{0}", count);
            priceTag.SetActive(!locked && count <= 0);
            priceText.SetText("{0}", data.Price);
            button.interactable = !locked && !_blocked && !_unavailable;
        }

        public void OnClicked()
        {
            if (_blocked || _unavailable || IsLocked) return;

            if (_currencyController.GetCurrency(boosterType) > 0)
            {
                _requestedPublisher.Publish(new BoosterRequestedSignal(boosterType, ScreenPoint()));
                return;
            }

            Buy();
        }

        private Vector2 ScreenPoint()
        {
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.WorldToScreenPoint(camera, transform.position);
        }

        // No shop in this project yet: a purchase the coins cannot cover just shakes the button.
        private void Buy()
        {
            var price = new CurrencyData(CurrencyType.Coin, _boosterSettings.Get(boosterType).Price);
            if (_currencyController.TryConsume(price) != CurrencySpendResult.Success)
            {
                transform.DOKill(true);
                transform.DOShakePosition(RefuseShakeDuration, new Vector3(RefuseShakeStrength, 0f, 0f))
                    .SetLink(gameObject);
                return;
            }

            _currencyController.AddCurrency(new CurrencyData(boosterType, 1), false);
        }
    }
}

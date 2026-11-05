using System;
using ColorBlockJam.Utils;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    // Top bar of the home page: profile, coin, life and settings; coin and life are buttons too.
    public class MainScreenHeaderView : BaseView
    {
        private const string FullText = "FULL";

        [SerializeField] private Button profileButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button coinButton;
        [SerializeField] private Button lifeButton;
        [SerializeField] private TextMeshProUGUI coinText;
        // Beside the heart: the time to the next life, how long limitless lives last, or FULL.
        [FormerlySerializedAs("lifeText")] [SerializeField] private TextMeshProUGUI lifeTimerText;
        // On the heart: the life count, or ∞ while lives are limitless.
        [FormerlySerializedAs("lifeTimeText")] [SerializeField] private TextMeshProUGUI lifeCountText;
        // The ∞ is drawn this many times the count's size.
        [SerializeField] private float infinityScale = 1.6f;
        [SerializeField] private Transform coinImageTransform;
        [SerializeField] private Transform lifeImageTransform;
        public Transform CoinImageTransform => coinImageTransform;
        public Transform LifeImageTransform => lifeImageTransform;
        public event Action ProfileClicked;
        public event Action SettingsClicked;
        public event Action CoinClicked;
        public event Action LifeClicked;

        public override void Initialize()
        {
            profileButton.onClick.AddListener(OnProfileClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            coinButton.onClick.AddListener(OnCoinClicked);
            lifeButton.onClick.AddListener(OnLifeClicked);
        }

        public override void Dispose()
        {
            if (profileButton != null) profileButton.onClick.RemoveListener(OnProfileClicked);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
            if (coinButton != null) coinButton.onClick.RemoveListener(OnCoinClicked);
            if (lifeButton != null) lifeButton.onClick.RemoveListener(OnLifeClicked);
        }

        public void SetCoin(long amount)
        {
            coinText.DOKill();
            coinText.SetText(amount.ToString());
        }

        // Counts the coin label from `from` to `to`, as reward coins land.
        public void CountCoin(long from, long to, float duration)
        {
            coinText.DOKill();
            var shown = from;
            DOTween.To(() => shown, value =>
                {
                    shown = value;
                    coinText.SetText(shown.ToString());
                }, to, duration)
                .SetEase(Ease.OutQuad)
                .SetTarget(coinText)
                .SetLink(gameObject);
        }

        // secondsLeft: until the next life (or the end of limitless lives); below zero when the lives are full.
        public void SetLife(string life, bool limitless, long secondsLeft)
        {
            lifeCountText.SetText(limitless ? $"<size={infinityScale * 100f:0}%>{life}</size>" : life);
            lifeTimerText.SetText(secondsLeft < 0 ? FullText : CurrencyText.Countdown(secondsLeft));
        }

        private void OnProfileClicked() => ProfileClicked?.Invoke();

        private void OnSettingsClicked() => SettingsClicked?.Invoke();

        private void OnCoinClicked() => CoinClicked?.Invoke();

        private void OnLifeClicked() => LifeClicked?.Invoke();
    }
}

using System.Threading;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Pool;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views.Currency
{
    // The fly animation shared by the currency icons: scale in where the
    // reward was given, wait, then a curved flight onto the target. Pooled; the label is optional.
    public abstract class CurrencyFlyView : BaseView, ICurrencyUpdate, IPoolable
    {
        [SerializeField] private TextMeshProUGUI valueLabel;
        // Optional: takes the reward type's icon from CurrencySettings (one booster view serves every booster).
        [SerializeField] private Image icon;

        private CurrencySettings _settings;

        [Inject]
        public void Construct(CurrencySettings settings)
        {
            _settings = settings;
        }

        // The label text for `value`, or null for no label.
        protected abstract string FormatValue(CurrencyType currencyType, float value);

        public void CreateView(CurrencyType currencyType, Vector3 position, float value)
        {
            transform.position = position;
            transform.localScale = Vector3.zero;
            transform.SetAsLastSibling();
            if (icon != null) icon.sprite = _settings.IconOf(currencyType);

            var text = FormatValue(currencyType, value);
            if (valueLabel != null)
            {
                valueLabel.gameObject.SetActive(text != null);
                valueLabel.SetText(text ?? "");
            }

            transform.DOScale(Vector3.one, _settings.AppearDuration).SetEase(Ease.OutBack).SetLink(gameObject);
        }

        public async UniTask FlyView(Vector3 targetPos, CancellationToken cancellationToken)
        {
            // Canvas units to world units, so the curve looks the same at every screen size.
            var unit = transform.parent != null ? transform.parent.lossyScale.x : 1f;
            var start = transform.position;
            var side = Random.Range(-1f, 1f) * _settings.CurveSide * unit;
            var path = new[]
            {
                targetPos,
                start + new Vector3(side, 0f, 0f),
                start + new Vector3(side, _settings.CurveHeight * unit, 0f)
            };

            var flight = DOTween.Sequence()
                .AppendInterval(_settings.FlyDelay)
                .Append(transform.DOPath(path, _settings.FlyDuration, PathType.CubicBezier).SetEase(Ease.InQuad))
                .Join(transform.DOScale(Vector3.one * _settings.LandScale, _settings.FlyDuration))
                .SetLink(gameObject);

            var landed = new UniTaskCompletionSource();
            flight.OnKill(() => landed.TrySetResult());
            using (cancellationToken.Register(() => flight.Kill()))
            {
                await landed.Task;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        public void OnSpawned() { }

        public void OnDespawned() => transform.DOKill();
    }
}

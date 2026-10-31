using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    // Footer tab button: selected tab shows its highlight, lifts and grows its icon and reveals its title.
    public class MainScreenMenuButtonView : BaseView
    {
        [SerializeField] private Button button;
        [SerializeField] private GameObject highlightImage;
        [SerializeField] private RectTransform iconRect;
        [SerializeField] private CanvasGroup titleCanvasGroup;
        [SerializeField] private float highlightIconOffsetY = 60f;
        [SerializeField] private float highlightIconScale = 1.2f;
        [SerializeField] private float tweenDuration = 0.2f;

        private Vector2 _iconRestPosition;

        public event Action<MainScreenMenuButtonView> Clicked;

        public override void Initialize()
        {
            _iconRestPosition = iconRect.anchoredPosition;
            button.onClick.AddListener(OnClicked);
            SetHighlight(false, true);
        }

        public override void Dispose()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
            if (iconRect != null) iconRect.DOKill();
        }

        public void Highlight() => SetHighlight(true, false);

        public void CancelHighlight() => SetHighlight(false, false);

        private void SetHighlight(bool active, bool instant)
        {
            highlightImage.SetActive(active);
            titleCanvasGroup.alpha = active ? 1f : 0f;

            var targetPosition = _iconRestPosition + (active ? new Vector2(0f, highlightIconOffsetY) : Vector2.zero);
            var targetScale = Vector3.one * (active ? highlightIconScale : 1f);
            iconRect.DOKill();
            if (instant)
            {
                iconRect.anchoredPosition = targetPosition;
                iconRect.localScale = targetScale;
                return;
            }
            iconRect.DOAnchorPos(targetPosition, tweenDuration).SetLink(iconRect.gameObject);
            iconRect.DOScale(targetScale, tweenDuration).SetLink(iconRect.gameObject);
        }

        private void OnClicked() => Clicked?.Invoke(this);
    }
}

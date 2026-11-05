using System;
using ColorBlockJam.Core.Settings;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    // Tutorial overlay: a hand tapping a point or dragging along a path over and over, and a dim frame around
    // a button. The frame has the button in its hole, so the button is tapped where it is while the frame takes every
    // other tap. Lives full screen at the top of the HUD; the hand takes no taps.
    public class TutorialView : BaseView
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform handParent;
        [SerializeField] private Image handImage;

        // Top, bottom, left and right of the hole; made when first needed.
        private readonly Image[] _frame = new Image[4];
        private Sequence _handSequence;
        private Func<Vector2> _tapPoint;
        private BoosterSettings _settings;

        private RectTransform Root => (RectTransform)transform;
        private Camera CanvasCamera => canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // Dims everything but the button and taps it.
        public void ShowButtonHint(RectTransform button, BoosterSettings settings)
        {
            gameObject.SetActive(true);
            var buttonCanvas = button.GetComponentInParent<Canvas>().rootCanvas;
            var buttonCamera = buttonCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : buttonCanvas.worldCamera;
            PlaceFrame(button, buttonCamera, settings);
            ShowTap(() => RectTransformUtility.WorldToScreenPoint(buttonCamera, button.position), settings);
        }

        // Only the hand, tapping a point of the screen; everything stays tappable.
        public void ShowTapHint(Vector2 screenPoint, BoosterSettings settings)
        {
            gameObject.SetActive(true);
            SetFrameActive(false);
            ShowTap(() => screenPoint, settings);
        }

        // Only the hand, pressing at the first point, dragging through the rest, letting go and fading out.
        public void ShowDragHint(Vector2[] screenPoints, BoosterSettings settings)
        {
            gameObject.SetActive(true);
            SetFrameActive(false);
            _settings = settings;
            var parent = (RectTransform)handParent.parent;
            var path = new Vector3[screenPoints.Length];
            for (var i = 0; i < path.Length; i++)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoints[i], CanvasCamera, out var local);
                path[i] = local;
            }

            handImage.gameObject.SetActive(true);
            PlayDrag(path);
        }

        public void Hide()
        {
            _handSequence?.Kill();
            SetFrameActive(false);
            gameObject.SetActive(false);
        }

        public override void Dispose()
        {
            _handSequence?.Kill();
        }

        private void ShowTap(Func<Vector2> screenPoint, BoosterSettings settings)
        {
            _tapPoint = screenPoint;
            _settings = settings;
            handImage.gameObject.SetActive(true);
            PlayTap();
        }

        // The point is asked again on every press, so a HUD that is still laying out cannot leave the hand behind.
        private void PlayTap()
        {
            _handSequence?.Kill();
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)handParent.parent, _tapPoint(),
                CanvasCamera, out var local);
            handParent.localPosition = local;
            handParent.localScale = Vector3.one;
            SetHandAlpha(1f);
            _handSequence = DOTween.Sequence().SetLink(gameObject)
                .Append(handParent.DOScale(_settings.TapPressedScale, _settings.TapPressDuration))
                .Append(handParent.DOScale(1f, _settings.TapPressDuration))
                .AppendInterval(_settings.TapRepeatDelay)
                .OnComplete(PlayTap);
        }

        private void PlayDrag(Vector3[] path)
        {
            _handSequence?.Kill();
            handParent.localPosition = path[0];
            handParent.localScale = Vector3.one;
            SetHandAlpha(0f);
            _handSequence = DOTween.Sequence().SetLink(gameObject)
                .Append(handImage.DOFade(1f, _settings.DragHintFadeDuration))
                .Append(handParent.DOScale(_settings.TapPressedScale, _settings.TapPressDuration))
                .Append(handParent.DOLocalPath(path, _settings.DragHintDuration, PathType.Linear).SetEase(Ease.InOutSine))
                .Append(handParent.DOScale(1f, _settings.TapPressDuration))
                .Append(handImage.DOFade(0f, _settings.DragHintFadeDuration))
                .AppendInterval(_settings.DragHintRepeatDelay)
                .OnComplete(() => PlayDrag(path));
        }

        private void SetHandAlpha(float alpha)
        {
            var color = handImage.color;
            color.a = alpha;
            handImage.color = color;
        }

        // The four pieces of the frame around the button's rectangle, in this view's space.
        private void PlaceFrame(RectTransform button, Camera buttonCamera, BoosterSettings settings)
        {
            var corners = new Vector3[4];
            button.GetWorldCorners(corners);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Root,
                RectTransformUtility.WorldToScreenPoint(buttonCamera, corners[0]), CanvasCamera, out var holeMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Root,
                RectTransformUtility.WorldToScreenPoint(buttonCamera, corners[2]), CanvasCamera, out var holeMax);
            var padding = Vector2.one * settings.ButtonHolePadding;
            holeMin -= padding;
            holeMax += padding;

            var whole = Root.rect;
            EnsureFrame();
            SetPiece(_frame[0], new Vector2(whole.xMin, holeMax.y), new Vector2(whole.xMax, whole.yMax), settings);
            SetPiece(_frame[1], new Vector2(whole.xMin, whole.yMin), new Vector2(whole.xMax, holeMin.y), settings);
            SetPiece(_frame[2], new Vector2(whole.xMin, holeMin.y), new Vector2(holeMin.x, holeMax.y), settings);
            SetPiece(_frame[3], new Vector2(holeMax.x, holeMin.y), new Vector2(whole.xMax, holeMax.y), settings);
            SetFrameActive(true);
        }

        private void EnsureFrame()
        {
            for (var i = 0; i < _frame.Length; i++)
            {
                if (_frame[i] != null) continue;

                var piece = new GameObject("TutorialFrame", typeof(RectTransform), typeof(Image));
                // On the view's (UI) layer: input counts a press as "over the GUI" only on that layer, so the board
                // under the frame takes no tap.
                piece.layer = gameObject.layer;
                var rect = (RectTransform)piece.transform;
                rect.SetParent(transform, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = Vector2.zero;
                // Under the hand.
                rect.SetAsFirstSibling();
                _frame[i] = piece.GetComponent<Image>();
                _frame[i].raycastTarget = true;
            }
        }

        // A piece from one corner to the other; the anchors sit in the middle, where this view's local origin is.
        private static void SetPiece(Image piece, Vector2 min, Vector2 max, BoosterSettings settings)
        {
            var rect = piece.rectTransform;
            rect.localPosition = new Vector3(min.x, min.y, 0f);
            rect.sizeDelta = new Vector2(Mathf.Max(0f, max.x - min.x), Mathf.Max(0f, max.y - min.y));
            piece.color = settings.TutorialDimColor;
        }

        private void SetFrameActive(bool active)
        {
            foreach (var piece in _frame)
            {
                if (piece != null) piece.gameObject.SetActive(active);
            }
        }
    }
}

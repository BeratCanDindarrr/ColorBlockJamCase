using System.Threading;
using ColorBlockJam.Core.Settings;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    // The main screen's Play button, labelled with the level it starts. After a win it shrinks, takes the new number,
    // springs back with a jolt and a shine.
    public class LevelButtonView : BaseView
    {
        private const string LabelFormat = "Level {0}";

        [SerializeField] private TextMeshProUGUI label;
        // White copy of the button's shape over its background, faded in for the shine.
        [SerializeField] private Image shine;

        public void SetLevel(int level)
        {
            label.SetText(LabelFormat, level);
        }

        public async UniTask PlayLevelUp(int level, LevelSettings settings, CancellationToken cancellationToken)
        {
            transform.DOKill(true);
            shine.DOKill();
            var grownAt = settings.LevelButtonShrinkDuration;
            var halfShine = settings.LevelButtonShineDuration * 0.5f;
            var sequence = DOTween.Sequence().SetLink(gameObject)
                .Append(transform.DOScale(settings.LevelButtonShrinkScale, settings.LevelButtonShrinkDuration)
                    .SetEase(Ease.InQuad))
                .AppendCallback(() => SetLevel(level))
                .Append(transform.DOScale(1f, settings.LevelButtonGrowDuration).SetEase(Ease.OutElastic))
                .Insert(grownAt, transform.DOPunchRotation(
                    new Vector3(0f, 0f, settings.LevelButtonShakeAngle), settings.LevelButtonGrowDuration))
                .Insert(grownAt, shine.DOFade(settings.LevelButtonShineAlpha, halfShine))
                .Insert(grownAt + halfShine, shine.DOFade(0f, halfShine));

            // Killed early (cancelled, button destroyed) or done, the button ends up at rest with the new number.
            var finished = new UniTaskCompletionSource();
            sequence.OnKill(() =>
            {
                if (this != null) Rest(level);
                finished.TrySetResult();
            });
            using (cancellationToken.Register(() => sequence.Kill()))
            {
                await finished.Task;
            }
        }

        private void Rest(int level)
        {
            SetLevel(level);
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            var color = shine.color;
            color.a = 0f;
            shine.color = color;
        }
    }
}

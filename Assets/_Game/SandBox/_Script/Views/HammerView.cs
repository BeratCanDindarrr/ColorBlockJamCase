using System;
using ColorBlockJam.Views;
using DG.Tweening;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // The Hammer booster's hammer, on the SelectedItem layer (drawn over the board), pivoting on the end of its handle
    // (this transform). It comes out upright on the handle's side of the target and, while scaling up with OutBack,
    // winds up: leans back away from the target and drifts a little that way. Then it leans over onto the target so its
    // striking face lands right on it; onHit runs at that moment, then it shrinks away. Mirrored art (face on the left)
    // mirrors the whole move: it comes from the right and winds up to the right.
    public class HammerView : BaseView
    {
        // The striking face in the hammer's own flat plane (x right, y up on screen) from the pivot, at scale 1 (the
        // art is mirrored, so it sits to the left).
        [SerializeField] private Vector2 face = new Vector2(-7.2f, 4.4f);

        // How much the wind-up drift rises toward the top of the screen, against 1 sideways.
        private const float WindUpLift = 0.35f;

        public struct Swing
        {
            // The target on top of the block, and the hammer's length from handle end to face (world units).
            public Vector3 Target;
            public float Size;
            public float AppearDuration;
            // How far it drifts away from the target while winding up (world units), and how far back it leans
            // (degrees, from the hit pose).
            public float Travel;
            public float RaiseAngle;
            public float StrikeDuration;
            public float FadeDuration;
        }

        public void Strike(Swing swing, Action onHit, Action onDone)
        {
            transform.DOKill();
            var scale = face.magnitude > 0f ? swing.Size / face.magnitude : 1f;
            // 1 when the face is on the art's right (comes from the left), -1 when mirrored (comes from the right).
            var side = face.x >= 0f ? 1f : -1f;
            // At the hit the face points straight at the target, away from the handle.
            var strikeYaw = (side > 0f ? 0f : 180f) - Mathf.Atan2(face.y, face.x) * Mathf.Rad2Deg;
            var pivot = swing.Target - Turn(face * scale, strikeYaw);
            // Away from the target, a little up.
            var away = new Vector3(-side, 0f, WindUpLift).normalized;

            transform.position = pivot;
            // Out upright (the art as drawn), then wound up: leaning back away from the target.
            transform.rotation = Flat(0f);
            transform.localScale = Vector3.zero;
            gameObject.SetActive(true);

            DOTween.Sequence().SetTarget(transform).SetLink(gameObject)
                .Append(transform.DOScale(scale, swing.AppearDuration).SetEase(Ease.OutBack))
                .Join(transform.DORotateQuaternion(Flat(strikeYaw + side * swing.RaiseAngle), swing.AppearDuration)
                    .SetEase(Ease.OutSine))
                .Join(transform.DOMove(pivot + away * swing.Travel, swing.AppearDuration).SetEase(Ease.OutSine))
                .Append(transform.DORotateQuaternion(Flat(strikeYaw), swing.StrikeDuration).SetEase(Ease.InQuad))
                .Join(transform.DOMove(pivot, swing.StrikeDuration).SetEase(Ease.InQuad))
                .AppendCallback(() => onHit?.Invoke())
                .Append(transform.DOScale(0f, swing.FadeDuration).SetEase(Ease.InSine))
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    onDone?.Invoke();
                });
        }

        // Lying flat (facing the top-down camera), turned counter-clockwise by yaw degrees on screen.
        private static Quaternion Flat(float yaw) => Quaternion.Euler(0f, -yaw, 0f) * Quaternion.Euler(90f, 0f, 0f);

        // A flat-plane offset (x right, y up on screen) turned counter-clockwise by yaw, on the board plane.
        private static Vector3 Turn(Vector2 offset, float yaw)
        {
            var radians = yaw * Mathf.Deg2Rad;
            return new Vector3(offset.x * Mathf.Cos(radians) - offset.y * Mathf.Sin(radians), 0f,
                offset.x * Mathf.Sin(radians) + offset.y * Mathf.Cos(radians));
        }
    }
}

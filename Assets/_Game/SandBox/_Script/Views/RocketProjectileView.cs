using System;
using System.Collections.Generic;
using ColorBlockJam.Views;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // The Rocket booster's rocket, on the SelectedItem layer (drawn over the board). Its sprite child is turned so the
    // nose points along this transform's forward, which DOPath's look-at keeps on the path. One smooth flight: from
    // under the HUD button it curves into the left of the target, swings over it to its right (half a circle, clockwise
    // on screen), then spirals in onto it, growing out of the button and shrinking on the dive. The path ends where the
    // rocket touches the target, so the piece breaks the moment it is hit.
    public class RocketProjectileView : BaseView
    {
        private const int CurveSamples = 20;
        private const int ArcSamples = 30;
        private const int DiveSamples = 16;
        // How far the approach curve leans into the circle, in orbit radii (it meets the circle along its tangent).
        private const float ApproachLean = 1f;
        // Degrees the dive keeps turning clockwise past the right point while the radius closes to nothing.
        private const float DiveSweep = 120f;
        // Look-ahead along the path (share of it) that the nose aims at.
        private const float LookAhead = 0.01f;
        private const int EaseSearchSteps = 24;

        private readonly List<Vector3> _path = new List<Vector3>();
        private readonly List<float> _distances = new List<float>();
        private Vector3 _baseScale;

        public struct Flight
        {
            public Vector3 From;
            // The target's centre at the flying height, and where the dive ends (on top of the block).
            public Vector3 Orbit;
            public Vector3 Impact;
            public float Radius;
            public float Duration;
            public Ease Ease;
            public float EndScale;
            // Seconds after leaving the button in which the rocket grows from nothing to full size.
            public float GrowDuration;
            // The flight ends (and the piece breaks) once the dive is this close to the target, seen from above.
            public float TouchRadius;
        }

        // Called once after the rocket is made: its prefab scale is its full size.
        public override void Initialize()
        {
            _baseScale = transform.localScale;
        }

        public void Fly(Flight flight, Action onArrived)
        {
            transform.DOKill();
            var diveStart = BuildPath(flight);
            var total = _distances[_distances.Count - 1];
            var diveTime = TimeAt(total > 0f ? diveStart / total : 1f, flight.Ease, flight.Duration);

            var waypoints = _path.GetRange(1, _path.Count - 1).ToArray();
            transform.position = _path[0];
            transform.rotation = Quaternion.LookRotation(_path[1] - _path[0], Vector3.up);
            transform.localScale = Vector3.zero;
            gameObject.SetActive(true);

            DOTween.Sequence().SetTarget(transform).SetLink(gameObject)
                .Append(transform.DOPath(waypoints, flight.Duration, PathType.Linear)
                    .SetLookAt(LookAhead).SetEase(flight.Ease))
                .Insert(0f, transform.DOScale(_baseScale, Mathf.Min(flight.GrowDuration, diveTime)).SetEase(Ease.OutSine))
                .Insert(diveTime, transform.DOScale(_baseScale * flight.EndScale, flight.Duration - diveTime))
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    transform.localScale = _baseScale;
                    onArrived?.Invoke();
                });
        }

        // Approach curve, half circle, inward spiral cut where it touches the target; returns the distance along the
        // path where the dive starts.
        private float BuildPath(Flight flight)
        {
            _path.Clear();
            _distances.Clear();
            var radius = flight.Radius;
            var entry = flight.Orbit + Vector3.left * radius;

            // Into the circle heading up (screen up = +z), its clockwise tangent at the left point.
            AddCurve(flight.From, entry + Vector3.back * radius * ApproachLean, entry, CurveSamples);
            for (var i = 1; i <= ArcSamples; i++)
            {
                var angle = Mathf.Lerp(180f, 0f, i / (float)ArcSamples) * Mathf.Deg2Rad;
                Add(flight.Orbit + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }

            var diveStart = _distances[_distances.Count - 1];
            // On round clockwise from the right point, the radius closing and the height dropping onto the block.
            for (var i = 1; i <= DiveSamples; i++)
            {
                var t = i / (float)DiveSamples;
                var angle = -DiveSweep * t * Mathf.Deg2Rad;
                var centre = Vector3.Lerp(flight.Orbit, flight.Impact, t);
                var point = centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius * (1f - t);
                Add(point);
                var offset = point - flight.Impact;
                offset.y = 0f;
                if (offset.magnitude <= flight.TouchRadius) break;
            }

            return diveStart;
        }

        private void AddCurve(Vector3 from, Vector3 control, Vector3 to, int samples)
        {
            for (var i = 0; i <= samples; i++)
            {
                var t = i / (float)samples;
                Add(Vector3.Lerp(Vector3.Lerp(from, control, t), Vector3.Lerp(control, to, t), t));
            }
        }

        private void Add(Vector3 point)
        {
            var last = _path.Count - 1;
            _distances.Add(last < 0 ? 0f : _distances[last] + Vector3.Distance(_path[last], point));
            _path.Add(point);
        }

        // The time (seconds) at which the eased flight has covered the given share of its path.
        private static float TimeAt(float share, Ease ease, float duration)
        {
            float low = 0f, high = duration;
            for (var i = 0; i < EaseSearchSteps; i++)
            {
                var mid = (low + high) * 0.5f;
                if (EaseManager.Evaluate(ease, null, mid, duration, 1.70158f, 0f) < share) low = mid;
                else high = mid;
            }

            return high;
        }
    }
}

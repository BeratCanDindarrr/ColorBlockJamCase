using ColorBlockJam.Const;
using DG.Tweening;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // How the in-game boosters look on the board (yarm_stack's BoosterAnimationSettings). Registered through
    // GlobalSettings like BoardSettings.
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(BoosterAnimationSettings), fileName = nameof(BoosterAnimationSettings))]
    public class BoosterAnimationSettings : ScriptableObject
    {
        private const float MinPositive = 0.001f;

        [Header("Selection")]
        // Darkens the board while a booster waits for a target (a quad in front of the gameplay camera).
        public GameObject BoardDimPrefab;

        [Header("Hammer")]
        public HammerView HammerPrefab;
        // Handle end to striking face, in cells; the face lands on the tapped cell HammerHeight cells above the floor.
        public float HammerSize = 1.8f;
        public float HammerHeight = 1f;
        // Comes out upright on the handle's side of the block (the right, with the mirrored art) and, scaling up
        // (OutBack), winds up: leans HammerRaiseAngle degrees back from the hit pose and drifts HammerTravel cells away.
        // Then it leans over onto the block, hits, and shrinks away.
        public float HammerAppearDuration = 0.25f;
        public float HammerTravel = 0.5f;
        public float HammerRaiseAngle = 80f;
        public float HammerStrikeDuration = 0.12f;
        public float HammerFadeDuration = 0.15f;
        // The hit block squashes to this scale, then breaks apart as it springs back (OutBack).
        public float HammerBlockSquash = 0.8f;
        public float HammerBlockSquashDuration = 0.07f;
        public float HammerBlockReboundDuration = 0.2f;

        [Header("Block hit")]
        // The block jolts by this much (after a Rocket broke a piece off; a plain BlockDestroyed also uses it).
        public float DestroyPunch = 0.15f;
        public float DestroyPunchDuration = 0.25f;

        [Header("Vacuum")]
        // The block swells to this scale, then shrinks to nothing while sinking this many cells into the floor.
        public float VacuumGrowScale = 1.15f;
        public float VacuumGrowDuration = 0.15f;
        public float VacuumShrinkDuration = 0.35f;
        public float VacuumSinkDepth = 0.4f;

        [Header("Rocket")]
        public RocketProjectileView RocketPrefab;
        // One smooth flight: from under the HUD button into a half circle over the target (left, top, right; radius
        // in cells, RocketHeight cells above the floor), then a dive onto the block (RocketImpactHeight) shrinking to
        // RocketEndScale.
        public float RocketFlightDuration = 0.85f;
        // Gently out of the button, faster into the target.
        public Ease RocketEase = Ease.InSine;
        public float RocketOrbitRadius = 1.8f;
        public float RocketEndScale = 0.4f;
        // Seconds after leaving the button in which the rocket grows from nothing to full size.
        public float RocketGrowDuration = 0.25f;
        public float RocketHeight = 1.5f;
        public float RocketImpactHeight = 0.9f;
        // The piece breaks as soon as the diving rocket is this close (cells, seen from above) to its centre.
        public float RocketTouchRadius = 0.5f;

        private void OnValidate()
        {
            HammerSize = Mathf.Max(MinPositive, HammerSize);
            HammerHeight = Mathf.Max(0f, HammerHeight);
            HammerAppearDuration = Mathf.Max(0f, HammerAppearDuration);
            HammerTravel = Mathf.Max(0f, HammerTravel);
            HammerBlockSquash = Mathf.Clamp(HammerBlockSquash, 0.1f, 1f);
            HammerBlockSquashDuration = Mathf.Max(MinPositive, HammerBlockSquashDuration);
            HammerBlockReboundDuration = Mathf.Max(MinPositive, HammerBlockReboundDuration);
            HammerStrikeDuration = Mathf.Max(MinPositive, HammerStrikeDuration);
            HammerFadeDuration = Mathf.Max(0f, HammerFadeDuration);
            DestroyPunch = Mathf.Max(0f, DestroyPunch);
            DestroyPunchDuration = Mathf.Max(MinPositive, DestroyPunchDuration);
            VacuumGrowScale = Mathf.Max(1f, VacuumGrowScale);
            VacuumGrowDuration = Mathf.Max(0f, VacuumGrowDuration);
            VacuumShrinkDuration = Mathf.Max(MinPositive, VacuumShrinkDuration);
            VacuumSinkDepth = Mathf.Max(0f, VacuumSinkDepth);
            RocketFlightDuration = Mathf.Max(MinPositive, RocketFlightDuration);
            RocketOrbitRadius = Mathf.Max(0f, RocketOrbitRadius);
            RocketEndScale = Mathf.Max(0f, RocketEndScale);
            RocketGrowDuration = Mathf.Max(0f, RocketGrowDuration);
            RocketHeight = Mathf.Max(0f, RocketHeight);
            RocketImpactHeight = Mathf.Max(0f, RocketImpactHeight);
            RocketTouchRadius = Mathf.Max(0f, RocketTouchRadius);
        }
    }
}

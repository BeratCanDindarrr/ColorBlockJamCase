using ColorBlockJam.Core.Level;
using System;
using ColorBlockJam.Const;
using ColorBlockJam.Pool;
using ColorBlockJam.SandBox.Board;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(BoardSettings), fileName = nameof(BoardSettings))]
    public class BoardSettings : ScriptableObject
    {
        // Floor for values that must stay above zero; BoardRules does not validate and 0 would stall exits.
        private const float MinPositive = 0.001f;
        // Core pools throw when initialSize > maxSize; mirror that ceiling here.
        private const int MaxPoolSize = MemoryPoolBase<object>.DefaultMaxSize;

        [Serializable]
        public struct ColorEntry
        {
            public BlockColor Key;
            public Color Value;
        }

        public float CellSize = 1f;
        public float PickPlaneHeight = 0.5f;
        public float LiftHeight = 0.4f;
        public float FollowSharpness = 20f;
        public float LiftDuration = 0.1f;
        public float DropDuration = 0.15f;
        // The held block leans toward where it moves: degrees per board unit per second, capped at MaxTilt.
        public float TiltPerSpeed = 3f;
        public float MaxTilt = 12f;
        public float TiltSharpness = 12f;

        public float AssistRange = 0.3f;
        public bool ClockWaitsForFirstMove = true;
        public float PushThreshold = 0.35f;
        // How long an exiting block slides through its gate, and its easing.
        public float ExitSeconds = 0.5f;
        public DG.Tweening.Ease ExitEase = DG.Tweening.Ease.InSine;
        // Time to settle onto the cell in front of the gate before the slide starts.
        public float ExitAlignDuration = 0.08f;

        public float ShatterDuration = 0.4f;
        public float ShatterForce = 3f;
        // Random sideways/upward speed added to each shattering piece, world units per second.
        public float ShatterSpread = 1.5f;
        // An exiting block's piece is cut off at the gate and bursts into this many small cubes of its colour.
        public ExitBurstView ExitBurstPrefab;
        public int ExitBurstCubesPerPiece = 4;
        // Outward and random speed of those cubes, world units per second.
        public float ExitBurstSpeed = 3f;
        public float ExitBurstSpread = 1.5f;
        // Upward speed the cubes start with, and how far in (cells) from the wall's outer face a piece is cut off.
        public float ExitBurstLift = 2.5f;
        public float ExitClipInset = 0.7f;

        public ColorEntry[] Colors =
        {
            new ColorEntry { Key = BlockColor.Red, Value = new Color(0.90f, 0.22f, 0.21f) },
            new ColorEntry { Key = BlockColor.Blue, Value = new Color(0.20f, 0.45f, 0.90f) },
            new ColorEntry { Key = BlockColor.Green, Value = new Color(0.30f, 0.75f, 0.30f) },
            new ColorEntry { Key = BlockColor.Yellow, Value = new Color(0.98f, 0.82f, 0.20f) },
            new ColorEntry { Key = BlockColor.Purple, Value = new Color(0.58f, 0.32f, 0.80f) },
            new ColorEntry { Key = BlockColor.Orange, Value = new Color(0.98f, 0.55f, 0.15f) },
            new ColorEntry { Key = BlockColor.Pink, Value = new Color(0.96f, 0.45f, 0.70f) },
            new ColorEntry { Key = BlockColor.Cyan, Value = new Color(0.25f, 0.82f, 0.88f) },
        };

        // A frozen block is tinted this far (0-1) toward IceColor; its count punches by IcePunch when a block leaves.
        public Color IceColor = new Color(0.80f, 0.93f, 1f);
        public float IceTint = 0.65f;
        public float IcePunch = 0.3f;
        public float IcePunchDuration = 0.25f;
        // Laid over a frozen block's pieces (multiplied by the ice tint); none = tint only.
        public Texture2D IceTexture;
        public Color ArrowColor = Color.white;

        public Color FloorColor = new Color(0.82f, 0.86f, 0.92f);
        // The thin lines between floor tiles.
        public Color FloorGapColor = new Color(0.49f, 0.55f, 0.65f);
        public Color WallColor = new Color(0.23f, 0.26f, 0.38f);
        // Removed cells are filled with solid wall (each along its longer straight run) instead of walls round them.
        public bool FillHolesSolid;
        public Color BackgroundColor = new Color(0.81f, 0.87f, 0.97f);
        // The white arrow on each gate pointing out of the board, in cells.
        public float GateArrowSize = 0.45f;

        public FloorTileView FloorTilePrefab;
        public WallPieceView WallPiecePrefab;
        public GateView GatePrefab;
        public BlockView BlockPrefab;
        public BlockPieceView BlockPiecePrefab;
        public int BlockPoolSize = 8;
        public int PiecePoolSize = 64;

        public BoardRules ToRules() => new BoardRules(
            Mathf.Max(0f, AssistRange),
            ClockWaitsForFirstMove,
            Mathf.Max(0f, PushThreshold));

        public Color GetColor(BlockColor color)
        {
            foreach (var entry in Colors)
            {
                if (entry.Key == color) return entry.Value;
            }

            Debug.LogError($"{nameof(BoardSettings)}: no colour for {color}", this);
            return Color.magenta;
        }

        private void OnValidate()
        {
            AssistRange = Mathf.Max(0f, AssistRange);
            PushThreshold = Mathf.Max(0f, PushThreshold);
            ExitSeconds = Mathf.Max(MinPositive, ExitSeconds);
            ExitAlignDuration = Mathf.Max(0f, ExitAlignDuration);
            CellSize = Mathf.Max(MinPositive, CellSize);
            GateArrowSize = Mathf.Max(0f, GateArrowSize);
            ExitBurstCubesPerPiece = Mathf.Max(0, ExitBurstCubesPerPiece);
            ExitBurstSpeed = Mathf.Max(0f, ExitBurstSpeed);
            ExitBurstSpread = Mathf.Max(0f, ExitBurstSpread);
            ExitBurstLift = Mathf.Max(0f, ExitBurstLift);
            ExitClipInset = Mathf.Max(0f, ExitClipInset);
            FollowSharpness = Mathf.Max(MinPositive, FollowSharpness);
            LiftDuration = Mathf.Max(0f, LiftDuration);
            DropDuration = Mathf.Max(0f, DropDuration);
            TiltPerSpeed = Mathf.Max(0f, TiltPerSpeed);
            MaxTilt = Mathf.Clamp(MaxTilt, 0f, 45f);
            TiltSharpness = Mathf.Max(MinPositive, TiltSharpness);
            ShatterDuration = Mathf.Max(MinPositive, ShatterDuration);
            ShatterForce = Mathf.Max(0f, ShatterForce);
            ShatterSpread = Mathf.Max(0f, ShatterSpread);
            IceTint = Mathf.Clamp01(IceTint);
            IcePunch = Mathf.Max(0f, IcePunch);
            IcePunchDuration = Mathf.Max(MinPositive, IcePunchDuration);
            BlockPoolSize = Mathf.Clamp(BlockPoolSize, 0, MaxPoolSize);
            PiecePoolSize = Mathf.Clamp(PiecePoolSize, 0, MaxPoolSize);
        }
    }
}

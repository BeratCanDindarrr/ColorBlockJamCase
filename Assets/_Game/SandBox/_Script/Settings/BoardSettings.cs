using ColorBlockJam.Core.Level;
using System;
using ColorBlockJam.Const;
using ColorBlockJam.SandBox.Board;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(BoardSettings), fileName = nameof(BoardSettings))]
    public class BoardSettings : ScriptableObject
    {
        // Floor for values that must stay above zero; BoardRules does not validate and 0 would stall exits.
        private const float MinPositive = 0.001f;

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

        public float AssistRange = 0.3f;
        public float MaxFrameDelta = 0.1f;
        public bool ClockWaitsForFirstMove = true;
        public float PushThreshold = 0.35f;
        public float ExitSeconds = 0.5f;

        public float ShatterDuration = 0.4f;
        public float ShatterForce = 3f;

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

        public Color FloorColor = new Color(0.82f, 0.86f, 0.92f);
        // World units kept free around the walled board when the camera frames it.
        public float CameraMargin = 0.5f;

        public FloorTileView FloorTilePrefab;
        public WallPieceView WallPiecePrefab;
        public GateView GatePrefab;
        public BlockView BlockPrefab;
        public BlockPieceView BlockPiecePrefab;
        public int BlockPoolSize = 8;
        public int PiecePoolSize = 64;

        public BoardRules ToRules() => new BoardRules(
            Mathf.Max(0f, AssistRange),
            Mathf.Max(MinPositive, MaxFrameDelta),
            ClockWaitsForFirstMove,
            Mathf.Max(0f, PushThreshold),
            Mathf.Max(MinPositive, ExitSeconds));

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
            MaxFrameDelta = Mathf.Max(MinPositive, MaxFrameDelta);
            PushThreshold = Mathf.Max(0f, PushThreshold);
            ExitSeconds = Mathf.Max(MinPositive, ExitSeconds);
            CellSize = Mathf.Max(MinPositive, CellSize);
            CameraMargin = Mathf.Max(0f, CameraMargin);
            BlockPoolSize = Mathf.Max(0, BlockPoolSize);
            PiecePoolSize = Mathf.Max(0, PiecePoolSize);
        }
    }
}

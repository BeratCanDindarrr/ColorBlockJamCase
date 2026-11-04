using System;
using System.Linq;
using ColorBlockJam.Const;
using UnityEngine;

namespace ColorBlockJam.Core.Settings
{
    // Gameplay camera framing. Manual: hand-tuned entries per board size (knit_Out's LevelCameraSettings), looking
    // straight down, CameraPos being the offset from the board centre. Otherwise the camera leans back by TiltDegrees
    // (the reference game's look) and is fitted to the fills below.
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(LevelCameraSettings), fileName = nameof(LevelCameraSettings))]
    public class LevelCameraSettings : ScriptableObject
    {
        [Tooltip("On: the camera uses the hand-tuned entries below. Off: it fits the board automatically.")]
        public bool UseManualCamera = true;

        // ColumnCount is how many cells wide the board is with its walls (cols + 1, or rows + 1 when taller).
        // Keep the entries sorted by ColumnCount.
        public LevelCameraData[] LevelCameraDatas;

        [Header("Fitted Camera")]
        [Tooltip("How far the camera leans back from looking straight down, degrees.")]
        [Range(0f, 45f)] public float TiltDegrees = 15f;
        [Tooltip("Share of the screen width the board (with its walls) fills at its widest.")]
        [Range(0.1f, 1f)] public float BoardWidthFill = 0.9f;
        [Tooltip("The board never fills more than this share of the screen height.")]
        [Range(0.1f, 1f)] public float BoardMaxHeightFill = 0.62f;
        [Tooltip("Where the board's middle sits on the screen, 0 = bottom, 1 = top.")]
        [Range(0f, 1f)] public float BoardViewportCenterY = 0.47f;

        public bool HasManualData => UseManualCamera && LevelCameraDatas != null && LevelCameraDatas.Length > 0;

        // The first entry wide enough for the board, or the widest one.
        public LevelCameraData GetCameraPosByColumnCount(int columnCount)
        {
            return LevelCameraDatas.FirstOrDefault(x => x.ColumnCount >= columnCount) ?? LevelCameraDatas.Last();
        }
    }

    [Serializable]
    public class LevelCameraData
    {
        public int ColumnCount;
        public Vector3 CameraPos;
        public float CameraFieldOfView = 60f;
        // Screens of other shapes than the reference (9:16): blends from CameraPos to the entry's values.
        public CameraRadioAspectSettings[] CameraRadioAspectSettings;

        public CameraRadioAspectSettings GetAspectSettings(float aspect)
        {
            return CameraRadioAspectSettings?.FirstOrDefault(item => item.DoesResideIn(aspect));
        }
    }

    [Serializable]
    public class CameraRadioAspectSettings
    {
        public float FromAspect;
        public float ToAspect;
        public Vector3 ToCameraPos;
        public float ToCameraFieldOfView = 60f;

        public bool DoesResideIn(float aspect)
        {
            var min = Mathf.Min(FromAspect, ToAspect);
            var max = Mathf.Max(FromAspect, ToAspect);
            return aspect >= min && aspect <= max;
        }
    }
}

using System;
using System.Linq;
using ColorBlockJam.Const;
using UnityEngine;

namespace ColorBlockJam.Core.Settings
{
    // Hand-tuned gameplay camera per board size (knit_Out's LevelCameraSettings). The camera always looks straight
    // down; CameraPos is its offset from the board centre.
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(LevelCameraSettings), fileName = nameof(LevelCameraSettings))]
    public class LevelCameraSettings : ScriptableObject
    {
        [Tooltip("On: the camera uses the hand-tuned entries below. Off: it fits the board automatically.")]
        public bool UseManualCamera = true;

        // ColumnCount is how many cells wide the board is with its walls (cols + 1, or rows + 1 when taller).
        // Keep the entries sorted by ColumnCount.
        public LevelCameraData[] LevelCameraDatas;

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

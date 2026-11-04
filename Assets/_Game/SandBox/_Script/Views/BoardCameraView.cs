using ColorBlockJam.Core.Settings;
using UnityEngine;
using VContainer;

namespace ColorBlockJam.SandBox
{
    public class BoardCameraView : ColorBlockJam.Views.BaseView
    {
        private const float Half = 0.5f;
        private const float CellCountTolerance = 0.001f;
        // Row 0 (the +z side) at the top of the screen.
        private static readonly Quaternion TopDown = Quaternion.LookRotation(Vector3.down, Vector3.forward);

        [SerializeField] private Camera gamePlayCamera;
        [SerializeField] private Camera selectedItemCamera;

        private LevelCameraSettings _levelCameraSettings;

        public Camera GamePlayCamera => gamePlayCamera;
        public Camera SelectedItemCamera => selectedItemCamera;

        [Inject]
        public void Construct(LevelCameraSettings levelCameraSettings)
        {
            _levelCameraSettings = levelCameraSettings;
        }

        // Looks straight down at the board: from the hand-tuned LevelCameraSettings entry for its size when those
        // are on, otherwise pulled up until the board (half extents along x/z plus margin) fits.
        public void Frame(Vector3 boardCenter, Vector2 halfExtents, float margin, float cellSize)
        {
            gamePlayCamera.transform.rotation = TopDown;
            if (_levelCameraSettings != null && _levelCameraSettings.HasManualData)
                ApplyManual(boardCenter, GetFramedCellCount(halfExtents, cellSize));
            else
                Fit(boardCenter, halfExtents, margin);

            // The selected-item camera rides on this one and must see the board through the same lens.
            if (selectedItemCamera != null) selectedItemCamera.fieldOfView = gamePlayCamera.fieldOfView;
        }

        // Board width in cells with its walls, or its height when that is larger.
        private static int GetFramedCellCount(Vector2 halfExtents, float cellSize) =>
            Mathf.CeilToInt(Mathf.Max(halfExtents.x, halfExtents.y) * 2f / cellSize - CellCountTolerance);

        // The entry for the board's size, blended toward its aspect entry on other screen shapes.
        private void ApplyManual(Vector3 boardCenter, int cellCount)
        {
            var data = _levelCameraSettings.GetCameraPosByColumnCount(cellCount);
            var aspect = gamePlayCamera.aspect;
            var aspectSettings = data.GetAspectSettings(aspect);
            var position = data.CameraPos;
            var fieldOfView = data.CameraFieldOfView;
            if (aspectSettings != null)
            {
                var ratio = Mathf.InverseLerp(aspectSettings.FromAspect, aspectSettings.ToAspect, aspect);
                position = Vector3.Lerp(data.CameraPos, aspectSettings.ToCameraPos, ratio);
                fieldOfView = Mathf.Lerp(data.CameraFieldOfView, aspectSettings.ToCameraFieldOfView, ratio);
            }

            gamePlayCamera.transform.position = boardCenter + position;
            gamePlayCamera.fieldOfView = fieldOfView;
        }

        private void Fit(Vector3 boardCenter, Vector2 halfExtents, float margin)
        {
            var halfFovTan = Mathf.Tan(gamePlayCamera.fieldOfView * Half * Mathf.Deg2Rad);
            var distanceForWidth = (halfExtents.x + margin) / (halfFovTan * gamePlayCamera.aspect);
            var distanceForDepth = (halfExtents.y + margin) / halfFovTan;
            gamePlayCamera.transform.position = boardCenter + Vector3.up * Mathf.Max(distanceForWidth, distanceForDepth);
        }
    }
}

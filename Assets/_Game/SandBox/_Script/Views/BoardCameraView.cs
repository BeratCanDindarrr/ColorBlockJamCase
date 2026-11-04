using ColorBlockJam.Core.Settings;
using UnityEngine;
using VContainer;

namespace ColorBlockJam.SandBox
{
    public class BoardCameraView : ColorBlockJam.Views.BaseView
    {
        private const float Half = 0.5f;
        private const float CellCountTolerance = 0.001f;
        private const float StraightDown = 90f;
        // The fit measures the board's outline at floor level and at about the top of the walls and blocks.
        private const float BoardTopInCells = 0.6f;
        // Search range of the camera's distance from the board centre, in cells.
        private const float MinDistanceInCells = 1f;
        private const float MaxDistanceInCells = 200f;
        private const int DistanceSearchSteps = 30;
        private const int CenterSteps = 4;
        // Row 0 (the +z side) at the top of the screen.
        private static readonly Quaternion TopDown = Quaternion.LookRotation(Vector3.down, Vector3.forward);

        private readonly Vector3[] _corners = new Vector3[8];

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

        // From the hand-tuned LevelCameraSettings entry for the board's size when those are on (straight down),
        // otherwise leaned back and fitted to the settings' fills.
        public void Frame(Vector3 boardCenter, Vector2 halfExtents, float cellSize)
        {
            if (_levelCameraSettings != null && _levelCameraSettings.HasManualData)
            {
                gamePlayCamera.transform.rotation = TopDown;
                ApplyManual(boardCenter, GetFramedCellCount(halfExtents, cellSize));
            }
            else
            {
                Fit(boardCenter, halfExtents, cellSize);
            }

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

        // The nearest distance at which the board fills BoardWidthFill of the width (or BoardMaxHeightFill of the
        // height, whichever comes first), then slid along the camera's up until its middle is at BoardViewportCenterY.
        private void Fit(Vector3 boardCenter, Vector2 halfExtents, float cellSize)
        {
            var settings = _levelCameraSettings;
            var view = gamePlayCamera.transform;
            view.rotation = Quaternion.Euler(StraightDown - (settings != null ? settings.TiltDegrees : 0f), 0f, 0f);
            SetCorners(boardCenter, halfExtents, cellSize * BoardTopInCells);

            var widthFill = settings != null ? settings.BoardWidthFill : 1f;
            var heightFill = settings != null ? settings.BoardMaxHeightFill : 1f;
            float near = cellSize * MinDistanceInCells, far = cellSize * MaxDistanceInCells;
            for (var i = 0; i < DistanceSearchSteps; i++)
            {
                var distance = (near + far) * Half;
                view.position = boardCenter - view.forward * distance;
                var rect = ViewportRect();
                if (rect.width > widthFill || rect.height > heightFill) near = distance;
                else far = distance;
            }

            view.position = boardCenter - view.forward * far;
            var centerY = settings != null ? settings.BoardViewportCenterY : Half;
            var viewportHeight = 2f * far * Mathf.Tan(gamePlayCamera.fieldOfView * Half * Mathf.Deg2Rad);
            for (var i = 0; i < CenterSteps; i++)
                view.position += view.up * (ViewportRect().center.y - centerY) * viewportHeight;
        }

        private void SetCorners(Vector3 center, Vector2 halfExtents, float top)
        {
            for (var i = 0; i < _corners.Length; i++)
            {
                var x = (i & 1) == 0 ? -halfExtents.x : halfExtents.x;
                var z = (i & 2) == 0 ? -halfExtents.y : halfExtents.y;
                var y = (i & 4) == 0 ? 0f : top;
                _corners[i] = center + new Vector3(x, y, z);
            }
        }

        private Rect ViewportRect()
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var corner in _corners)
            {
                var point = (Vector2)gamePlayCamera.WorldToViewportPoint(corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        // The gameplay camera clears to a plain colour (the selected-item camera draws over it without clearing).
        public void SetBackground(Color color)
        {
            gamePlayCamera.clearFlags = CameraClearFlags.SolidColor;
            gamePlayCamera.backgroundColor = color;
        }
    }
}

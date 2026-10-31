using UnityEngine;

namespace ColorBlockJam.SandBox
{
    public class BoardCameraView : MonoBehaviour
    {
        private const float Half = 0.5f;

        [SerializeField] private Camera gamePlayCamera;
        [SerializeField] private Camera selectedItemCamera;

        public Camera GamePlayCamera => gamePlayCamera;
        public Camera SelectedItemCamera => selectedItemCamera;

        // Keeps the scene's pitch and FOV and pulls the camera back along its view direction until the
        // board (half extents along x/z plus margin) fits the screen. The selected-item camera is a child and follows.
        public void Frame(Vector3 boardCenter, Vector2 halfExtents, float margin)
        {
            var cameraTransform = gamePlayCamera.transform;
            var tilt = Vector3.Angle(-cameraTransform.forward, Vector3.up) * Mathf.Deg2Rad;
            var halfFovTan = Mathf.Tan(gamePlayCamera.fieldOfView * Half * Mathf.Deg2Rad);
            var distanceForWidth = (halfExtents.x + margin) / (halfFovTan * gamePlayCamera.aspect);
            var distanceForDepth = (halfExtents.y + margin) * Mathf.Cos(tilt) / halfFovTan;

            cameraTransform.position = boardCenter - cameraTransform.forward * Mathf.Max(distanceForWidth, distanceForDepth);
        }
    }
}

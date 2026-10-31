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

        // Looks straight down at the board (row 0, the +z side, at the top of the screen) and pulls the camera up
        // until the board (half extents along x/z plus margin) fits. The selected-item camera is a child and follows.
        public void Frame(Vector3 boardCenter, Vector2 halfExtents, float margin)
        {
            var cameraTransform = gamePlayCamera.transform;
            cameraTransform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            var halfFovTan = Mathf.Tan(gamePlayCamera.fieldOfView * Half * Mathf.Deg2Rad);
            var distanceForWidth = (halfExtents.x + margin) / (halfFovTan * gamePlayCamera.aspect);
            var distanceForDepth = (halfExtents.y + margin) / halfFovTan;

            cameraTransform.position = boardCenter + Vector3.up * Mathf.Max(distanceForWidth, distanceForDepth);
        }
    }
}

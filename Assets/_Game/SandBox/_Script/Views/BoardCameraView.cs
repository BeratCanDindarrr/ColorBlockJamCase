using UnityEngine;

namespace ColorBlockJam.SandBox
{
    public class BoardCameraView : MonoBehaviour
    {
        [SerializeField] private Camera gamePlayCamera;
        [SerializeField] private Camera selectedItemCamera;

        public Camera GamePlayCamera => gamePlayCamera;
        public Camera SelectedItemCamera => selectedItemCamera;
    }
}

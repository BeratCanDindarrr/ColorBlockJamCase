using System.Collections;
using System.Collections.Generic;
using ColorBlockJam.Views;
using UnityEngine;

namespace ColorBlockJam
{
    public class MainCameraView : BaseView
    {
        [SerializeField] private Camera mainCamera;
        
        public Camera MainCamera => mainCamera;
    }
}

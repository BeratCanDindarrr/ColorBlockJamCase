using System;
using System.Collections;
using System.Collections.Generic;
using ColorBlockJam.Views;
using UnityEngine;
using VContainer;

namespace ColorBlockJam
{
    [RequireComponent(typeof(Canvas))]
    public class CanvasCameraSetter : BaseView
    {
        [Inject]
        public void Construct(MainCameraView mainCameraView)
        {
            GetComponent<Canvas>().worldCamera = mainCameraView.MainCamera;
        }
    }
}

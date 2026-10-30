using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ColorBlockJam
{
    public class ViewLifeTimeScope : MonoBehaviour
    {
        [SerializeField] private MainCameraView mainCameraView;
        public void Init(IContainerBuilder  builder)
        {
            builder.RegisterComponent(mainCameraView).AsSelf();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using ColorBlockJam.Core.Controller;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ColorBlockJam
{
    public class ControllerLifeTimeScope : MonoBehaviour
    {
        public void Init(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<ApplicationController>(Lifetime.Singleton);
            builder.Register<ScreenController>(Lifetime.Singleton);
        }
    }
}

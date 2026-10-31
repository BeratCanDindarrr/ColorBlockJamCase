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
            builder.RegisterEntryPoint<ApplicationController>(Lifetime.Singleton).AsSelf();
            builder.Register<ScreenController>(Lifetime.Singleton);
            builder.Register<SceneLoaderController>(Lifetime.Singleton);
            builder.Register<PlayerController>(Lifetime.Singleton);
            builder.Register<LifeController>(Lifetime.Singleton);
            builder.Register<CurrencyController>(Lifetime.Singleton);
            builder.Register<SettingsController>(Lifetime.Singleton);
            builder.RegisterEntryPoint<InputController>(Lifetime.Singleton).AsSelf();
        }
    }
}

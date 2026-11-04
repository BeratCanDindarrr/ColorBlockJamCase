using System.Collections;
using System.Collections.Generic;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Core.Controller.Mock;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ColorBlockJam
{
    public class ControllerLifeTimeScope : MonoBehaviour
    {
        // Optional dev tool in the scene; injected when there is one.
        [SerializeField] private MockMessageController mockMessageController;

        public void Init(IContainerBuilder builder)
        {
            if (mockMessageController != null) builder.RegisterComponent(mockMessageController);
            builder.RegisterEntryPoint<ApplicationController>(Lifetime.Singleton).AsSelf();
            builder.Register<ScreenController>(Lifetime.Singleton);
            builder.Register<SceneLoaderController>(Lifetime.Singleton);
            builder.Register<PlayerController>(Lifetime.Singleton);
            builder.Register<LifeController>(Lifetime.Singleton);
            builder.Register<CurrencyController>(Lifetime.Singleton);
            builder.Register<SettingsController>(Lifetime.Singleton);
            builder.RegisterEntryPoint<BoosterTutorialController>(Lifetime.Singleton);
            builder.RegisterEntryPoint<DragTutorialController>(Lifetime.Singleton);
            builder.RegisterEntryPoint<InputController>(Lifetime.Singleton).AsSelf();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using ColorBlockJam.Core.Service;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ColorBlockJam
{
    public class ServiceLifeTimeScope : MonoBehaviour
    {
        public void Init(IContainerBuilder  builder)
        {
            builder.Register<TaskService>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<PopupService>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<AudioService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<StorageService>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<TimerService>(Lifetime.Singleton).AsSelf();
        }
    }
}

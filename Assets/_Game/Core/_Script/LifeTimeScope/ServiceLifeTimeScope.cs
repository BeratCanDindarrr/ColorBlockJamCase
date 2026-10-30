using System.Collections;
using System.Collections.Generic;
using ColorBlockJam.Core.Service;
using UnityEngine;
using VContainer;

namespace ColorBlockJam
{
    public class ServiceLifeTimeScope : MonoBehaviour
    {
        public void Init(IContainerBuilder  builder)
        {
            builder.Register<TaskService>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<PopupService>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
        }
    }
}

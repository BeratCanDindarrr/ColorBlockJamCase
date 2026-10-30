using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ColorBlockJam.Core
{
    public class GameLifeTimeScope : LifetimeScope
    {
        [SerializeField] private ControllerLifeTimeScope controllerLifeTimeScope;
        [SerializeField] private ServiceLifeTimeScope serviceLifeTimeScope;
        [SerializeField] private ViewLifeTimeScope viewLifeTimeScope;
        [SerializeField] private GlobalSettings globalSettings;
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            globalSettings.Init(builder);
            controllerLifeTimeScope.Init(builder);
            serviceLifeTimeScope.Init(builder);
            viewLifeTimeScope.Init(builder);
        }
    }
}
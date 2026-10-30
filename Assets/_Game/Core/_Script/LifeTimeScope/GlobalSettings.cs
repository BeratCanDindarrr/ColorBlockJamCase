using System.Collections;
using System.Collections.Generic;
using ColorBlockJam.Const;
using UnityEngine;
using VContainer;

namespace ColorBlockJam.Core
{
    [CreateAssetMenu(menuName = MenuConst.Core + nameof(GlobalSettings), fileName = nameof(GlobalSettings))]
    public class GlobalSettings : ScriptableObject
    {
        public ScriptableObject[] Settings;

        public void Init(IContainerBuilder builder)
        {
            foreach (var setting in Settings)
            {
                builder.RegisterInstance(setting).AsSelf();
            }
        }
    }
}

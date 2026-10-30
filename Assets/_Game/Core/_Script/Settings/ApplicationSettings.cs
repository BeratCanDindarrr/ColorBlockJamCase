using ColorBlockJam.Const;
using UnityEngine;

namespace ColorBlockJam.Core.Settings
{
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(ApplicationSettings), fileName = nameof(ApplicationSettings))]
    public class ApplicationSettings : ScriptableObject
    {
        public string ApplicationName;
    }
}
using ColorBlockJam.Const;
using UnityEngine;

namespace ColorBlockJam.Core.Settings
{
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(PopupSettings), fileName = nameof(PopupSettings))]
    public class PopupSettings : ScriptableObject
    {
        public GameObject[] PopupPrefabs;
    }
}
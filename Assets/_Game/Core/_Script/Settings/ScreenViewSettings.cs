using ColorBlockJam.Const;
using ColorBlockJam.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ColorBlockJam.Core.Settings
{
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(ScreenViewSettings),fileName = nameof(ScreenViewSettings))]
    public class ScreenViewSettings : ScriptableObject
    {
        public LoadingScreenView loadingScreenView;
        public MainScreenView mainScreenView;
        public GamePlayScreenView gamePlayScreenView;
    }
}
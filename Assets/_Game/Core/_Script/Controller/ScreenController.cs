using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.Views;
using VContainer;
using VContainer.Unity;

namespace ColorBlockJam.Core.Controller
{
    public class ScreenController
    {
        public LoadingScreenView LoadingScreenView;
        public MainScreenView MainScreenView;

        private ScreenType _screenType = ScreenType.None;
        
        private ScreenViewSettings _screenViewSettings;
        private IObjectResolver _objectResolver;
        public ScreenController(ScreenViewSettings screenViewSettings
            , IObjectResolver objectResolver)
        {
            _screenViewSettings = screenViewSettings;
            _objectResolver = objectResolver;
        }

        public void Init()
        {
            LoadingScreenView = _objectResolver.Instantiate(_screenViewSettings.loadingScreenView);
            MainScreenView = _objectResolver.Instantiate(_screenViewSettings.mainScreenView);
            LoadingScreenView.Init();
            MainScreenView.Init();
            LoadingScreenView.Hide();
            MainScreenView.Hide();
        }

        public void SetScreen(ScreenType screenType)
        {
            _screenType = screenType;
            switch (screenType)
            {
                case ScreenType.MainScreen:
                    LoadingScreenView.Hide();
                    MainScreenView.Show();
                    break;
                case ScreenType.LoadingScreen:
                    MainScreenView.Hide();
                    LoadingScreenView.Show();
                    LoadingScreenView.FakeLoading().Forget();
                    break;
            }
        }
    }
}
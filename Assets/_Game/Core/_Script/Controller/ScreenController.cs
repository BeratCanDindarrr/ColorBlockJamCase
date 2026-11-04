using System;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ColorBlockJam.Core.Controller
{
    public class ScreenController
    {
        public LoadingScreenView LoadingScreenView;
        public MainScreenView MainScreenView;
        public GamePlayScreenView GamePlayScreenView;

        private ScreenType _screenType = ScreenType.None;
        public ScreenType ScreenStatus => _screenType;
        
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
            GamePlayScreenView = _objectResolver.Instantiate(_screenViewSettings.gamePlayScreenView);
            LoadingScreenView.Init();
            MainScreenView.Init();
            GamePlayScreenView.Init();
            LoadingScreenView.Hide();
            MainScreenView.Hide();
            GamePlayScreenView.Hide();
            // Development builds and the editor only, like knit_Out's DevelopmentBuild check.
            if (_screenViewSettings.showAdminPanel && Debug.isDebugBuild && _screenViewSettings.adminPanelButtonView != null)
                _objectResolver.Instantiate(_screenViewSettings.adminPanelButtonView);
        }

        // After a screen is shown (e.g. the main menu once loading is over).
        public event Action<ScreenType> ScreenChanged;

        public void SetScreen(ScreenType screenType)
        {
            _screenType = screenType;
            switch (screenType)
            {
                case ScreenType.MainScreen:
                    LoadingScreenView.Hide();
                    GamePlayScreenView.Hide();
                    MainScreenView.Show();
                    break;
                case ScreenType.LoadingScreen:
                    MainScreenView.Hide();
                    GamePlayScreenView.Hide();
                    LoadingScreenView.Show();
                    LoadingScreenView.FakeLoading().Forget();
                    break;
                case ScreenType.GamePlayScreen:
                    LoadingScreenView.Hide();
                    MainScreenView.Hide();
                    GamePlayScreenView.Show();
                    break;
            }

            ScreenChanged?.Invoke(screenType);
        }
    }
}
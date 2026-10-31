namespace ColorBlockJam.Const
{
    public struct GlobalConst
    {
        public struct PopupName
        {
            public const string TestPopup = "TestPopup";
            public const string OutOfMovesPopup = "OutOfMovesPopup";
            public const string QuitLevelPopup = "QuitLevelPopup";
            public const string LevelFailedPopup = "LevelFailedPopup";
            public const string MoreLivesPopup = "MoreLivesPopup";
            public const string MenuSettingsPopup = "MenuSettingsPopup";
            public const string InGameSettingsPopup = "InGameSettingsPopup";
        }

        public struct SceneName
        {
            public const string GamePlay = "GamePlayScene";
        }

        // PlayerPrefs keys of StorageService.
        public struct Storage
        {
            public const string Version = "ColorBlockJam_STORAGE_VERSION";
            public const string Local = "ColorBlockJam_STORAGE_LOCAL";
            // File names under persistentDataPath (PlayerController).
            public const string UserIdFile = "ColorBlockJam_USER_DB_ID";
            public const string FirstOpenTimeFile = "ColorBlockJam_FIRST_OPEN_TIME";
        }
    }
}
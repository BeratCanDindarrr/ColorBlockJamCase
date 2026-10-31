namespace ColorBlockJam.Enums
{
    public enum TaskType
    {
        FirstLayer = 0,
        SecondLayer = 1,
    }
    
    public enum ScreenType
    {
        LoadingScreen = 0,
        MainScreen = 1,
        GamePlayScreen = 2,
        None = 99,
    }

    public enum CurrencyType
    {
        Coin = 0,
        Booster1 = 1,
        Booster2 = 2,
        Booster3 = 3,
        // Limitless life, value in seconds.
        Time = 4,
        RemoveAds = 5
    }

    public enum CurrencyUpdateType
    {
        Gain = 0,
        Consume = 1
    }

    public enum CurrencySpendResult
    {
        Success,
        NotEnough
    }
}

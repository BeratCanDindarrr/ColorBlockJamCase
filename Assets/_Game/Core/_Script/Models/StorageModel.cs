using System;

namespace ColorBlockJam.Core.Model
{
    // Everything saved to disk. Add one [Serializable] model field per saved feature.
    [Serializable]
    public class StorageModel
    {
        public PlayerModel playerModel;
        public LevelModel levelModel;
        public CurrencyModel currencyModel;
        public LifeModel lifeModel;
        public SettingsModel settingsModel;
    }

    // What StorageService writes to PlayerPrefs: the serialized StorageModel and its save count.
    [Serializable]
    public class Storage
    {
        public int version;
        public string storageData;
    }
}

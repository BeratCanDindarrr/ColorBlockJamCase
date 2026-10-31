using System;
using ColorBlockJam.Const;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace ColorBlockJam.Core.Service
{
    // Keeps the whole save (StorageModel) as one JSON string in PlayerPrefs, with a version that grows on every save.
    public class StorageService
    {
        public StorageModel StorageModel { get; private set; }
        public bool IsAvailable { get; private set; }

        private Storage _storage;

        public void Init()
        {
            if (IsAvailable) return;
            IsAvailable = true;

            StorageModel = new StorageModel();
            _storage = new Storage
            {
                storageData = PlayerPrefs.GetString(GlobalConst.Storage.Local, ""),
                version = PlayerPrefs.GetInt(GlobalConst.Storage.Version, 0)
            };

            if (_storage.storageData == "" || _storage.version <= 0) return;

            try
            {
                var settings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
                JsonConvert.PopulateObject(_storage.storageData, StorageModel, settings);
            }
            catch (Exception error)
            {
                // Release-visible: a corrupt save silently resets the player's progress.
                Debug.LogError($"{nameof(StorageService)}: storage json can't be parsed! {error.Message}");
            }
        }

        public void SaveStorage()
        {
            if (!IsAvailable)
            {
                Debug.LogError($"{nameof(StorageService)}: storage not loaded yet!");
                return;
            }

            var modelSerialized = JsonConvert.SerializeObject(StorageModel, Formatting.Indented,
                new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

            if (_storage.storageData == modelSerialized)
            {
                DebugExtension.Log("No changes to save!");
                return;
            }

            DebugExtension.Log("SaveStorage : " + modelSerialized);

            _storage.version++;
            _storage.storageData = modelSerialized;
            SaveLocalStorage();
        }

        private void SaveLocalStorage()
        {
            PlayerPrefs.SetInt(GlobalConst.Storage.Version, _storage.version);
            PlayerPrefs.SetString(GlobalConst.Storage.Local, _storage.storageData);
            PlayerPrefs.Save();
        }
    }
}

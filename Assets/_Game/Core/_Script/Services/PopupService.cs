using System.Collections.Generic;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.GameTask;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Views;
using UnityEngine;

namespace ColorBlockJam.Core.Service
{
    public class PopupService : IPopupService
    {
        #region Injection

        private PopupSettings _popupSettings;
        private ITaskService _taskService;

        public PopupService(PopupSettings popupSettings
        , ITaskService taskService)
        {
            _popupSettings  =  popupSettings;
            _taskService = taskService;
        }

        #endregion

        private Dictionary<string, GameObject> _popupPrefabs = new Dictionary<string, GameObject>();
        private int _openPopupCount;

        public bool HasOpenPopup => _openPopupCount > 0;

        public void Init()
        {
            foreach (var prefab in _popupSettings.PopupPrefabs)
            {
                _popupPrefabs.Add(prefab.name, prefab);
            }  
        }

        public void AddPopup(BasePopupParameters param)
        {
            var popupTask = new PopupGameTask();
            popupTask.Init(param,GetPopup(param), isOpen => _openPopupCount += isOpen ? 1 : -1);
            _taskService.AddTask(popupTask);
        }

        private GameObject GetPopup(BasePopupParameters parameters)
        {
            return _popupPrefabs.GetValueOrDefault(parameters.GetPopupName());
        }


    }
}
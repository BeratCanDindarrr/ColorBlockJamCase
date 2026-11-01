using System;
using System.Collections.Generic;
using System.Linq;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Core.Model;
using ColorBlockJam.Core.Service;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.GameTask;
using ColorBlockJam.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    // Lists the rewards it was given; Collect adds them to the player, saves, closes and flies each one from its
    // place in the list onto the main screen (CurrencyUpdateAnimationGameTask, coins as many).
    public class CollectPopup : BasePopup<CollectPopupParameters>
    {
        [SerializeField] private CollectRewardItemView itemPrefab;
        [SerializeField] private Transform itemParent;
        [SerializeField] private Button collectButton;
        [SerializeField] private Canvas canvas;

        private readonly List<CollectRewardItemView> _items = new List<CollectRewardItemView>();
        private CurrencyController _currencyController;
        private LifeController _lifeController;
        private StorageService _storageService;
        private CurrencySettings _currencySettings;
        private ITaskService _taskService;

        [Inject]
        public void Construct(CurrencyController currencyController
            , LifeController lifeController
            , StorageService storageService
            , CurrencySettings currencySettings
            , ITaskService taskService)
        {
            _currencyController = currencyController;
            _lifeController = lifeController;
            _storageService = storageService;
            _currencySettings = currencySettings;
            _taskService = taskService;
        }

        private CurrencyData[] Rewards => Parameters.Rewards ?? Array.Empty<CurrencyData>();

        public override void ShowPopup()
        {
            // shortcut: one item per reward, made with the popup and destroyed with it (a popup is rare, not pooled).
            foreach (var reward in Rewards)
            {
                var item = Instantiate(itemPrefab, itemParent);
                item.Set(reward, _currencySettings.IconOf(reward.currencyType));
                _items.Add(item);
            }

            collectButton.onClick.AddListener(Collect);
        }

        protected override void Dispose()
        {
            collectButton.onClick.RemoveListener(Collect);
        }

        private void Collect()
        {
            collectButton.interactable = false;
            var rewards = Rewards;
            foreach (var reward in rewards) _currencyController.AddCurrency(reward, false);
            _lifeController.SyncModel();
            _storageService.SaveStorage();

            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var starts = _items.Select(i => RectTransformUtility.WorldToScreenPoint(camera, i.IconPosition)).ToArray();
            var flyTask = new CurrencyUpdateAnimationGameTask();
            flyTask.Init(rewards, starts);
            ClosePopup();
            _taskService.AddTask(flyTask);
        }
    }
}

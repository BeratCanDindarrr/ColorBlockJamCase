using System;
using System.Collections.Generic;
using ColorBlockJam.Core.Controller;
using ColorBlockJam.Core.Service;
using ColorBlockJam.Interfaces;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    // Admin panel (dimmed background, frameless container, red close button, green and red buttons) with
    // the tabs listed down the left side and the picked tab's buttons on the right: the level (go to / win / lose),
    // currency and life. Each tab's buttons are made from the
    // templates when the popup opens (see Tabs in AdminPanelPopup.Actions.cs); the level field and its Go button sit at
    // the top of the first (Level) tab.
    public partial class AdminPanelPopup : BasePopup<AdminPanelPopupParameters>
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform levelRow;
        [SerializeField] private TMP_InputField levelInput;
        [SerializeField] private Transform tabBar;
        [SerializeField] private Button tabTemplate;
        [SerializeField] private Transform pageRoot;
        [SerializeField] private GameObject pageTemplate;
        [SerializeField] private Button actionTemplate;
        // Red: actions that reset or take something away.
        [SerializeField] private Button resetTemplate;
        [SerializeField] private Button goButton;

        private readonly List<Button> _tabButtons = new List<Button>();
        private readonly List<GameObject> _pages = new List<GameObject>();

        private ApplicationController _applicationController;
        private PlayerController _playerController;
        private CurrencyController _currencyController;
        private LifeController _lifeController;
        private SceneLoaderController _sceneLoaderController;
        private IPopupService _popupService;
        private ITaskService _taskService;
        private StorageService _storageService;

        private readonly struct AdminAction
        {
            public readonly string Label;
            public readonly Action Run;
            public readonly bool IsReset;

            public AdminAction(string label, Action run, bool isReset = false)
            {
                Label = label;
                Run = run;
                IsReset = isReset;
            }
        }

        private readonly struct AdminTab
        {
            public readonly string Name;
            public readonly AdminAction[] Actions;

            public AdminTab(string name, params AdminAction[] actions)
            {
                Name = name;
                Actions = actions;
            }
        }

        [Inject]
        public void Construct(ApplicationController applicationController, PlayerController playerController,
            CurrencyController currencyController, LifeController lifeController,
            SceneLoaderController sceneLoaderController, IPopupService popupService, ITaskService taskService,
            StorageService storageService)
        {
            _applicationController = applicationController;
            _playerController = playerController;
            _currencyController = currencyController;
            _lifeController = lifeController;
            _sceneLoaderController = sceneLoaderController;
            _popupService = popupService;
            _taskService = taskService;
            _storageService = storageService;
        }

        public override void ShowPopup()
        {
            closeButton.onClick.AddListener(ClosePopup);
            goButton.onClick.AddListener(OnGoClicked);
            levelInput.text = _playerController.GetVisualLevel().ToString();
            tabTemplate.gameObject.SetActive(false);
            pageTemplate.SetActive(false);
            actionTemplate.gameObject.SetActive(false);
            resetTemplate.gameObject.SetActive(false);

            var tabs = Tabs();
            for (var i = 0; i < tabs.Length; i++)
                AddTab(tabs[i], i);
            levelRow.SetParent(_pages[0].transform, false);
            levelRow.SetAsFirstSibling();
            SelectTab(0);
        }

        protected override void Dispose()
        {
            closeButton.onClick.RemoveListener(ClosePopup);
            goButton.onClick.RemoveListener(OnGoClicked);
        }

        private void OnGoClicked() => GoToLevel().Forget();

        private void AddTab(AdminTab tab, int index)
        {
            var tabButton = Instantiate(tabTemplate, tabBar);
            tabButton.gameObject.SetActive(true);
            SetLabel(tabButton, tab.Name);
            tabButton.onClick.AddListener(() => SelectTab(index));
            _tabButtons.Add(tabButton);

            var page = Instantiate(pageTemplate, pageRoot);
            _pages.Add(page);
            foreach (var action in tab.Actions)
            {
                var button = Instantiate(action.IsReset ? resetTemplate : actionTemplate, page.transform);
                button.gameObject.SetActive(true);
                SetLabel(button, action.Label);
                var run = action.Run;
                button.onClick.AddListener(() => run());
            }
        }

        private void SelectTab(int index)
        {
            for (var i = 0; i < _pages.Count; i++)
            {
                _pages[i].SetActive(i == index);
                _tabButtons[i].interactable = i != index;
            }
        }

        private static void SetLabel(Button button, string text) =>
            button.GetComponentInChildren<TextMeshProUGUI>(true).text = text;
    }
}

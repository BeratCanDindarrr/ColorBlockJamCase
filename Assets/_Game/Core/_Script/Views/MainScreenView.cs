using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    public class MainScreenView : BaseView
    {
        [SerializeField] private GraphicRaycaster[] raycasterArray;
        [SerializeField] private MainScreenPageSnapperView pageSnapper;
        [SerializeField] private MainScreenBasePageView[] pages;
        // Same order as pages: menuButtons[i] opens pages[i].
        [SerializeField] private MainScreenMenuButtonView[] menuButtons;
        [SerializeField] private int startPage = 1;

        // Where reward icons fly to (set by the home page that holds them) and the canvas they fly on.
        public MainScreenHeaderView Header { get; private set; }
        public Transform PlayButtonTransform { get; private set; }
        public Transform CoinImageTransform => Header != null ? Header.CoinImageTransform : null;
        public Transform LifeImageTransform => Header != null ? Header.LifeImageTransform : null;
        public Transform FlyLayer => Header != null ? Header.GetComponentInParent<Canvas>().rootCanvas.transform : null;

        public void Init()
        {
            if (menuButtons.Length != pages.Length)
                Debug.LogError($"{nameof(MainScreenView)}: {menuButtons.Length} menu buttons for {pages.Length} pages", this);

            foreach (var page in pages) page.Initialize();
            foreach (var menuButton in menuButtons)
            {
                menuButton.Initialize();
                menuButton.Clicked += OnMenuButtonClicked;
            }

            pageSnapper.Init(pages.Length, startPage);
            pageSnapper.PageChanged += OnPageChanged;
            SelectPage(pageSnapper.CurrentPage);
            
        }

        public void SetCurrencyTargets(MainScreenHeaderView header, Transform playButtonTransform)
        {
            Header = header;
            PlayButtonTransform = playButtonTransform;
        }

        public void SnapToPage(int page) => pageSnapper.SnapTo(page);

        public override void Dispose()
        {
            if (pageSnapper != null) pageSnapper.PageChanged -= OnPageChanged;
            foreach (var page in pages) page.Dispose();
            foreach (var menuButton in menuButtons)
            {
                menuButton.Clicked -= OnMenuButtonClicked;
                menuButton.Dispose();
            }
        }

        private void OnMenuButtonClicked(MainScreenMenuButtonView menuButton)
        {
            var index = Array.IndexOf(menuButtons, menuButton);
            if (index >= 0) SnapToPage(index);
        }

        private void OnPageChanged(int previous, int current)
        {
            pages[previous].OnPageDeselected();
            if (previous < menuButtons.Length) menuButtons[previous].CancelHighlight();
            SelectPage(current);
        }

        private void SelectPage(int page)
        {
            pages[page].OnPageSelected();
            if (page < menuButtons.Length) menuButtons[page].Highlight();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void SwitchRaycaster(bool isEnable)
        {
            if(raycasterArray is {Length: <= 0})return;
            foreach (var raycaster in raycasterArray)
            {
                raycaster.enabled = isEnable;
            }
        }
    }
}
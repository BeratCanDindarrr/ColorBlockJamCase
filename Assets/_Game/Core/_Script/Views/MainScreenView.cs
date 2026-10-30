using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    public class MainScreenView : BaseView
    {
        [SerializeField] private GraphicRaycaster[] raycasterArray;
        [SerializeField] private MainScreenPageSnapperView pageSnapper;
        [SerializeField] private MainScreenBasePageView[] pages;
        [SerializeField] private int startPage = 1;

        public void Init()
        {
            foreach (var page in pages) page.Initialize();
            pageSnapper.Init(pages.Length, startPage);
            pageSnapper.PageChanged += OnPageChanged;
            pages[pageSnapper.CurrentPage].OnPageSelected();
        }

        public void SnapToPage(int page) => pageSnapper.SnapTo(page);

        public override void Dispose()
        {
            if (pageSnapper != null) pageSnapper.PageChanged -= OnPageChanged;
            foreach (var page in pages) page.Dispose();
        }

        private void OnPageChanged(int previous, int current)
        {
            pages[previous].OnPageDeselected();
            pages[current].OnPageSelected();
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
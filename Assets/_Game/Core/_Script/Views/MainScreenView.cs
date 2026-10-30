using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    public class MainScreenView : BaseView
    {
        [SerializeField] private GraphicRaycaster[] raycasterArray;
        public void Init()
        {
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
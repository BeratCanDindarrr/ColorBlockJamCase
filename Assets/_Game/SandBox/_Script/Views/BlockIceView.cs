using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // The ice count on a frozen block: how many more blocks must leave before it can be picked up. The label lies flat
    // on the block, facing the board camera that looks straight down.
    public class BlockIceView : ColorBlockJam.Views.BaseView
    {
        [SerializeField] private TextMeshPro label;

        // localPosition: the block-local point the label sits over; its height stays as authored in the prefab.
        public void Show(int ice, Vector3 localPosition)
        {
            transform.localPosition = new Vector3(localPosition.x, transform.localPosition.y, localPosition.z);
            gameObject.SetActive(ice > 0);
            label.SetText("{0}", ice);
        }

        // A block left the board: the count drops with a punch; at 0 the ice is gone.
        public void Crack(int ice, float punch, float duration)
        {
            if (ice <= 0)
            {
                Hide();
                return;
            }

            label.SetText("{0}", ice);
            transform.DOKill(true);
            transform.DOPunchScale(Vector3.one * punch, duration).SetLink(gameObject);
        }

        public void Hide()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }
    }
}

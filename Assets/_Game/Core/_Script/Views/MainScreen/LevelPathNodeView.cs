using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ColorBlockJam.Views
{
    // One round node of the level path: "Level\n{n}", with a lock while the level is still ahead, and the green bar up
    // to the next node behind it.
    public class LevelPathNodeView : BaseView
    {
        private const string LabelFormat = "Level\n{0}";

        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Transform lockIcon;
        // Grows up from the node's centre; drawn under the node.
        [SerializeField] private RectTransform link;

        public void SetLinkLength(float length) => link.sizeDelta = new Vector2(link.sizeDelta.x, length);

        public void Set(int level, bool locked)
        {
            label.SetText(LabelFormat, level);
            lockIcon.DOKill();
            lockIcon.localScale = Vector3.one;
            lockIcon.gameObject.SetActive(locked);
        }

        public Tween HideLock(float duration) =>
            lockIcon.DOScale(0f, duration).SetEase(Ease.InBack).SetLink(gameObject);
    }
}

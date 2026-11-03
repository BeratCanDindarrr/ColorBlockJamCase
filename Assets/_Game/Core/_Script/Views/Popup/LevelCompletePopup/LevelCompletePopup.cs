using TMPro;
using UnityEngine;

namespace ColorBlockJam.Views
{
    // Level won: shows the coin reward; the Continue button is wired to ClosePopup in the prefab.
    public class LevelCompletePopup : BasePopup<LevelCompletePopupParameters>
    {
        [SerializeField] private TextMeshProUGUI coinsText;

        public override void ShowPopup()
        {
            coinsText.SetText("{0}", Parameters.Coins);
        }
    }
}

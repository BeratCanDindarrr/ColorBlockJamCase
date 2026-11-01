using ColorBlockJam.Const;
using ColorBlockJam.Core.Model;

namespace ColorBlockJam.Views
{
    // Rewards to give: shown as a list, added when the player presses Collect, then flown onto the main screen.
    public class CollectPopupParameters : BasePopupParameters
    {
        public CurrencyData[] Rewards;

        public override string GetPopupName()
        {
            return GlobalConst.PopupName.CollectPopup;
        }
    }
}

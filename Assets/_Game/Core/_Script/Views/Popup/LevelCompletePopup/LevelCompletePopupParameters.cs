using ColorBlockJam.Const;
using ColorBlockJam.Core.Model;

namespace ColorBlockJam.Views
{
    // The level's rewards, already added to the player; Continue flies them onto the main screen.
    public class LevelCompletePopupParameters : BasePopupParameters
    {
        public CurrencyData[] Rewards;

        public override string GetPopupName()
        {
            return GlobalConst.PopupName.LevelCompletePopup;
        }
    }
}

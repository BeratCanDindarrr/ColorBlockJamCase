using ColorBlockJam.Const;

namespace ColorBlockJam.Views
{
    public class LevelCompletePopupParameters : BasePopupParameters
    {
        // Coins shown on the Continue button.
        public int Coins;

        public override string GetPopupName()
        {
            return GlobalConst.PopupName.LevelCompletePopup;
        }
    }
}

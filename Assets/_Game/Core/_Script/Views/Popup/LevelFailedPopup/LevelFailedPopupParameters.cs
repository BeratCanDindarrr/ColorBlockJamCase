using ColorBlockJam.Const;

namespace ColorBlockJam.Views
{
    public class LevelFailedPopupParameters : BasePopupParameters
    {
        public override string GetPopupName()
        {
            return GlobalConst.PopupName.LevelFailedPopup;
        }
    }
}

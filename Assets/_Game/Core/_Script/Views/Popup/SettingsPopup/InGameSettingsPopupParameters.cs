using ColorBlockJam.Const;

namespace ColorBlockJam.Views
{
    public class InGameSettingsPopupParameters : BasePopupParameters
    {
        public override string GetPopupName()
        {
            return GlobalConst.PopupName.InGameSettingsPopup;
        }
    }
}

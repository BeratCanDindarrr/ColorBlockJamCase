using System;
using ColorBlockJam.Const;

namespace ColorBlockJam.Views
{
    public class QuitLevelPopupParameters : BasePopupParameters
    {
        // Runs when the player backs out of quitting.
        public Action OnCloseAction;

        public override string GetPopupName()
        {
            return GlobalConst.PopupName.QuitLevelPopup;
        }
    }
}

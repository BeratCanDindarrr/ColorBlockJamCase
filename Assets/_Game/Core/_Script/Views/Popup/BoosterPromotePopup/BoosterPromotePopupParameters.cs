using System;
using ColorBlockJam.Const;
using UnityEngine;

namespace ColorBlockJam.Views
{
    // A booster just unlocked: what it is, before its tutorial starts.
    public class BoosterPromotePopupParameters : BasePopupParameters
    {
        public Sprite Icon;
        public string Title;
        public string Info;
        // Runs once the popup is closed (Continue or the X).
        public Action OnCloseAction;

        public override string GetPopupName()
        {
            return GlobalConst.PopupName.BoosterPromotePopup;
        }
    }
}

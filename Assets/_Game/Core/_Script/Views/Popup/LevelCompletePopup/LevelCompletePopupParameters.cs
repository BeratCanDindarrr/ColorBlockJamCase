using System;
using ColorBlockJam.Const;
using ColorBlockJam.Core.Model;
using UnityEngine;

namespace ColorBlockJam.Views
{
    // The level's rewards, already added to the player.
    public class LevelCompletePopupParameters : BasePopupParameters
    {
        public CurrencyData[] Rewards;
        // Continue was pressed: the screen point the rewards fly from; the caller goes on with the win flow.
        public Action<Vector2> Continued;

        public override string GetPopupName()
        {
            return GlobalConst.PopupName.LevelCompletePopup;
        }
    }
}

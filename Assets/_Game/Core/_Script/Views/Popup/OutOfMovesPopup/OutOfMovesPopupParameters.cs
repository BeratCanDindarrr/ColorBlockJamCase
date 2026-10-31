using ColorBlockJam.Const;

namespace ColorBlockJam.Views
{
    public class OutOfMovesPopupParameters : BasePopupParameters
    {
        public int AddTimeCoinValue;
        public float AddTimeSeconds;

        public override string GetPopupName()
        {
            return GlobalConst.PopupName.OutOfMovesPopup;
        }
    }
}

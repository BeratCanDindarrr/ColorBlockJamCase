using ColorBlockJam.Const;

namespace ColorBlockJam.Views
{
    public class OutOfTimePopupParameters : BasePopupParameters
    {
        public int AddTimeCoinValue;
        public float AddTimeSeconds;

        public override string GetPopupName()
        {
            return GlobalConst.PopupName.OutOfTimePopup;
        }
    }
}

using ColorBlockJam.Enums;
using ColorBlockJam.Utils;

namespace ColorBlockJam.Views.Currency
{
    // A booster reward, flying to the Play button (boosters are used in a level).
    public class BoosterCurrencyView : CurrencyFlyView
    {
        protected override string FormatValue(CurrencyType currencyType, float value) =>
            "+" + CurrencyText.Amount(currencyType, value);
    }
}

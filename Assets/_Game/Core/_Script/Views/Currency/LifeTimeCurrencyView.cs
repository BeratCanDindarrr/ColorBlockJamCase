using ColorBlockJam.Enums;
using ColorBlockJam.Utils;

namespace ColorBlockJam.Views.Currency
{
    // Limitless life time (value in seconds), flying to the header's life icon.
    public class LifeTimeCurrencyView : CurrencyFlyView
    {
        protected override string FormatValue(CurrencyType currencyType, float value) =>
            "+" + CurrencyText.Amount(currencyType, value);
    }
}

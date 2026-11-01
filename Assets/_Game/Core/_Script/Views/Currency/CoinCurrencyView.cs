using ColorBlockJam.Enums;

namespace ColorBlockJam.Views.Currency
{
    // One of the coins of a coin reward; the header's coin label counts the amount, so it has no label.
    public class CoinCurrencyView : CurrencyFlyView
    {
        protected override string FormatValue(CurrencyType currencyType, float value) => null;
    }
}

using System;
using ColorBlockJam.Enums;

namespace ColorBlockJam.Utils
{
    // How an amount of a currency reads on screen: limitless life time as "30m" / "2h", everything else as the number.
    public static class CurrencyText
    {
        public static string Amount(CurrencyType currencyType, double value)
        {
            if (currencyType != CurrencyType.Time) return $"{value:0}";
            var time = TimeSpan.FromSeconds(value);
            return time.TotalHours >= 1 ? $"{(int)time.TotalHours}h" : $"{(int)time.TotalMinutes}m";
        }
    }
}

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

        // Time left on a running clock: "1h 05m" from an hour up, "12:35" below.
        public static string Countdown(long seconds)
        {
            var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return time.TotalHours >= 1 ? $"{(int)time.TotalHours}h {time.Minutes:00}m" : $"{time.Minutes:00}:{time.Seconds:00}";
        }
    }
}

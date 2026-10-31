using System;
using ColorBlockJam.Enums;

namespace ColorBlockJam.Core.Model
{
    [Serializable]
    public struct CurrencyData
    {
        public CurrencyType currencyType;
        public long currencyValue;

        public CurrencyData(CurrencyType currencyType, long currencyValue)
        {
            this.currencyType = currencyType;
            this.currencyValue = currencyValue;
        }
    }
}

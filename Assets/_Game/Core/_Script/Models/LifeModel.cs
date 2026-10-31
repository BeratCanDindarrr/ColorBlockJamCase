using System;

namespace ColorBlockJam.Core.Model
{
    [Serializable]
    public class LifeModel
    {
        public int currentLife;
        // Unix seconds the refill timer counts from.
        public long lastChangeTime;
        public bool isLimitless;
        public long limitlessFinishTime;

        public LifeModel Clone()
        {
            return new LifeModel
            {
                currentLife = currentLife,
                lastChangeTime = lastChangeTime,
                isLimitless = isLimitless,
                limitlessFinishTime = limitlessFinishTime
            };
        }
    }
}

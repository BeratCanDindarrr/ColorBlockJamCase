using ColorBlockJam.Core.Model;
using ColorBlockJam.Enums;
using Lean.Touch;

namespace ColorBlockJam.Signals
{
    public readonly struct OnFingerDownSignal
    {
        public readonly LeanFinger Finger;

        public OnFingerDownSignal(LeanFinger finger)
        {
            Finger = finger;
        }
    }

    public readonly struct OnFingerUpdateSignal
    {
        public readonly LeanFinger Finger;

        public OnFingerUpdateSignal(LeanFinger finger)
        {
            Finger = finger;
        }
    }

    public readonly struct OnFingerUpSignal
    {
        public readonly LeanFinger Finger;

        public OnFingerUpSignal(LeanFinger finger)
        {
            Finger = finger;
        }
    }

    public readonly struct LevelTimeChangedSignal
    {
        public readonly int SecondsLeft;
        // False until the first move when the clock waits for it.
        public readonly bool IsClockRunning;

        public LevelTimeChangedSignal(int secondsLeft, bool isClockRunning)
        {
            SecondsLeft = secondsLeft;
            IsClockRunning = isClockRunning;
        }
    }

    public readonly struct LevelEndedSignal
    {
        public readonly bool Won;

        public LevelEndedSignal(bool won)
        {
            Won = won;
        }
    }

    public readonly struct LevelUpSignal
    {
    }

    public readonly struct LevelSuccessSignal
    {
    }

    public readonly struct CurrencyUpdateSignal
    {
        public readonly CurrencyData CurrencyData;
        public readonly CurrencyUpdateType CurrencyUpdateType;

        public CurrencyUpdateSignal(CurrencyData currencyData, CurrencyUpdateType currencyUpdateType)
        {
            CurrencyData = currencyData;
            CurrencyUpdateType = currencyUpdateType;
        }
    }

    public readonly struct CoinUpdateSignal
    {
        public readonly long RewardCount;

        public CoinUpdateSignal(long rewardCount)
        {
            RewardCount = rewardCount;
        }
    }

    // The life count or its timer changed.
    public readonly struct LifeModelSync
    {
    }

    // "Play On" in the out-of-time popup bought more time for the running level.
    public readonly struct TimePurchasedSignal
    {
        public readonly float Seconds;

        public TimePurchasedSignal(float seconds)
        {
            Seconds = seconds;
        }
    }
}

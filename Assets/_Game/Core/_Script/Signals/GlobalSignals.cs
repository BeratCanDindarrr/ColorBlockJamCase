using ColorBlockJam.Core.Model;
using ColorBlockJam.Enums;
using Lean.Touch;
using UnityEngine;

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
        // False while the clock still waits for the first move.
        public readonly bool IsClockRunning;
        // True while the Timer booster holds the clock.
        public readonly bool IsClockFrozen;

        public LevelTimeChangedSignal(int secondsLeft, bool isClockRunning, bool isClockFrozen)
        {
            SecondsLeft = secondsLeft;
            IsClockRunning = isClockRunning;
            IsClockFrozen = isClockFrozen;
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

    // A HUD booster button was pressed with at least one of that booster owned; the board decides whether it starts.
    public readonly struct BoosterRequestedSignal
    {
        public readonly CurrencyType Type;
        // Where the button is on screen (pixels): the Rocket flies out from there.
        public readonly Vector2 ScreenPoint;

        public BoosterRequestedSignal(CurrencyType type, Vector2 screenPoint)
        {
            Type = type;
            ScreenPoint = screenPoint;
        }
    }

    // The board is done with the requested booster: Used when one was spent, false when it was refused or cancelled.
    public readonly struct BoosterEndedSignal
    {
        public readonly CurrencyType Type;
        public readonly bool Used;

        public BoosterEndedSignal(CurrencyType type, bool used)
        {
            Type = type;
            Used = used;
        }
    }

    // A booster tutorial waits for (Active) or is done with its booster: while active the board offers only one block
    // for it and ignores taps on any other instead of cancelling.
    public readonly struct BoosterTutorialSignal
    {
        public readonly CurrencyType Type;
        public readonly bool Active;

        public BoosterTutorialSignal(CurrencyType type, bool active)
        {
            Type = type;
            Active = active;
        }
    }

    // The tutorial's booster is picking a target: where on screen (pixels) the one block to tap is.
    public readonly struct BoosterTutorialTargetSignal
    {
        public readonly Vector2 ScreenPoint;

        public BoosterTutorialTargetSignal(Vector2 screenPoint)
        {
            ScreenPoint = screenPoint;
        }
    }

    // The first-level tutorial starts (Active) or stops: the board lets only one block be picked and shows its way out.
    public readonly struct DragTutorialSignal
    {
        public readonly bool Active;

        public DragTutorialSignal(bool active)
        {
            Active = active;
        }
    }

    // The board's answer: the screen points (pixels) the hand drags along, null to hide the hand; Done once the block
    // went out through its gate.
    public readonly struct DragTutorialHintSignal
    {
        public readonly Vector2[] Points;
        public readonly bool Done;

        public DragTutorialHintSignal(Vector2[] points, bool done)
        {
            Points = points;
            Done = done;
        }
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

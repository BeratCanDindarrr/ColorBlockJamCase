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

        public LevelTimeChangedSignal(int secondsLeft)
        {
            SecondsLeft = secondsLeft;
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
}

using Solo.MOST_IN_ONE;

namespace ColorBlockJam.Interfaces
{
    public interface IHapticService
    {
        void Play(MOST_HapticFeedback.HapticTypes type);
        // Ignores calls that come right after the last one.
        void PlayLimited(MOST_HapticFeedback.HapticTypes type);
    }
}

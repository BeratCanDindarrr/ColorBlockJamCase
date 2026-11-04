using ColorBlockJam.Core.Model;
using ColorBlockJam.Interfaces;
using Cysharp.Threading.Tasks;
using Solo.MOST_IN_ONE;

namespace ColorBlockJam.Core.Service
{
    // Vibrates through MOST_HapticFeedback when the saved haptic switch is on (as a
    // service so SandBox can use it too). MOST does nothing in the editor; it vibrates on iOS / Android devices.
    public class HapticService : IHapticService
    {
        // PlayLimited drops calls closer together than this, so bursts stay one buzz.
        private const int LimitIntervalMs = 33;

        private readonly StorageService _storageService;
        private bool _isLimited;

        public HapticService(StorageService storageService)
        {
            _storageService = storageService;
        }

        // The settings model is made by SettingsController.Init; before that, vibrate (the switch defaults to on).
        private bool IsOn => _storageService.StorageModel?.settingsModel?.haptic ?? true;

        public void Play(MOST_HapticFeedback.HapticTypes type)
        {
            if (IsOn) MOST_HapticFeedback.Generate(type);
        }

        public void PlayLimited(MOST_HapticFeedback.HapticTypes type)
        {
            if (_isLimited) return;
            Play(type);
            ReleaseLimit().Forget();
        }

        private async UniTaskVoid ReleaseLimit()
        {
            _isLimited = true;
            await UniTask.Delay(LimitIntervalMs);
            _isLimited = false;
        }
    }
}

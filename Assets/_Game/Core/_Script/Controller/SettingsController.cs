using ColorBlockJam.Core.Model;
using ColorBlockJam.Core.Service;
using ColorBlockJam.Interfaces;

namespace ColorBlockJam.Core.Controller
{
    // Owns the saved music / sound / haptic switches (StorageModel.settingsModel) and applies the audio ones.
    public class SettingsController
    {
        private readonly StorageService _storageService;
        private readonly IAudioService _audioService;

        public SettingsModel Model { get; private set; }

        public SettingsController(StorageService storageService, IAudioService audioService)
        {
            _storageService = storageService;
            _audioService = audioService;
        }

        public void Init()
        {
            _storageService.StorageModel.settingsModel ??= new SettingsModel
            {
                music = true,
                sound = true,
                haptic = true
            };

            Model = _storageService.StorageModel.settingsModel;
            _audioService.SetMusicEnabled(Model.music);
            _audioService.SetSfxEnabled(Model.sound);
        }

        public void SetMusic(bool isOn)
        {
            Model.music = isOn;
            _audioService.SetMusicEnabled(isOn);
        }

        public void SetSound(bool isOn)
        {
            Model.sound = isOn;
            _audioService.SetSfxEnabled(isOn);
        }

        // HapticService reads Model.haptic before every vibration.
        public void SetHaptic(bool isOn)
        {
            Model.haptic = isOn;
        }
    }
}

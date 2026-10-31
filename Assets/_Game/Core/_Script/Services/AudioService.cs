using Ami.BroAudio;
using ColorBlockJam.Interfaces;

namespace ColorBlockJam.Core.Service
{
    // Game code depends on IAudioService only; BroAudio's static API stays behind this class.
    public class AudioService : IAudioService
    {
        private const BroAudioType SfxTypes = BroAudioType.SFX | BroAudioType.UI;

        private bool _sfxEnabled = true;

        public void PlaySfx(SoundID id)
        {
            if (!_sfxEnabled) return;
            BroAudio.Play(id);
        }

        // Music entries play as BGM: a new track crossfades out the current one.
        public void PlayMusic(SoundID id)
        {
            BroAudio.Play(id);
        }

        public void StopMusic(float fadeOut = 0f)
        {
            BroAudio.Stop(BroAudioType.Music, fadeOut);
        }

        // Music keeps playing muted so turning it back on resumes the same track.
        public void SetMusicEnabled(bool enabled)
        {
            BroAudio.SetVolume(BroAudioType.Music, enabled ? 1f : 0f);
        }

        public void SetSfxEnabled(bool enabled)
        {
            _sfxEnabled = enabled;
            BroAudio.SetVolume(SfxTypes, _sfxEnabled ? 1f : 0f);
        }
    }
}

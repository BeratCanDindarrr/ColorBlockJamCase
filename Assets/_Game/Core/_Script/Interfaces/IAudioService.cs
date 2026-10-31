using Ami.BroAudio;

namespace ColorBlockJam.Interfaces
{
    public interface IAudioService
    {
        void PlaySfx(SoundID id);
        void PlayMusic(SoundID id);
        void StopMusic(float fadeOut = 0f);
        void SetMusicEnabled(bool enabled);
        void SetSfxEnabled(bool enabled);
    }
}

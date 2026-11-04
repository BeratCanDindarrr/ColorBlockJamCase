using ColorBlockJam.Enums;

namespace ColorBlockJam.Interfaces
{
    public interface IAudioService
    {
        void PlaySfx(SfxName name);
        void PlayMusic(SfxName name);
        void StopMusic();
        void SetMusicEnabled(bool enabled);
        void SetSfxEnabled(bool enabled);
    }
}

using System.Collections.Generic;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
using ColorBlockJam.Interfaces;
using UnityEngine;

namespace ColorBlockJam.Core.Service
{
    // Game code depends on IAudioService only; Audio Manager Pro's SFXManager (scene object in SampleScene, kept across
    // scenes) stays behind this class. Sounds are looked up by SfxName in SfxListData, as in knit_Out's AudioService.
    public class AudioService : IAudioService
    {
        private readonly Dictionary<SfxName, SfxListData.SfxItem> _items = new Dictionary<SfxName, SfxListData.SfxItem>();

        private bool _sfxEnabled = true;
        private bool _musicEnabled = true;
        // The track asked for last; turning music back on resumes it.
        private SfxName? _music;

        public AudioService(SfxListData sfxList)
        {
            foreach (var item in sfxList.Items)
            {
                if (item.Sfx == null) Debug.LogError($"{nameof(SfxListData)}: {item.Name} has no SFXObject", sfxList);
                else _items[item.Name] = item;
            }
        }

        public void PlaySfx(SfxName name)
        {
            if (_sfxEnabled) Play(name);
        }

        public void PlayMusic(SfxName name)
        {
            _music = name;
            if (!_musicEnabled) return;

            StopActiveMusic();
            Play(name);
        }

        public void StopMusic()
        {
            _music = null;
            StopActiveMusic();
        }

        public void SetMusicEnabled(bool enabled)
        {
            if (_musicEnabled == enabled) return;

            _musicEnabled = enabled;
            if (!enabled) StopActiveMusic();
            else if (_music.HasValue) PlayMusic(_music.Value);
        }

        public void SetSfxEnabled(bool enabled)
        {
            _sfxEnabled = enabled;
        }

        private static void StopActiveMusic()
        {
            if (SFXManager.Main != null) SFXManager.Main.StopLoopMusicWithFade();
        }

        private void Play(SfxName name)
        {
            var manager = SFXManager.Main;
            if (manager == null)
            {
                Debug.LogError($"{nameof(AudioService)}: no SFXManager in the scene, {name} not played");
                return;
            }

            if (!_items.TryGetValue(name, out var item))
            {
                Debug.LogError($"{nameof(AudioService)}: {name} is not in {nameof(SfxListData)}");
                return;
            }

            // The manager reads IsLoop when the sound starts (after its delay), so it is set before every play.
            manager.IsLoop = item.IsLoop;
            manager.Play(item.Sfx);
        }
    }
}

using System;
using System.Collections.Generic;
using ColorBlockJam.Const;
using ColorBlockJam.Enums;
using UnityEngine;

namespace ColorBlockJam.Core.Settings
{
    // Which Audio Manager Pro SFXObject plays for each SfxName (knit_Out's SfxListData, objects only).
    [CreateAssetMenu(menuName = MenuConst.Settings + nameof(SfxListData), fileName = nameof(SfxListData))]
    public class SfxListData : ScriptableObject
    {
        [Serializable]
        public class SfxItem
        {
            public SfxName Name;
            public SFXObject Sfx;
            // Music: plays looped as the one active track.
            public bool IsLoop;
        }

        public List<SfxItem> Items = new List<SfxItem>();
    }
}

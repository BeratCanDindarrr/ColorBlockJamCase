using System;
using System.Collections.Generic;
using ColorBlockJam.Enums;

namespace ColorBlockJam.Core.Model
{
    // The tutorials the player has finished: each booster's (once, on its unlock level) and the first level's drag.
    [Serializable]
    public class BoosterTutorialModel
    {
        public List<CurrencyType> completed = new List<CurrencyType>();
        public bool dragCompleted;
    }
}

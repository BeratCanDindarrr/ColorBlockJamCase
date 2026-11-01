using System.Threading;
using ColorBlockJam.Enums;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ColorBlockJam.Interfaces
{
    // A currency icon that flies from where a reward was given to where that currency is shown.
    public interface ICurrencyUpdate
    {
        // Shows the icon at `position` (world) for `value` of `currencyType`, scaling in.
        public void CreateView(CurrencyType currencyType, Vector3 position, float value);

        // Flies to `targetPos` (world); done when it lands.
        public UniTask FlyView(Vector3 targetPos, CancellationToken cancellationToken);
    }
}

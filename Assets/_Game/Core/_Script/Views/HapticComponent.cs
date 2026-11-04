using ColorBlockJam.Interfaces;
using Solo.MOST_IN_ONE;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    // Vibrates when its button is clicked (knit_Out's HapticComponent; it sits on every UI button). The button's onClick
    // in the prefab calls PlayHaptic.
    [RequireComponent(typeof(Button))]
    public class HapticComponent : BaseView
    {
        [SerializeField] private MOST_HapticFeedback.HapticTypes hapticType = MOST_HapticFeedback.HapticTypes.Selection;

        private IHapticService _hapticService;

        [Inject]
        public void Construct(IHapticService hapticService)
        {
            _hapticService = hapticService;
        }

        public void PlayHaptic()
        {
            if (_hapticService == null)
            {
                Debug.LogWarning($"{nameof(HapticComponent)} on '{name}' was not injected; its view must be made through VContainer", this);
                return;
            }

            _hapticService.Play(hapticType);
        }
    }
}

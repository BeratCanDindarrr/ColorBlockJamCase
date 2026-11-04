using ColorBlockJam.Interfaces;
using Solo.MOST_IN_ONE;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ColorBlockJam.Views
{
    // Vibrates when its button is clicked (knit_Out's HapticComponent; it sits on every UI button). The button's own
    // onClick wiring stays as it is.
    [RequireComponent(typeof(Button))]
    public class HapticComponent : MonoBehaviour
    {
        [SerializeField] private MOST_HapticFeedback.HapticTypes hapticType = MOST_HapticFeedback.HapticTypes.Selection;

        private IHapticService _hapticService;
        private Button _button;

        [Inject]
        public void Construct(IHapticService hapticService)
        {
            _hapticService = hapticService;
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(PlayHaptic);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(PlayHaptic);
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

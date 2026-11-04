using System.Threading;
using ColorBlockJam.Enums;
using ColorBlockJam.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace ColorBlockJam.Views
{
    public class BasePopup<T>: MonoBehaviour,IPopup where T : BasePopupParameters
    {
        private T _popupParameters;
        private bool _isPopupClosed = false;
        private IAudioService _audioService;

        protected T Parameters => _popupParameters;

        // Named apart from the popups' own Construct methods; VContainer calls both.
        [Inject]
        public void ConstructBasePopup(IAudioService audioService)
        {
            _audioService = audioService;
        }

        public void Init(BasePopupParameters parameters)
        {
            _popupParameters = (T)parameters;
            _audioService?.PlaySfx(SfxName.PopupOpen);
            ShowPopup();
        }

        public virtual void ShowPopup()
        {
            
        }

        public virtual void ClosePopup()
        {
            if (_isPopupClosed) return;
            _audioService?.PlaySfx(SfxName.PopupClose);
            _isPopupClosed = true;
        }

        public void DestroyPopup(float delay = 0f)
        {
            Dispose();
            Destroy(this.gameObject,delay);
        }

        protected virtual void Dispose()
        {
            
        }

        public async UniTask RunPopup(CancellationToken cancellationToken)
        {
            await UniTask.WaitUntil(() => _isPopupClosed, cancellationToken:cancellationToken);
            DestroyPopup();
        }
    }
}
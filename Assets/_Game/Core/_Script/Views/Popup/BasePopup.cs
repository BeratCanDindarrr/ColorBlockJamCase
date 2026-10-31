using System.Threading;
using ColorBlockJam.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ColorBlockJam.Views
{
    public class BasePopup<T>: MonoBehaviour,IPopup where T : BasePopupParameters
    {
        private T _popupParameters;
        private bool _isPopupClosed = false;

        protected T Parameters => _popupParameters;

        public void Init(BasePopupParameters parameters)
        {
            _popupParameters = (T)parameters;
            ShowPopup();
        }

        public virtual void ShowPopup()
        {
            
        }

        public virtual void ClosePopup()
        {
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
using System;
using System.Threading;
using ColorBlockJam.Core.Service;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Utils;
using ColorBlockJam.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ColorBlockJam.GameTask
{
    public class PopupGameTask : IGameTask
    {
        private IObjectResolver _objectResolver;
        [Inject]
        public void Constructor(IObjectResolver objectResolver)
        {
            _objectResolver = objectResolver;
        }

        private BasePopupParameters _popupParameters;
        private GameObject _popupPrefab;
        public void Init(BasePopupParameters parameters,GameObject prefab)
        {
            _popupParameters = parameters;
            _popupPrefab = prefab;
        }
        
        public async UniTask RunTask(CancellationToken cancellationToken)
        {
            if(_popupParameters == null) return;
            if (_popupPrefab == null)
            {
                Debug.LogError($"{nameof(PopupGameTask)}: no prefab in PopupSettings named '{_popupParameters.GetPopupName()}'");
                return;
            }
            try
            {
                var popup = _objectResolver.Instantiate(_popupPrefab);
                if (!popup.TryGetComponent(out IPopup popupComponent))
                {
                    Debug.LogError($"{nameof(PopupGameTask)}: prefab '{_popupPrefab.name}' has no {nameof(IPopup)} component", _popupPrefab);
                    UnityEngine.Object.Destroy(popup);
                    return;
                }
                popupComponent.Init(_popupParameters);
                await popupComponent.RunPopup(cancellationToken);
                
            }
            catch (OperationCanceledException e)
            {
                DebugExtension.Log($"{nameof(PopupGameTask)} cancelled: {e.Message}");
            }
        }

        public void Dispose()
        {
            
        }
    }
}
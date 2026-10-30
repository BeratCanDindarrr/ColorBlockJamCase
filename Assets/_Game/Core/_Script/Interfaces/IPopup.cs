using System.Threading;
using ColorBlockJam.Views;
using Cysharp.Threading.Tasks;

namespace ColorBlockJam.Interfaces
{
    public interface IPopup
    {
        public void Init(BasePopupParameters parameters);
        public UniTask RunPopup(CancellationToken cancellationToken);
    }
}
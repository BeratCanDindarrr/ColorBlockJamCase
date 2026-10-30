using System.Threading;
using Cysharp.Threading.Tasks;

namespace ColorBlockJam.Interfaces
{
    public interface IGameTask
    {
        public UniTask RunTask(CancellationToken cancellationToken);
        public void Dispose();
    }
}
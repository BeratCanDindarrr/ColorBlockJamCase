using ColorBlockJam.Signals;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace ColorBlockJam.Core
{
    public class SignalsLifeTimeScope : MonoBehaviour
    {
        public void Init(IContainerBuilder builder)
        {
            var options = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<OnFingerDownSignal>(options);
            builder.RegisterMessageBroker<OnFingerUpdateSignal>(options);
            builder.RegisterMessageBroker<OnFingerUpSignal>(options);
        }
    }
}

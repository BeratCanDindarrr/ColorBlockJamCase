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
            builder.RegisterMessageBroker<LevelTimeChangedSignal>(options);
            builder.RegisterMessageBroker<LevelEndedSignal>(options);
            builder.RegisterMessageBroker<LevelUpSignal>(options);
            builder.RegisterMessageBroker<LevelSuccessSignal>(options);
            builder.RegisterMessageBroker<CurrencyUpdateSignal>(options);
            builder.RegisterMessageBroker<CoinUpdateSignal>(options);
            builder.RegisterMessageBroker<LifeModelSync>(options);
            builder.RegisterMessageBroker<TimePurchasedSignal>(options);
            builder.RegisterMessageBroker<BoosterRequestedSignal>(options);
            builder.RegisterMessageBroker<BoosterEndedSignal>(options);
            builder.RegisterMessageBroker<PopupOpenedSignal>(options);
            builder.RegisterMessageBroker<BoosterTutorialSignal>(options);
            builder.RegisterMessageBroker<BoosterTutorialTargetSignal>(options);
            builder.RegisterMessageBroker<DragTutorialSignal>(options);
            builder.RegisterMessageBroker<DragTutorialHintSignal>(options);
        }
    }
}

using ColorBlockJam.Core.Settings;
using ColorBlockJam.Pool;
using ColorBlockJam.Views.Currency;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ColorBlockJam
{
    public class ViewLifeTimeScope : MonoBehaviour
    {
        [SerializeField] private MainCameraView mainCameraView;
        public void Init(IContainerBuilder  builder)
        {
            builder.RegisterComponent(mainCameraView).AsSelf();
            RegisterCurrencyViewPools(builder);
        }

        // Reward icons of CurrencyUpdateAnimationGameTask; prefabs come from CurrencySettings.
        private static void RegisterCurrencyViewPools(IContainerBuilder builder)
        {
            builder.Register(r =>
            {
                var settings = r.Resolve<CurrencySettings>();
                return new MonoMemoryPool<CoinCurrencyView>(r, settings.CoinViewPrefab, settings.MaxCoinViews);
            }, Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register(r => new MonoMemoryPool<LifeTimeCurrencyView>(r, r.Resolve<CurrencySettings>().LifeTimeViewPrefab),
                Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register(r => new MonoMemoryPool<BoosterCurrencyView>(r, r.Resolve<CurrencySettings>().BoosterViewPrefab),
                Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}

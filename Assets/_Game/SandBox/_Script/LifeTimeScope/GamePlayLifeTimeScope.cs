using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ColorBlockJam.SandBox
{
    // Child of GameLifeTimeScope (set via parentReference) so gameplay can resolve Core services.
    public class GamePlayLifeTimeScope : LifetimeScope
    {
        [SerializeField] private BoardSettings boardSettings;
        [SerializeField] private LevelSettings levelSettings;
        [SerializeField] private BoardCameraView boardCameraView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(boardSettings);
            builder.RegisterInstance(levelSettings);
            builder.RegisterComponent(boardCameraView);
            builder.RegisterEntryPoint<BoardController>().AsSelf();
        }
    }
}

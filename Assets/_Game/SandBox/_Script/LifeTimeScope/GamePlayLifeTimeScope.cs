using ColorBlockJam.Pool;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ColorBlockJam.SandBox
{
    // Child of GameLifeTimeScope (set via parentReference) so gameplay can resolve Core services.
    public class GamePlayLifeTimeScope : LifetimeScope
    {
        [SerializeField] private BoardCameraView boardCameraView;
        [SerializeField] private BoardViewController boardViewController;

        protected override void Configure(IContainerBuilder builder)
        {
            // BoardSettings is registered in the root scope (GlobalSettings); the pools need its prefabs now, at build time.
            var boardSettings = Parent.Container.Resolve<BoardSettings>();
            builder.RegisterComponent(boardCameraView);
            builder.RegisterComponent(boardViewController);
            // Idle pool items park under the board root so they unload with the gameplay scene. Not under this
            // scope: the scope is mid-destroy when its container disposes, and re-parenting onto it then fails.
            var poolRoot = boardViewController.transform;
            builder.RegisterMonoPool(boardSettings.FloorTilePrefab, parent: poolRoot);
            builder.RegisterMonoPool(boardSettings.WallPiecePrefab, parent: poolRoot);
            builder.RegisterMonoPool(boardSettings.GatePrefab, parent: poolRoot);
            builder.RegisterMonoPool(boardSettings.BlockPrefab, boardSettings.BlockPoolSize, parent: poolRoot);
            builder.RegisterMonoPool(boardSettings.BlockPiecePrefab, boardSettings.PiecePoolSize, parent: poolRoot);
            builder.RegisterEntryPoint<BoardController>().AsSelf();
            builder.RegisterEntryPoint<BoardDragController>();
        }
    }
}

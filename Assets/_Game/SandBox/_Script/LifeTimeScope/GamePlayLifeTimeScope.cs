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
            // OnValidate clamps these in the editor but does not run in builds, so clamp again here against
            // the pool's own ceiling (MemoryPoolBase throws when initialSize > maxSize).
            var blockPoolSize = Mathf.Clamp(boardSettings.BlockPoolSize, 0, MemoryPoolBase<object>.DefaultMaxSize);
            var piecePoolSize = Mathf.Clamp(boardSettings.PiecePoolSize, 0, MemoryPoolBase<object>.DefaultMaxSize);
            builder.RegisterMonoPool(boardSettings.BlockPrefab, blockPoolSize, parent: poolRoot);
            builder.RegisterMonoPool(boardSettings.BlockPiecePrefab, piecePoolSize, parent: poolRoot);
            builder.RegisterEntryPoint<BoardController>().AsSelf();
            builder.RegisterEntryPoint<BoardDragController>();
        }
    }
}

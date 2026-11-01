using ColorBlockJam.Core.Level;
using System.Collections.Generic;
using ColorBlockJam.Pool;
using ColorBlockJam.SandBox.Board;
using UnityEngine;
using VContainer;

namespace ColorBlockJam.SandBox
{
    // Builds the board's floor, walls, gates and blocks from pooled views and parents them under this
    // scene object, so they live (and unload) with the gameplay scene.
    public class BoardViewController : MonoBehaviour
    {
        // Wall pieces are half a cell long: two per edge cell, centred a quarter cell either side of it.
        private const double WallPieceReach = 0.25;
        // The outer corner pieces sit a quarter cell past the first/last cell's outer edge.
        private const double CornerReach = 0.75;
        private const float HalfCell = 0.5f;

        private readonly List<FloorTileView> _floorTiles = new List<FloorTileView>();
        private readonly List<WallPieceView> _wallPieces = new List<WallPieceView>();
        private readonly List<GateView> _gates = new List<GateView>();
        private readonly Dictionary<BlockState, BlockView> _blocks = new Dictionary<BlockState, BlockView>();

        private BoardSettings _boardSettings;
        private BoardCameraView _cameraView;
        private IMemoryPool<FloorTileView> _floorPool;
        private IMemoryPool<WallPieceView> _wallPool;
        private IMemoryPool<GateView> _gatePool;
        private IMemoryPool<BlockView> _blockPool;
        private BlockView _heldView;
        private Vector3 _heldTarget;

        public BoardLayout Layout { get; private set; }

        [Inject]
        public void Construct(BoardSettings boardSettings, BoardCameraView cameraView,
            IMemoryPool<FloorTileView> floorPool, IMemoryPool<WallPieceView> wallPool,
            IMemoryPool<GateView> gatePool, IMemoryPool<BlockView> blockPool)
        {
            _boardSettings = boardSettings;
            _cameraView = cameraView;
            _floorPool = floorPool;
            _wallPool = wallPool;
            _gatePool = gatePool;
            _blockPool = blockPool;
        }

        public void Build(BoardSimulation simulation)
        {
            Clear();
            Layout = new BoardLayout(_boardSettings, simulation.Cols, simulation.Rows);

            BuildFloor();
            BuildWalls(simulation.Gates);
            foreach (var gate in simulation.Gates)
                Spawn(_gatePool, _gates).Init(gate, Layout, _boardSettings.GetColor(gate.Color));
            foreach (var block in simulation.Blocks)
            {
                var view = _blockPool.Spawn();
                view.transform.SetParent(transform, false);
                view.Init(block, Layout, _boardSettings.GetColor(block.Color));
                _blocks.Add(block, view);
            }

            _cameraView.Frame(transform.position, Layout.OuterHalfExtents, _boardSettings.CameraMargin, Layout.CellSize);
        }

        public BlockView GetView(BlockState block) => _blocks.TryGetValue(block, out var view) ? view : null;

        // Simulation events drive the held block's lift and drop, whatever ended the drag.
        public void Apply(IReadOnlyList<BoardEvent> events)
        {
            foreach (var boardEvent in events)
            {
                switch (boardEvent.Type)
                {
                    case BoardEventType.DragStarted:
                        BeginBlockDrag(boardEvent.Block);
                        break;
                    case BoardEventType.DragEnded:
                        EndBlockDrag(boardEvent.Block);
                        break;
                    case BoardEventType.BlockExitStarted:
                        // The rules let go of the block without a DragEnded; it stops following here.
                        ReleaseHeld(boardEvent.Block);
                        PlayExit(boardEvent.Block, boardEvent.Gate);
                        break;
                    case BoardEventType.Won:
                    case BoardEventType.Lost:
                        // The level end drops the drag without a DragEnded; set the held block back down.
                        if (_heldView) EndBlockDrag(_heldView.Block);
                        break; // Task 10: win/lose flow.
                }
            }
        }

        // Slides the block out through the gate over ExitSeconds, far enough that its rear edge clears the outer
        // face of the wall, then it shatters and goes back to the pool.
        public void PlayExit(BlockState block, GateState gate)
        {
            var view = GetView(block);
            if (!view) return;

            var direction = Layout.OutwardLocal(gate.Edge);
            var wall = Vector3.Dot(Layout.GateOuterLocal(gate.Edge, gate.Index, gate.Length), direction);
            var rearEdge = float.MaxValue;
            foreach (var cell in block.OccupiedCells(block.Origin))
                rearEdge = Mathf.Min(rearEdge, Vector3.Dot(Layout.CellToLocal(cell), direction) - HalfCell * Layout.CellSize);

            view.PlayExit(direction, wall - rearEdge, _boardSettings.ExitSeconds, OnBlockShattered);
        }

        private void OnBlockShattered(BlockView view) => ReleaseBlock(view.Block);

        private void ReleaseBlock(BlockState block)
        {
            var view = _blocks[block];
            _blocks.Remove(block);
            _blockPool.Despawn(view);
        }

        // Lifts the held block and draws it with the SelectedItem camera, above walls and other blocks.
        public void BeginBlockDrag(BlockState block)
        {
            _heldView = GetView(block);
            if (!_heldView) return;

            _heldTarget = _heldView.transform.localPosition;
            _heldView.SetLayer(LayerMask.NameToLayer(BoardLayers.SelectedItem));
            _heldView.Lift();
        }

        // position is the simulation's drag position (origin cell space).
        public void MoveBlock(BlockState block, BoardPoint position)
        {
            if (_heldView && _heldView.Block == block) _heldTarget = Layout.PointToLocal(position);
        }

        // Sets the block down on the cell it now stands on and returns it to the Board layer.
        public void EndBlockDrag(BlockState block)
        {
            var view = ReleaseHeld(block);
            if (view) view.Drop(Layout.CellToLocal(block.Origin));
        }

        private BlockView ReleaseHeld(BlockState block)
        {
            var view = GetView(block);
            if (view == _heldView) _heldView = null;
            if (view) view.SetLayer(LayerMask.NameToLayer(BoardLayers.Board));
            return view;
        }

        // Called on every finger update (each frame while the finger is down), also when the finger is still.
        public void FollowHeld(float deltaTime)
        {
            if (_heldView) _heldView.Follow(_heldTarget, deltaTime);
        }

        public void Clear()
        {
            DespawnAll(_floorPool, _floorTiles);
            DespawnAll(_wallPool, _wallPieces);
            DespawnAll(_gatePool, _gates);
            DespawnAll(_blockPool, _blocks.Values);
            _blocks.Clear();
            _heldView = null;
            Layout = null;
        }

        private void BuildFloor()
        {
            for (var row = 0; row < Layout.Rows; row++)
            {
                for (var col = 0; col < Layout.Cols; col++)
                {
                    var tile = Spawn(_floorPool, _floorTiles);
                    Place(tile.transform, Layout.CellToLocal(new CellCoord(col, row)), Quaternion.identity);
                    tile.SetColor(_boardSettings.FloorColor);
                }
            }
        }

        private void BuildWalls(IReadOnlyList<GateState> gates)
        {
            foreach (var edge in new[] { BoardEdge.Top, BoardEdge.Bottom, BoardEdge.Left, BoardEdge.Right })
            {
                var rotation = Layout.GateRotation(edge);
                for (var i = 0; i < Layout.EdgeLength(edge); i++)
                {
                    if (IsGateCell(gates, edge, i)) continue;
                    AddWallPiece(Layout.EdgeLocal(edge, i - WallPieceReach), rotation, false);
                    AddWallPiece(Layout.EdgeLocal(edge, i + WallPieceReach), rotation, false);
                }
            }

            // The corner mesh rounds toward +x/+z at yaw 0; each yaw step turns it a quarter clockwise.
            AddWallPiece(Layout.EdgeLocal(BoardEdge.Top, Layout.Cols - 1 + CornerReach), Quaternion.Euler(0f, 0f, 0f), true);
            AddWallPiece(Layout.EdgeLocal(BoardEdge.Bottom, Layout.Cols - 1 + CornerReach), Quaternion.Euler(0f, 90f, 0f), true);
            AddWallPiece(Layout.EdgeLocal(BoardEdge.Bottom, -CornerReach), Quaternion.Euler(0f, 180f, 0f), true);
            AddWallPiece(Layout.EdgeLocal(BoardEdge.Top, -CornerReach), Quaternion.Euler(0f, 270f, 0f), true);
        }

        private void AddWallPiece(Vector3 local, Quaternion rotation, bool isCorner)
        {
            var piece = Spawn(_wallPool, _wallPieces);
            Place(piece.transform, local, rotation);
            piece.SetCorner(isCorner);
        }

        private static bool IsGateCell(IReadOnlyList<GateState> gates, BoardEdge edge, int index)
        {
            foreach (var gate in gates)
            {
                if (gate.Edge == edge && index >= gate.Index && index < gate.Index + gate.Length) return true;
            }

            return false;
        }

        private T Spawn<T>(IMemoryPool<T> pool, List<T> spawned) where T : Component
        {
            var view = pool.Spawn();
            view.transform.SetParent(transform, false);
            spawned.Add(view);
            return view;
        }

        private void Place(Transform target, Vector3 local, Quaternion rotation)
        {
            target.localPosition = local;
            target.localRotation = rotation;
            target.localScale = Vector3.one * Layout.CellSize;
        }

        private static void DespawnAll<T>(IDespawnPool<T> pool, ICollection<T> views) where T : Component
        {
            foreach (var view in views)
            {
                // Views may already be destroyed when the scene unloads before the scope is disposed.
                if (view) pool.Despawn(view);
            }

            if (!views.IsReadOnly) views.Clear();
        }
    }
}

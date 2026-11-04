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
    public partial class BoardViewController : ColorBlockJam.Views.BaseView
    {
        private const float HalfCell = 0.5f;

        private readonly List<FloorTileView> _floorTiles = new List<FloorTileView>();
        private readonly List<WallPieceView> _wallPieces = new List<WallPieceView>();
        private readonly List<GateView> _gates = new List<GateView>();
        private readonly Dictionary<BlockState, BlockView> _blocks = new Dictionary<BlockState, BlockView>();
        // Blocks the Vacuum booster takes: their BlockDestroyed plays the suck-in instead of the break-apart.
        private readonly HashSet<BlockState> _vacuumed = new HashSet<BlockState>();
        // Blocks a Rocket takes whole (its last cell): their BlockDestroyed flies the pieces off at once, like a broken
        // cell.
        private readonly HashSet<BlockState> _hit = new HashSet<BlockState>();
        // Blocks the Hammer takes: squash, then break apart springing back.
        private readonly HashSet<BlockState> _hammered = new HashSet<BlockState>();

        private BoardSettings _boardSettings;
        private BoardCameraView _cameraView;
        private IMemoryPool<FloorTileView> _floorPool;
        private IMemoryPool<WallPieceView> _wallPool;
        private IMemoryPool<GateView> _gatePool;
        private IMemoryPool<BlockView> _blockPool;
        private BlockView _heldView;
        private ExitBurstView _exitBurst;
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

            BuildFloor(simulation.Holes);
            BuildWalls(simulation.Gates, simulation.Holes);
            foreach (var gate in simulation.Gates)
                Spawn(_gatePool, _gates).Init(gate, Layout, _boardSettings.GetColor(gate.Color), _boardSettings.GateArrowSize);
            foreach (var block in simulation.Blocks)
                SpawnBlock(block);

            _cameraView.SetBackground(_boardSettings.BackgroundColor);
            _cameraView.Frame(transform.position, Layout.OuterHalfExtents, Layout.CellSize);
        }

        // Called before the rules remove the block, so its view sucks in rather than breaks apart.
        public void MarkVacuumed(BlockState block) => _vacuumed.Add(block);

        // Called before a Rocket takes a block's last cell, so its view breaks apart at once.
        public void MarkHit(BlockState block) => _hit.Add(block);

        // Called before the Hammer takes a block, so its view squashes and breaks apart.
        public void MarkHammered(BlockState block) => _hammered.Add(block);

        private BlockView SpawnBlock(BlockState block)
        {
            var view = _blockPool.Spawn();
            view.transform.SetParent(transform, false);
            view.Init(block, Layout, _boardSettings.GetColor(block.Color));
            _blocks.Add(block, view);
            return view;
        }

        // A board cell's centre in world space, this many cells above the floor.
        public Vector3 CellToWorld(CellCoord cell, float heightInCells) =>
            transform.TransformPoint(Layout.CellToLocal(cell) + Vector3.up * heightInCells * Layout.CellSize);

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
                    case BoardEventType.BlockDestroyed:
                        var destroyed = GetView(boardEvent.Block);
                        if (destroyed && _vacuumed.Remove(boardEvent.Block)) destroyed.PlayVacuum(OnBlockShattered);
                        else if (destroyed && _hammered.Remove(boardEvent.Block)) destroyed.PlayHammerHit(OnBlockShattered);
                        else if (destroyed && _hit.Remove(boardEvent.Block)) destroyed.PlayBreakAll(OnBlockShattered);
                        else if (destroyed) destroyed.PlayDestroy(OnBlockShattered);
                        break;
                    case BoardEventType.BlockBroken:
                        var broken = GetView(boardEvent.Block);
                        var origin = boardEvent.Block.Origin;
                        if (broken)
                            broken.PlayBreak(new CellCoord(boardEvent.Cell.Col - origin.Col, boardEvent.Cell.Row - origin.Row));
                        break;
                    case BoardEventType.BlockAdded:
                        SpawnBlock(boardEvent.Block);
                        break;
                    case BoardEventType.IceChanged:
                        var iced = GetView(boardEvent.Block);
                        if (iced) iced.CrackIce();
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
            var wall = Vector3.Dot(Layout.GateOuterLocal(gate.Edge, gate.Index, gate.Length, gate.Depth), direction);
            var rearEdge = float.MaxValue;
            foreach (var cell in block.OccupiedCells(block.Origin))
                rearEdge = Mathf.Min(rearEdge, Vector3.Dot(Layout.CellToLocal(cell), direction) - HalfCell * Layout.CellSize);

            view.PlayExit(direction, wall - rearEdge, _boardSettings.ExitSeconds, ExitBurst(), OnBlockShattered);
        }

        private void OnBlockShattered(BlockView view) => ReleaseBlock(view.Block);

        // One cube burst per board, made on first exit under the board root (unloads with the scene); none without a
        // prefab, then the pieces fly off as before.
        private ExitBurstView ExitBurst()
        {
            if (_exitBurst == null && _boardSettings.ExitBurstPrefab != null)
                _exitBurst = Instantiate(_boardSettings.ExitBurstPrefab, transform);
            return _exitBurst;
        }

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
            _heldView.SetOutline(true);
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
            if (view)
            {
                view.SetLayer(LayerMask.NameToLayer(BoardLayers.Board));
                view.SetOutline(false);
            }

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
            _vacuumed.Clear();
            _hit.Clear();
            _hammered.Clear();
            _heldView = null;
            Layout = null;
        }

        private void BuildFloor(IReadOnlyCollection<CellCoord> removed)
        {
            var holes = removed as ICollection<CellCoord> ?? new HashSet<CellCoord>(removed);
            for (var row = 0; row < Layout.Rows; row++)
            {
                for (var col = 0; col < Layout.Cols; col++)
                {
                    var cell = new CellCoord(col, row);
                    if (holes.Contains(cell)) continue;

                    var tile = Spawn(_floorPool, _floorTiles);
                    Place(tile.transform, Layout.CellToLocal(cell), Quaternion.identity);
                    tile.SetColor(_boardSettings.FloorColor, _boardSettings.FloorGapColor);
                }
            }
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

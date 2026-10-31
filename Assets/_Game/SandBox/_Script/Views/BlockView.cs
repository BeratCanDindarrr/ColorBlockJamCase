using ColorBlockJam.Core.Level;
using System;
using System.Collections.Generic;
using ColorBlockJam.Pool;
using ColorBlockJam.SandBox.Board;
using DG.Tweening;
using UnityEngine;
using VContainer;

namespace ColorBlockJam.SandBox
{
    // A block built from quarter-cell modular pieces (see BlockOutline). Local units are cells.
    public class BlockView : MonoBehaviour, IPoolable
    {
        private const float Quarter = 0.25f;
        private const float Half = 0.5f;
        // The kit pieces are authored with their rounded/open side toward +x/+z; yaw 0 faces +z.
        private const float CornerYawOffset = 45f;

        private static readonly CellCoord[] Quarters =
        {
            new CellCoord(1, 1), new CellCoord(1, -1), new CellCoord(-1, -1), new CellCoord(-1, 1)
        };

        private readonly List<BlockPieceView> _pieces = new List<BlockPieceView>();
        private readonly HashSet<CellCoord> _cells = new HashSet<CellCoord>();
        // Pieces still riding the exit slide; a piece leaves this list when it shatters.
        private readonly List<BlockPieceView> _intact = new List<BlockPieceView>();
        private IMemoryPool<BlockPieceView> _piecePool;
        private BoardSettings _boardSettings;
        private BoardLayout _layout;
        private Vector3 _exitDirection;
        // Depth along _exitDirection (board local) past which a piece centre has wholly left the wall.
        private float _exitLine;
        private int _shattering;
        private Action<BlockView> _onShattered;

        public BlockState Block { get; private set; }
        // Every piece has shattered and finished its tween; the view is safe to despawn.
        public bool IsShattered { get; private set; }
        public Transform Root => transform;

        [Inject]
        public void Construct(IMemoryPool<BlockPieceView> piecePool, BoardSettings boardSettings)
        {
            _piecePool = piecePool;
            _boardSettings = boardSettings;
        }

        public void Init(BlockState block, BoardLayout layout, Color color)
        {
            Block = block;
            _layout = layout;
            IsShattered = false;
            transform.localPosition = layout.CellToLocal(block.Origin);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * layout.CellSize;

            _cells.Clear();
            _cells.UnionWith(block.Cells);
            foreach (var cell in block.Cells)
            {
                foreach (var quarter in Quarters)
                    AddPiece(BlockOutline.PieceFor(_cells, cell, quarter.Col, quarter.Row), cell, quarter, color);
            }
        }

        public void OnSpawned()
        {
        }

        // Lifts the block off the board; Follow moves it across while the height tween runs.
        public void Lift()
        {
            transform.DOKill();
            transform.DOLocalMoveY(_boardSettings.LiftHeight, _boardSettings.LiftDuration).SetLink(gameObject);
        }

        // Eases toward the target on the board plane (exponential smoothing); the height stays with Lift/Drop.
        public void Follow(Vector3 target, float dt)
        {
            var t = 1f - Mathf.Exp(-_boardSettings.FollowSharpness * dt);
            var current = transform.localPosition;
            transform.localPosition = new Vector3(Mathf.Lerp(current.x, target.x, t), current.y,
                Mathf.Lerp(current.z, target.z, t));
        }

        public void Drop(Vector3 localPosition)
        {
            transform.DOKill();
            transform.DOLocalMove(localPosition, _boardSettings.DropDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
        }

        // Slides `distance` (board local) along `direction` from the block's resting cell, back down onto the floor,
        // over `seconds`. The slide ends with the rearmost piece wholly past the wall line; each piece shatters as it
        // crosses that line (that is how the part past the wall is hidden), and onShattered runs once all are gone.
        public void PlayExit(Vector3 direction, float distance, float seconds, Action<BlockView> onShattered)
        {
            _exitDirection = direction;
            _onShattered = onShattered;
            _intact.Clear();
            _intact.AddRange(_pieces);

            var scale = transform.localScale.x;
            var rearmost = float.MaxValue;
            foreach (var piece in _pieces)
                rearmost = Mathf.Min(rearmost, Vector3.Dot(piece.transform.localPosition, direction) * scale);

            var target = _layout.CellToLocal(Block.Origin) + direction * distance;
            _exitLine = Vector3.Dot(target, direction) + rearmost;

            transform.DOKill();
            // Linear, so the view keeps pace with the simulation, which removes the block after the same time.
            transform.DOLocalMove(target, seconds).SetEase(Ease.Linear).SetLink(gameObject)
                .OnUpdate(ShatterPiecesPastLine)
                .OnComplete(ShatterIntact);
        }

        public void SetLayer(int layer)
        {
            foreach (var child in GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
        }

        public void OnDespawned()
        {
            transform.DOKill();
            // A block let go mid-drag (scene unload) must not hand SelectedItem pieces back to the pool.
            SetLayer(LayerMask.NameToLayer(BoardLayers.Board));
            foreach (var piece in _pieces)
            {
                // Pieces may already be gone when the scene unloads before the scope is disposed.
                if (piece) _piecePool.Despawn(piece);
            }

            _pieces.Clear();
            _intact.Clear();
            _shattering = 0;
            _onShattered = null;
            _layout = null;
            Block = null;
        }

        private void ShatterPiecesPastLine()
        {
            var origin = transform.localPosition;
            var scale = transform.localScale.x;
            for (var i = _intact.Count - 1; i >= 0; i--)
            {
                var depth = Vector3.Dot(origin + _intact[i].transform.localPosition * scale, _exitDirection);
                if (depth < _exitLine) continue;

                PlayShatter(_intact[i]);
                _intact.RemoveAt(i);
            }
        }

        // The slide is over: whatever has not crossed yet (float rounding) goes now.
        private void ShatterIntact()
        {
            foreach (var piece in _intact)
                PlayShatter(piece);
            _intact.Clear();
            if (_shattering == 0) FinishShatter();
        }

        // The piece flies on outward with some sideways/upward spread and shrinks away. It stays parented to the
        // block, so it also keeps the slide's motion.
        private void PlayShatter(BlockPieceView piece)
        {
            var scale = transform.localScale.x;
            var spread = UnityEngine.Random.insideUnitSphere * _boardSettings.ShatterSpread;
            spread.y = Mathf.Abs(spread.y);
            var velocity = _exitDirection * _boardSettings.ShatterForce + spread;
            var duration = _boardSettings.ShatterDuration;

            _shattering++;
            piece.transform.DOLocalMove(piece.transform.localPosition + velocity * (duration / scale), duration)
                .SetEase(Ease.OutQuad).SetLink(piece.gameObject);
            piece.transform.DOScale(0f, duration).SetEase(Ease.InQuad).SetLink(piece.gameObject)
                .OnComplete(OnPieceShattered);
        }

        private void OnPieceShattered()
        {
            _shattering--;
            // Pieces crossing mid-slide may all finish before the slide does; ShatterIntact finishes then.
            if (_shattering == 0 && _intact.Count == 0) FinishShatter();
        }

        private void FinishShatter()
        {
            if (IsShattered) return;
            IsShattered = true;
            var onShattered = _onShattered;
            _onShattered = null;
            onShattered?.Invoke(this);
        }

        private void AddPiece(BlockPiece piece, CellCoord cell, CellCoord quarter, Color color)
        {
            if (piece.Kind == BlockPieceKind.None) return;

            // Row grows toward -z, so a board direction (col, row) is the local direction (x, -z).
            var reach = piece.Kind == BlockPieceKind.InnerCorner ? Half : Quarter;
            var position = new Vector3(cell.Col + quarter.Col * reach, 0f, -(cell.Row + quarter.Row * reach));

            var view = _piecePool.Spawn();
            view.transform.SetParent(transform, false);
            view.transform.localPosition = position;
            view.transform.localRotation = Quaternion.Euler(0f, GetYaw(piece), 0f);
            view.Set(piece.Kind, color);
            _pieces.Add(view);
        }

        private static float GetYaw(BlockPiece piece)
        {
            if (piece.Kind == BlockPieceKind.Center) return 0f;
            var yaw = Mathf.Atan2(piece.DirCol, -piece.DirRow) * Mathf.Rad2Deg;
            return piece.Kind == BlockPieceKind.Edge ? yaw : yaw - CornerYawOffset;
        }
    }
}

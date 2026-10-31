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
        // Held pose: board-plane position, lift height and lean are kept apart and composed in ApplyHeldPose,
        // so the lean pivots on the block's centre instead of its origin cell.
        private Vector3 _planar;
        private float _liftY;
        private Quaternion _tilt = Quaternion.identity;
        private Vector3 _centroid;

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
            _centroid = Vector3.zero;
            foreach (var cell in block.Cells)
                _centroid += new Vector3(cell.Col, 0f, -cell.Row);
            if (block.Cells.Count > 0) _centroid /= block.Cells.Count;

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
            var position = transform.localPosition;
            _planar = new Vector3(position.x, 0f, position.z);
            _liftY = position.y;
            _tilt = transform.localRotation;
            // Targeted at the transform so transform.DOKill() stops it with the other block tweens.
            DOTween.To(() => _liftY, value => _liftY = value, _boardSettings.LiftHeight, _boardSettings.LiftDuration)
                .SetTarget(transform).SetLink(gameObject);
        }

        // Eases toward the target on the board plane (exponential smoothing) and leans toward the way it moves.
        public void Follow(Vector3 target, float dt)
        {
            var t = 1f - Mathf.Exp(-_boardSettings.FollowSharpness * dt);
            var previous = _planar;
            _planar = new Vector3(Mathf.Lerp(previous.x, target.x, t), 0f, Mathf.Lerp(previous.z, target.z, t));

            var velocity = dt > 0f ? (_planar - previous) / dt : Vector3.zero;
            var maxTilt = _boardSettings.MaxTilt;
            // Pitch about x tips the top toward +z, roll about z toward -x: lean with the motion.
            var lean = Quaternion.Euler(
                Mathf.Clamp(velocity.z * _boardSettings.TiltPerSpeed, -maxTilt, maxTilt), 0f,
                Mathf.Clamp(-velocity.x * _boardSettings.TiltPerSpeed, -maxTilt, maxTilt));
            _tilt = Quaternion.Slerp(_tilt, lean, 1f - Mathf.Exp(-_boardSettings.TiltSharpness * dt));
            ApplyHeldPose();
        }

        public void Drop(Vector3 localPosition)
        {
            transform.DOKill();
            ResetHeldPose();
            transform.DOLocalMove(localPosition, _boardSettings.DropDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
            transform.DOLocalRotateQuaternion(Quaternion.identity, _boardSettings.DropDuration)
                .SetEase(Ease.OutQuad).SetLink(gameObject);
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
            ResetHeldPose();
            var align = _boardSettings.ExitAlignDuration;
            // Straighten up while aligning; the exit line math assumes an upright block.
            transform.DOLocalRotateQuaternion(Quaternion.identity, align).SetLink(gameObject);
            // First settle onto the cell in front of the gate (the rules already moved the block there), then slide
            // straight out. Sliding from a half-way drag pose would cut diagonally through the wall beside the gate.
            // The view waits for the shatter before it is released, so the extra align time is safe.
            DOTween.Sequence().SetTarget(transform).SetLink(gameObject)
                .Append(transform.DOLocalMove(_layout.CellToLocal(Block.Origin), align).SetEase(Ease.OutQuad))
                .Append(transform.DOLocalMove(target, seconds).SetEase(_boardSettings.ExitEase)
                    .OnUpdate(ShatterPiecesPastLine))
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
            ResetHeldPose();
            transform.localRotation = Quaternion.identity;
            _shattering = 0;
            _onShattered = null;
            _layout = null;
            Block = null;
        }

        // Rotates about the block centre: the origin-cell pivot is shifted by (c - R c).
        private void ApplyHeldPose()
        {
            var centre = _centroid * transform.localScale.x;
            transform.localRotation = _tilt;
            transform.localPosition = _planar + Vector3.up * _liftY + (centre - _tilt * centre);
        }

        private void ResetHeldPose()
        {
            _liftY = 0f;
            _tilt = Quaternion.identity;
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

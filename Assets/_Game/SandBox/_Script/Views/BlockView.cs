using ColorBlockJam.Core.Level;
using System.Collections.Generic;
using ColorBlockJam.Pool;
using ColorBlockJam.SandBox.Board;
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
        private IMemoryPool<BlockPieceView> _piecePool;

        public BlockState Block { get; private set; }
        public Transform Root => transform;

        [Inject]
        public void Construct(IMemoryPool<BlockPieceView> piecePool)
        {
            _piecePool = piecePool;
        }

        public void Init(BlockState block, BoardLayout layout, Color color)
        {
            Block = block;
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

        public void OnDespawned()
        {
            foreach (var piece in _pieces)
            {
                // Pieces may already be gone when the scene unloads before the scope is disposed.
                if (piece) _piecePool.Despawn(piece);
            }

            _pieces.Clear();
            Block = null;
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

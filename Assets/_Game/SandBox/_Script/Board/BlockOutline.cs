using ColorBlockJam.Core.Level;
using System.Collections.Generic;

namespace ColorBlockJam.SandBox.Board
{
    public enum BlockPieceKind
    {
        Center = 0,
        Edge = 1,
        InnerCorner = 2,
        OuterCorner = 3,
        // Already covered by a neighbour's InnerCorner, which spans the three quarters around a notch.
        None = 4
    }

    // The modular piece for one quarter of a block cell. DirCol/DirRow (each -1, 0 or 1) point at the
    // open side for an Edge, and at the rounded (outer) or notched (inner) corner for the corner kinds.
    public readonly struct BlockPiece
    {
        public readonly BlockPieceKind Kind;
        public readonly int DirCol;
        public readonly int DirRow;

        public BlockPiece(BlockPieceKind kind, int dirCol, int dirRow)
        {
            Kind = kind;
            DirCol = dirCol;
            DirRow = dirRow;
        }
    }

    // Picks pieces so a multi-cell block reads as one rounded shape: every cell is split into four
    // quarters, and each quarter looks only at its two side neighbours and the diagonal toward its corner.
    public static class BlockOutline
    {
        public static BlockPiece PieceFor(ISet<CellCoord> cells, CellCoord cell, int quarterCol, int quarterRow)
        {
            var hasSide = cells.Contains(cell + new CellCoord(quarterCol, 0));
            var hasVertical = cells.Contains(cell + new CellCoord(0, quarterRow));
            var hasDiagonal = cells.Contains(cell + new CellCoord(quarterCol, quarterRow));

            if (!hasSide && !hasVertical) return new BlockPiece(BlockPieceKind.OuterCorner, quarterCol, quarterRow);
            // One side open but the diagonal filled: this quarter borders a notch and the InnerCorner covers it.
            if (hasSide != hasVertical && hasDiagonal) return new BlockPiece(BlockPieceKind.None, 0, 0);
            if (!hasSide) return new BlockPiece(BlockPieceKind.Edge, quarterCol, 0);
            if (!hasVertical) return new BlockPiece(BlockPieceKind.Edge, 0, quarterRow);

            return hasDiagonal
                ? new BlockPiece(BlockPieceKind.Center, 0, 0)
                : new BlockPiece(BlockPieceKind.InnerCorner, quarterCol, quarterRow);
        }
    }
}

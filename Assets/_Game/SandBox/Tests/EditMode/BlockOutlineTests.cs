using ColorBlockJam.Core.Level;
using System.Collections.Generic;
using NUnit.Framework;

namespace ColorBlockJam.SandBox.Board.Tests
{
    public class BlockOutlineTests
    {
        // L shape: (0,0) (1,0) / (0,1); the cell (1,1) is missing.
        private static readonly HashSet<CellCoord> LShape = new HashSet<CellCoord>
        {
            new CellCoord(0, 0), new CellCoord(1, 0), new CellCoord(0, 1)
        };

        private static BlockPiece Piece(int col, int row, int quarterCol, int quarterRow) =>
            BlockOutline.PieceFor(LShape, new CellCoord(col, row), quarterCol, quarterRow);

        [Test]
        public void PieceFor_NoNeighbours_OuterCorner()
        {
            var piece = Piece(0, 0, -1, -1);
            Assert.That(piece.Kind, Is.EqualTo(BlockPieceKind.OuterCorner));
            Assert.That((piece.DirCol, piece.DirRow), Is.EqualTo((-1, -1)));
        }

        [Test]
        public void PieceFor_OneNeighbour_EdgeFacingTheMissingSide()
        {
            var piece = Piece(0, 0, -1, 1);
            Assert.That(piece.Kind, Is.EqualTo(BlockPieceKind.Edge));
            Assert.That((piece.DirCol, piece.DirRow), Is.EqualTo((-1, 0)));
        }

        [Test]
        public void PieceFor_QuarterNextToNotch_NoneBecauseInnerCornerCoversIt()
        {
            Assert.That(Piece(1, 0, -1, 1).Kind, Is.EqualTo(BlockPieceKind.None));
            Assert.That(Piece(0, 1, 1, -1).Kind, Is.EqualTo(BlockPieceKind.None));
        }

        [Test]
        public void PieceFor_BothNeighboursNoDiagonal_InnerCorner() =>
            Assert.That(Piece(0, 0, 1, 1).Kind, Is.EqualTo(BlockPieceKind.InnerCorner));

        [Test]
        public void PieceFor_AllNeighbours_Center()
        {
            var square = new HashSet<CellCoord> { new CellCoord(0, 0), new CellCoord(1, 0), new CellCoord(0, 1), new CellCoord(1, 1) };
            Assert.That(BlockOutline.PieceFor(square, new CellCoord(0, 0), 1, 1).Kind, Is.EqualTo(BlockPieceKind.Center));
        }
    }
}

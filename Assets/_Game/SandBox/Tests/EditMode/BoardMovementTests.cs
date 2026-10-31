using ColorBlockJam.Core.Level;
using NUnit.Framework;

namespace ColorBlockJam.SandBox.Board.Tests
{
    public class BoardMovementTests
    {
        private const double Tolerance = 1e-9;

        [Test]
        public void SlideBlock_StopsAtOtherBlock()
        {
            var moving = new BlockState(BoardTestLevels.Block("A", 0, 0, BlockColor.Red, BoardTestLevels.Cells(0, 0)));
            var blocker = new BlockState(BoardTestLevels.Block("B", 3, 0, BlockColor.Blue, BoardTestLevels.Cells(0, 0)));
            var movement = new BoardMovement(new[] { moving, blocker }, BoardTestLevels.GridSize, BoardTestLevels.GridSize, 0.3);

            var result = movement.SlideBlock(moving, new BoardPoint(0, 0), new BoardPoint(5, 0));

            Assert.That(result.Col, Is.EqualTo(2).Within(Tolerance));
            Assert.That(result.Row, Is.EqualTo(0).Within(Tolerance));
        }

        [Test]
        public void SlideBlock_StopsAtBoardEdge()
        {
            var moving = new BlockState(BoardTestLevels.Block("A", 0, 0, BlockColor.Red, BoardTestLevels.Cells(0, 0, 1, 0)));
            var movement = new BoardMovement(new[] { moving }, 6, 6, 0.3);

            var result = movement.SlideBlock(moving, new BoardPoint(0, 0), new BoardPoint(10, 0));

            Assert.That(result.Col, Is.EqualTo(4).Within(Tolerance));
        }

        [Test]
        public void SlideBlock_FastTargetDoesNotTunnel()
        {
            var moving = new BlockState(BoardTestLevels.Block("A", 0, 0, BlockColor.Red, BoardTestLevels.Cells(0, 0)));
            var blocker = new BlockState(BoardTestLevels.Block("B", 2, 0, BlockColor.Blue, BoardTestLevels.Cells(0, 0)));
            var movement = new BoardMovement(new[] { moving, blocker }, BoardTestLevels.GridSize, BoardTestLevels.GridSize, 0.3);

            var result = movement.SlideBlock(moving, new BoardPoint(0, 0), new BoardPoint(5, 0));

            Assert.That(result.Col, Is.EqualTo(1).Within(Tolerance));
        }

        [Test]
        public void Snap_RoundsHalfUp_ToFreeCell()
        {
            var moving = new BlockState(BoardTestLevels.Block("A", 0, 0, BlockColor.Red, BoardTestLevels.Cells(0, 0)));
            var movement = new BoardMovement(new[] { moving }, BoardTestLevels.GridSize, BoardTestLevels.GridSize, 0.3);

            var snapped = movement.Snap(moving, new BoardPoint(1.5, 0), new CellCoord(0, 0));

            Assert.That(snapped, Is.EqualTo(new CellCoord(2, 0)));
        }

        [Test]
        public void Snap_OccupiedCell_ReturnsFallback()
        {
            var moving = new BlockState(BoardTestLevels.Block("A", 0, 0, BlockColor.Red, BoardTestLevels.Cells(0, 0)));
            var blocker = new BlockState(BoardTestLevels.Block("B", 2, 0, BlockColor.Blue, BoardTestLevels.Cells(0, 0)));
            var movement = new BoardMovement(new[] { moving, blocker }, BoardTestLevels.GridSize, BoardTestLevels.GridSize, 0.3);

            var snapped = movement.Snap(moving, new BoardPoint(1.5, 0), new CellCoord(0, 0));

            Assert.That(snapped, Is.EqualTo(new CellCoord(0, 0)));
        }

        [Test]
        public void BlockAt_IgnoresExitingBlocks()
        {
            var block = new BlockState(BoardTestLevels.Block("A", 3, 0, BlockColor.Red, BoardTestLevels.Cells(0, 0)));
            block.IsExiting = true;
            var movement = new BoardMovement(new[] { block }, BoardTestLevels.GridSize, BoardTestLevels.GridSize, 0.3);

            Assert.That(movement.BlockAt(3.5, 0.5), Is.Null);
        }
    }
}

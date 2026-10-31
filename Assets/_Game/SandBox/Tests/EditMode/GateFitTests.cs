using NUnit.Framework;
using static ColorBlockJam.SandBox.Board.Tests.BoardTestLevels;

namespace ColorBlockJam.SandBox.Board.Tests
{
    public class GateFitTests
    {
        private static bool FitsWhereItStands(GateData gateData, BlockColor color, int col, int row, CellCoord[] cells)
        {
            var block = new BlockState(Block("s", col, row, color, cells));
            var gate = new GateState(gateData);
            return GateFit.Fits(block, block.Origin, gate, GridSize, GridSize);
        }

        [Test]
        public void Fits_SameColourAlignedTouching_True() =>
            Assert.That(FitsWhereItStands(Gate(BoardEdge.Right, 2, 1, BlockColor.Red), BlockColor.Red, GridSize - 1, 2, Cells(0, 0)),
                Is.True);

        [Test]
        public void Fits_WrongColour_False() =>
            Assert.That(FitsWhereItStands(Gate(BoardEdge.Right, 2, 1, BlockColor.Red), BlockColor.Blue, GridSize - 1, 2, Cells(0, 0)),
                Is.False);

        [Test]
        public void Fits_BlockWiderThanGate_False() =>
            // Two cells stacked along the right wall (0,0)+(0,1); the gate is only 1 long.
            Assert.That(FitsWhereItStands(Gate(BoardEdge.Right, 2, 1, BlockColor.Red), BlockColor.Red, GridSize - 1, 2, Cells(0, 0, 0, 1)),
                Is.False);

        [Test]
        public void Fits_OffByOneAlongWall_False() =>
            // Gate spans row 2 only; the block touches the wall one row below it.
            Assert.That(FitsWhereItStands(Gate(BoardEdge.Right, 2, 1, BlockColor.Red), BlockColor.Red, GridSize - 1, 3, Cells(0, 0)),
                Is.False);

        [Test]
        public void Fits_NotTouchingWall_False()
        {
            // Block sits on the left wall. A wrong-colour gate also sits on the left wall (touching, wrong colour);
            // a same-colour gate sits on the right wall, which the block does not touch.
            var block = new BlockState(Block("s", 0, 2, BlockColor.Red, Cells(0, 0)));
            var wrongColourOnTouchedWall = new GateState(Gate(BoardEdge.Left, 2, 1, BlockColor.Blue));
            var sameColourOnUntouchedWall = new GateState(Gate(BoardEdge.Right, 2, 1, BlockColor.Red));

            Assert.That(GateFit.Fits(block, block.Origin, wrongColourOnTouchedWall, GridSize, GridSize), Is.False);
            Assert.That(GateFit.Fits(block, block.Origin, sameColourOnUntouchedWall, GridSize, GridSize), Is.False);
        }

        [Test]
        public void Fits_RepeatedUse_StillTrue()
        {
            // No capacity on gates: two different same-colour blocks both fit the same gate.
            var gate = new GateState(Gate(BoardEdge.Right, 2, 1, BlockColor.Red));
            var first = new BlockState(Block("a", GridSize - 1, 2, BlockColor.Red, Cells(0, 0)));
            var second = new BlockState(Block("b", GridSize - 1, 2, BlockColor.Red, Cells(0, 0)));

            Assert.That(GateFit.Fits(first, first.Origin, gate, GridSize, GridSize), Is.True);
            Assert.That(GateFit.Fits(second, second.Origin, gate, GridSize, GridSize), Is.True);
        }

        [Test]
        public void Push_IntoRightWall_Positive_AwayNegative()
        {
            var into = GateFit.Push(new BoardPoint(3, 2), new BoardPoint(5, 2), BoardEdge.Right);
            var away = GateFit.Push(new BoardPoint(3, 2), new BoardPoint(1, 2), BoardEdge.Right);

            Assert.That(into, Is.GreaterThan(0));
            Assert.That(away, Is.LessThan(0));
        }
    }
}

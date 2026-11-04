using System.Linq;
using ColorBlockJam.Core.Level;
using NUnit.Framework;
using static ColorBlockJam.SandBox.Board.Tests.BoardTestLevels;

namespace ColorBlockJam.SandBox.Board.Tests
{
    public class BoardRouteTests
    {
        private static BoardSimulation Level(BlockAxis axis, params BlockData[] extra)
        {
            var a = Block("A", 2, 2, BlockColor.Red, Cells(0, 0));
            a.Axis = axis;
            var blocks = new[] { a }.Concat(extra).ToArray();
            var gates = new[] { Gate(BoardEdge.Left, 0, 1, BlockColor.Red) };
            return new BoardSimulation(Create(GridSize, GridSize, 60, blocks, gates), new BoardRules(0.3, true, 0.35));
        }

        [Test]
        public void RouteToGate_WalksCellByCell_ThenOneIntoTheGate()
        {
            var sim = Level(BlockAxis.Free);

            var route = sim.RouteToGate(sim.Blocks[0]);

            Assert.That(route.First(), Is.EqualTo(new CellCoord(2, 2)));
            Assert.That(route[route.Count - 2], Is.EqualTo(new CellCoord(0, 0)));
            Assert.That(route.Last(), Is.EqualTo(new CellCoord(-1, 0)));
            Assert.That(route.Count, Is.EqualTo(6));
            for (var i = 1; i < route.Count; i++)
                Assert.That(System.Math.Abs(route[i].Col - route[i - 1].Col) + System.Math.Abs(route[i].Row - route[i - 1].Row), Is.EqualTo(1));
        }

        [Test]
        public void RouteToGate_GoesAroundOtherBlocks()
        {
            // A wall of blue blocks in column 1, rows 1-5: the way to the gate is over the top.
            var wall = Block("W", 1, 1, BlockColor.Blue, Cells(0, 0, 0, 1, 0, 2, 0, 3, 0, 4));
            var sim = Level(BlockAxis.Free, wall);

            var route = sim.RouteToGate(sim.Blocks[0]);

            Assert.That(route, Is.Not.Null);
            Assert.That(route, Has.None.Matches<CellCoord>(c => c.Col == 1 && c.Row >= 1));
            Assert.That(route.Last(), Is.EqualTo(new CellCoord(-1, 0)));
        }

        [Test]
        public void RouteToGate_AxisBlockOffTheGateRow_ReturnsNull()
        {
            var sim = Level(BlockAxis.Horizontal);

            Assert.That(sim.RouteToGate(sim.Blocks[0]), Is.Null);
        }
    }
}

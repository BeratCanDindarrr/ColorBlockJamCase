using System.Linq;
using ColorBlockJam.Core.Level;
using NUnit.Framework;
using static ColorBlockJam.SandBox.Board.Tests.BoardTestLevels;

namespace ColorBlockJam.SandBox.Board.Tests
{
    public class BoardGateIceTests
    {
        private static readonly BoardRules Rules = new BoardRules(0.3, true, 0.35);

        // A (red) beside the red gate on the left wall, B (blue) beside the blue gate on the right wall; the red gate is
        // iced `ice` times.
        private static BoardSimulation Level(int ice)
        {
            var blocks = new[]
            {
                Block("A", 0, 0, BlockColor.Red, Cells(0, 0)),
                Block("B", GridSize - 1, GridSize - 1, BlockColor.Blue, Cells(0, 0)),
                Block("C", 2, 2, BlockColor.Blue, Cells(0, 0))
            };
            var red = Gate(BoardEdge.Left, 0, 1, BlockColor.Red);
            red.Ice = ice;
            var gates = new[] { red, Gate(BoardEdge.Right, GridSize - 1, 1, BlockColor.Blue) };
            return new BoardSimulation(Create(GridSize, GridSize, 60, blocks, gates), Rules);
        }

        private static BlockState Find(BoardSimulation sim, string id) => sim.Blocks.First(b => b.Id == id);

        private static bool Push(BoardSimulation sim, string id, BoardPoint target)
        {
            var block = Find(sim, id);
            sim.BeginDrag(block);
            sim.MoveDrag(target);
            var exited = block.IsExiting;
            if (!exited) sim.EndDrag();
            return exited;
        }

        [Test]
        public void IcedGate_RefusesAFittingBlock()
        {
            var sim = Level(1);

            Assert.That(Push(sim, "A", new BoardPoint(-1, 0)), Is.False);
            Assert.That(sim.Blocks.Count, Is.EqualTo(3));
        }

        [Test]
        public void BlockLeaving_LowersGateIce_AndReportsIt()
        {
            var sim = Level(1);
            sim.TakeEvents();

            Assert.That(Push(sim, "B", new BoardPoint(GridSize, GridSize - 1)), Is.True);

            Assert.That(sim.Gates[0].Ice, Is.EqualTo(0));
            var changed = sim.TakeEvents().Where(e => e.Type == BoardEventType.IceChanged).ToList();
            Assert.That(changed.Count, Is.EqualTo(1));
            Assert.That(changed[0].Gate, Is.SameAs(sim.Gates[0]));
            Assert.That(changed[0].Block, Is.Null);
        }

        [Test]
        public void ThawedGate_TakesTheBlock()
        {
            var sim = Level(1);
            Push(sim, "B", new BoardPoint(GridSize, GridSize - 1));

            Assert.That(Push(sim, "A", new BoardPoint(-1, 0)), Is.True);
        }

        [Test]
        public void RouteToGate_SkipsIcedGate()
        {
            var sim = Level(1);

            Assert.That(sim.RouteToGate(Find(sim, "A")), Is.Null);
            Assert.That(sim.RouteToGate(Find(sim, "B")), Is.Not.Null);
        }
    }
}

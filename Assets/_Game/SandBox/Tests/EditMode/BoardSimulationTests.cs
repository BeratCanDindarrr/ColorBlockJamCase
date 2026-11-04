using System.Collections.Generic;
using System.Linq;
using ColorBlockJam.Core.Level;
using NUnit.Framework;
using static ColorBlockJam.SandBox.Board.Tests.BoardTestLevels;

namespace ColorBlockJam.SandBox.Board.Tests
{
    public class BoardSimulationTests
    {
        private static BoardRules Rules(bool clockWaitsForFirstMove = true) =>
            new BoardRules(0.3, clockWaitsForFirstMove, 0.35);

        private static BlockState Find(BoardSimulation sim, string id) => sim.Blocks.First(b => b.Id == id);

        // Drags block A (top-left, red) one cell past the left wall into its red gate.
        private static BlockState PushAIntoLeftGate(BoardSimulation sim)
        {
            var block = Find(sim, "A");
            Assert.That(sim.BeginDrag(block), Is.True);
            sim.MoveDrag(new BoardPoint(-1, 0));
            return block;
        }

        private static bool HasEvent(IEnumerable<BoardEvent> events, BoardEventType type) => events.Any(e => e.Type == type);

        [Test]
        public void BeginDrag_StartsClock_WhenWaitingForFirstMove()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules());
            Assert.That(sim.IsClockRunning, Is.False);

            Assert.That(sim.BeginDrag(Find(sim, "A")), Is.True);
            Assert.That(sim.IsClockRunning, Is.True);
            Assert.That(HasEvent(sim.TakeEvents(), BoardEventType.DragStarted), Is.True);
            sim.AdvanceClock(1);
            Assert.That(sim.TimeLeft, Is.EqualTo(59));
        }

        [Test]
        public void TakeEvents_HandsEachEventOverOnce()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules());
            sim.BeginDrag(Find(sim, "A"));

            Assert.That(sim.TakeEvents().Count, Is.EqualTo(1));
            Assert.That(sim.TakeEvents(), Is.Empty);
        }

        [Test]
        public void AdvanceClock_BeforeFirstMove_KeepsTime()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules());

            sim.AdvanceClock(10);

            Assert.That(sim.TimeLeft, Is.EqualTo(60));
            Assert.That(sim.TakeEvents(), Is.Empty);
        }

        [Test]
        public void MoveDrag_IntoMatchingGate_RemovesBlock_AndClearsDrag()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules());
            sim.BeginDrag(Find(sim, "A"));
            sim.TakeEvents();
            var block = Find(sim, "A");
            sim.MoveDrag(new BoardPoint(-1, 0));

            Assert.That(block.IsExiting, Is.True);
            Assert.That(block.ExitGate, Is.SameAs(sim.Gates[0]));
            Assert.That(sim.DraggedBlock, Is.Null);
            Assert.That(sim.Blocks, Has.No.Member(block));
            Assert.That(sim.BlockAt(new BoardPoint(0.5, 0.5)), Is.Null);
            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Playing));

            var exit = sim.TakeEvents().Single();
            Assert.That(exit.Type, Is.EqualTo(BoardEventType.BlockExitStarted));
            Assert.That(exit.Block, Is.SameAs(block));
            Assert.That(exit.Gate, Is.SameAs(sim.Gates[0]));
        }

        [Test]
        public void MoveDrag_IntoGateBelowPushThreshold_NoExit()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules());
            var block = Find(sim, "A");
            sim.BeginDrag(block);
            sim.MoveDrag(new BoardPoint(-0.2, 0));

            Assert.That(block.IsExiting, Is.False);
            Assert.That(sim.DraggedBlock, Is.SameAs(block));
        }

        [Test]
        public void MoveDrag_LastBlockIntoGate_Won()
        {
            var sim = new BoardSimulation(OneBlockOneGate(60), Rules());
            PushAIntoLeftGate(sim);

            var events = sim.TakeEvents();

            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Won));
            Assert.That(sim.Blocks, Is.Empty);
            // The exit is reported before the win, so the view still slides the block out.
            Assert.That(events.Select(e => e.Type), Is.EqualTo(new[]
            {
                BoardEventType.DragStarted, BoardEventType.BlockExitStarted, BoardEventType.Won
            }));
        }

        [Test]
        public void AdvanceClock_RunsOut_WithBlocksLeft_Lost()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules(clockWaitsForFirstMove: false));

            sim.AdvanceClock(61);

            Assert.That(sim.TimeLeft, Is.EqualTo(0));
            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Lost));
            Assert.That(HasEvent(sim.TakeEvents(), BoardEventType.Lost), Is.True);
        }

        [Test]
        public void BeginDrag_ExitingBlock_ReturnsFalse()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules());
            var block = PushAIntoLeftGate(sim);

            Assert.That(sim.BeginDrag(block), Is.False);
            Assert.That(sim.DraggedBlock, Is.Null);
        }

        [Test]
        public void AfterWon_NoFurtherEvents()
        {
            var sim = new BoardSimulation(OneBlockOneGate(60), Rules());
            PushAIntoLeftGate(sim);
            sim.TakeEvents();
            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Won));

            sim.AdvanceClock(100);
            Assert.That(sim.TakeEvents(), Is.Empty);
            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Won));
        }

        [Test]
        public void AddTime_AfterLost_ResumesPlaying()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules(clockWaitsForFirstMove: false));
            sim.AdvanceClock(61);
            sim.TakeEvents();
            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Lost));

            sim.AddTime(30);

            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Playing));
            Assert.That(sim.TimeLeft, Is.EqualTo(30));
            Assert.That(sim.BeginDrag(Find(sim, "A")), Is.True);
        }

        [Test]
        public void AddTime_AfterWon_DoesNothing()
        {
            var sim = new BoardSimulation(OneBlockOneGate(60), Rules());
            PushAIntoLeftGate(sim);

            sim.AddTime(30);

            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Won));
            Assert.That(sim.TimeLeft, Is.EqualTo(60));
        }

        // Cell (2,2) of a 6x6 board removed; a red gate at depth 3 stands on the Top side of (2,3), just below the hole.
        [Test]
        public void MoveDrag_IntoInnerGateBesideRemovedCell_Exits()
        {
            var level = new BoardLevelDefinition(6, 6, 60,
                new[] { Block("A", 2, 5, BlockColor.Red, Cells(0, 0)) },
                new[] { Gate(BoardEdge.Top, 2, 1, BlockColor.Red, depth: 3) },
                new[] { new CellCoord(2, 2) });
            var sim = new BoardSimulation(level, Rules());
            var block = sim.Blocks[0];

            Assert.That(sim.BeginDrag(block), Is.True);
            sim.MoveDrag(new BoardPoint(2, 1));

            Assert.That(sim.Blocks, Has.No.Member(block));
            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Won));
        }

        [Test]
        public void MoveDrag_PastRemovedCellWithoutGate_Stops()
        {
            var level = new BoardLevelDefinition(6, 6, 60,
                new[] { Block("A", 2, 5, BlockColor.Red, Cells(0, 0)), Block("B", 0, 0, BlockColor.Blue, Cells(0, 0)) },
                new[] { Gate(BoardEdge.Left, 0, 1, BlockColor.Blue) },
                new[] { new CellCoord(2, 2) });
            var sim = new BoardSimulation(level, Rules());
            var block = sim.Blocks[0];

            sim.BeginDrag(block);
            var position = sim.MoveDrag(new BoardPoint(2, 0));

            Assert.That(position.Row, Is.EqualTo(3).Within(1e-9));
            Assert.That(sim.DraggedBlock, Is.SameAs(block));
        }

        // A (red, top-left) next to its gate; B (blue, bottom-right) frozen for one exit.
        private static BoardSimulation IcedLevel(int ice)
        {
            var b = Block("B", GridSize - 1, GridSize - 1, BlockColor.Blue, Cells(0, 0));
            b.Ice = ice;
            var blocks = new[] { Block("A", 0, 0, BlockColor.Red, Cells(0, 0)), b };
            var gates = new[]
            {
                Gate(BoardEdge.Left, 0, 1, BlockColor.Red),
                Gate(BoardEdge.Right, GridSize - 1, 1, BlockColor.Blue)
            };
            return new BoardSimulation(Create(GridSize, GridSize, 60, blocks, gates), Rules());
        }

        // One red block in the top-left corner locked to an axis, with red gates on the left and top walls.
        private static BoardSimulation AxisLevel(BlockAxis axis)
        {
            var a = Block("A", 0, 0, BlockColor.Red, Cells(0, 0));
            a.Axis = axis;
            var gates = new[] { Gate(BoardEdge.Left, 0, 1, BlockColor.Red), Gate(BoardEdge.Top, 0, 1, BlockColor.Red) };
            return new BoardSimulation(Create(GridSize, GridSize, 60, new[] { a }, gates), Rules());
        }

        [Test]
        public void BeginDrag_IcedBlock_ReturnsFalse()
        {
            var sim = IcedLevel(1);

            Assert.That(sim.BeginDrag(Find(sim, "B")), Is.False);
            Assert.That(sim.DraggedBlock, Is.Null);
        }

        [Test]
        public void BlockExit_CracksIce_AndThawedBlockCanBeDragged()
        {
            var sim = IcedLevel(1);
            var iced = Find(sim, "B");

            PushAIntoLeftGate(sim);

            Assert.That(iced.Ice, Is.EqualTo(0));
            Assert.That(sim.TakeEvents().Any(e => e.Type == BoardEventType.IceChanged && e.Block == iced), Is.True);
            Assert.That(sim.BeginDrag(iced), Is.True);
        }

        [Test]
        public void BlockExit_IceAboveOne_StaysFrozen()
        {
            var sim = IcedLevel(2);
            var iced = Find(sim, "B");

            PushAIntoLeftGate(sim);

            Assert.That(iced.Ice, Is.EqualTo(1));
            Assert.That(sim.BeginDrag(iced), Is.False);
        }

        [Test]
        public void MoveDrag_HorizontalBlock_MovesOnlyAlongColumns()
        {
            var sim = AxisLevel(BlockAxis.Horizontal);
            sim.BeginDrag(Find(sim, "A"));

            Assert.That(sim.MoveDrag(new BoardPoint(0, 3)).Row, Is.EqualTo(0));
            var moved = sim.MoveDrag(new BoardPoint(3, 2));
            Assert.That(moved.Col, Is.EqualTo(3));
            Assert.That(moved.Row, Is.EqualTo(0));
        }

        [Test]
        public void MoveDrag_VerticalBlock_ExitsTopGate_NotLeftGate()
        {
            var sim = AxisLevel(BlockAxis.Vertical);
            var block = Find(sim, "A");
            sim.BeginDrag(block);

            sim.MoveDrag(new BoardPoint(-1, 0));
            Assert.That(block.IsExiting, Is.False);

            sim.MoveDrag(new BoardPoint(0, -1));
            Assert.That(block.IsExiting, Is.True);
            Assert.That(block.ExitGate.Edge, Is.EqualTo(BoardEdge.Top));
        }
    }
}

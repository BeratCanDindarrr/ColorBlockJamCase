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

        [Test]
        public void FreezeClock_BeforeClockRuns_Refused()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules());

            Assert.That(sim.FreezeClock(30), Is.False);
            Assert.That(sim.IsClockFrozen, Is.False);
        }

        [Test]
        public void FreezeClock_StopsTime_ThenClockRunsOn()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules(clockWaitsForFirstMove: false));

            Assert.That(sim.FreezeClock(30), Is.True);
            sim.AdvanceClock(20);
            Assert.That(sim.TimeLeft, Is.EqualTo(60));
            Assert.That(sim.IsClockFrozen, Is.True);

            // 10 s of freeze are left: the other 5 come off the clock.
            sim.AdvanceClock(15);
            Assert.That(sim.IsClockFrozen, Is.False);
            Assert.That(sim.TimeLeft, Is.EqualTo(55));
        }

        [Test]
        public void FreezeClock_WhileFrozen_Refused()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules(clockWaitsForFirstMove: false));
            sim.FreezeClock(30);

            Assert.That(sim.FreezeClock(30), Is.False);
            Assert.That(sim.FrozenTimeLeft, Is.EqualTo(30));
        }

        [Test]
        public void FreezeClock_AfterLevelEnded_Refused()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules(clockWaitsForFirstMove: false));
            sim.AdvanceClock(61);

            Assert.That(sim.FreezeClock(30), Is.False);
        }

        [Test]
        public void RemoveBlock_LeavesBoard_WithDestroyedEvent()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules());
            var block = Find(sim, "A");

            Assert.That(sim.RemoveBlock(block), Is.True);

            Assert.That(sim.Blocks, Has.No.Member(block));
            Assert.That(sim.BlockAt(new BoardPoint(0.5, 0.5)), Is.Null);
            var removed = sim.TakeEvents().Single();
            Assert.That(removed.Type, Is.EqualTo(BoardEventType.BlockDestroyed));
            Assert.That(removed.Block, Is.SameAs(block));
            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Playing));
        }

        [Test]
        public void RemoveBlock_CracksIce_LikeAnExit()
        {
            var sim = IcedLevel(1);
            var iced = Find(sim, "B");

            sim.RemoveBlock(Find(sim, "A"));

            Assert.That(iced.Ice, Is.EqualTo(0));
        }

        [Test]
        public void RemoveBlock_LastBlock_Won()
        {
            var sim = new BoardSimulation(OneBlockOneGate(60), Rules());

            sim.RemoveBlock(Find(sim, "A"));

            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Won));
        }

        [Test]
        public void RemoveBlock_IcedOrHeldBlock_Refused()
        {
            var sim = IcedLevel(1);
            Assert.That(sim.RemoveBlock(Find(sim, "B")), Is.False);

            var held = Find(sim, "A");
            sim.BeginDrag(held);
            Assert.That(sim.RemoveBlock(held), Is.False);
            Assert.That(sim.Blocks, Has.Member(held));
        }

        private static BoardSimulation SingleBlockLevel(params int[] cells)
        {
            var blocks = new[] { Block("A", 1, 1, BlockColor.Red, Cells(cells)) };
            var gates = new[] { Gate(BoardEdge.Left, 1, 1, BlockColor.Red) };
            return new BoardSimulation(Create(GridSize, GridSize, 60, blocks, gates), Rules());
        }

        [Test]
        public void BreakCell_SquareLosesOneCell_KeepsTheOtherThree()
        {
            var sim = SingleBlockLevel(0, 0, 1, 0, 0, 1, 1, 1);
            var block = Find(sim, "A");

            // The bottom-left piece of the 2x2 (offset (0,1) at origin (1,1)).
            Assert.That(sim.BreakCell(block, new CellCoord(1, 2)), Is.True);

            Assert.That(block.Cells, Is.EquivalentTo(Cells(0, 0, 1, 0, 1, 1)));
            Assert.That(sim.Blocks.Count, Is.EqualTo(1));
            var broken = sim.TakeEvents().Single();
            Assert.That(broken.Type, Is.EqualTo(BoardEventType.BlockBroken));
            Assert.That(broken.Cell, Is.EqualTo(new CellCoord(1, 2)));
        }

        [Test]
        public void BreakCell_MiddleOfALine_SplitsIntoTwoBlocks()
        {
            var sim = SingleBlockLevel(0, 0, 1, 0, 2, 0);
            var block = Find(sim, "A");

            sim.BreakCell(block, new CellCoord(2, 1));

            Assert.That(sim.Blocks.Count, Is.EqualTo(2));
            var other = sim.Blocks.Single(b => b != block);
            Assert.That(other.Color, Is.EqualTo(BlockColor.Red));
            Assert.That(other.Id, Is.Not.EqualTo(block.Id));
            var cells = sim.Blocks.SelectMany(b => b.OccupiedCells(b.Origin)).ToList();
            Assert.That(cells, Is.EquivalentTo(new[] { new CellCoord(1, 1), new CellCoord(3, 1) }));
            Assert.That(sim.TakeEvents().Select(e => e.Type),
                Is.EqualTo(new[] { BoardEventType.BlockBroken, BoardEventType.BlockAdded }));
        }

        [Test]
        public void BreakCell_OneCellBlock_LeavesLikeAnExit()
        {
            var sim = SingleBlockLevel(0, 0);

            sim.BreakCell(Find(sim, "A"), new CellCoord(1, 1));

            Assert.That(sim.Blocks, Is.Empty);
            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Won));
        }

        [Test]
        public void BreakCell_ShrinkingBlock_DoesNotCrackIce()
        {
            var b = Block("B", GridSize - 1, GridSize - 1, BlockColor.Blue, Cells(0, 0));
            b.Ice = 1;
            var blocks = new[] { Block("A", 0, 0, BlockColor.Red, Cells(0, 0, 1, 0)), b };
            var sim = new BoardSimulation(Create(GridSize, GridSize, 60, blocks, new GateData[0]), Rules());

            sim.BreakCell(Find(sim, "A"), new CellCoord(1, 0));

            Assert.That(Find(sim, "B").Ice, Is.EqualTo(1));
        }

        [Test]
        public void BreakCell_IcedBlockOrForeignCell_Refused()
        {
            var sim = IcedLevel(1);

            Assert.That(sim.BreakCell(Find(sim, "B"), new CellCoord(GridSize - 1, GridSize - 1)), Is.False);
            Assert.That(sim.BreakCell(Find(sim, "A"), new CellCoord(3, 3)), Is.False);
            Assert.That(sim.Blocks.Count, Is.EqualTo(2));
        }
    }
}

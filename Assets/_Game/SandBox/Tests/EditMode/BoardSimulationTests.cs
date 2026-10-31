using System.Collections.Generic;
using System.Linq;
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
    }
}

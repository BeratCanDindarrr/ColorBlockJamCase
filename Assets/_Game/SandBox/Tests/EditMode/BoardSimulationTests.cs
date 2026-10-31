using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static ColorBlockJam.SandBox.Board.Tests.BoardTestLevels;

namespace ColorBlockJam.SandBox.Board.Tests
{
    public class BoardSimulationTests
    {
        private const double ExitSeconds = 0.5;
        private const double MaxFrameDelta = 0.05;

        private static BoardRules Rules(bool clockWaitsForFirstMove = true) =>
            new BoardRules(0.3, MaxFrameDelta, clockWaitsForFirstMove, 0.35, ExitSeconds);

        private static BlockState Find(BoardSimulation sim, string id) => sim.Blocks.First(b => b.Id == id);

        // Drags block A (top-left, red) one cell past the left wall into its red gate.
        private static BlockState PushAIntoLeftGate(BoardSimulation sim)
        {
            var block = Find(sim, "A");
            Assert.That(sim.BeginDrag(block), Is.True);
            sim.MoveDrag(new BoardPoint(-1, 0));
            return block;
        }

        private static List<BoardEvent> TickFor(BoardSimulation sim, double seconds)
        {
            var events = new List<BoardEvent>();
            for (var t = 0.0; t < seconds; t += MaxFrameDelta)
                events.AddRange(sim.Tick(MaxFrameDelta));
            return events;
        }

        private static bool HasEvent(IEnumerable<BoardEvent> events, BoardEventType type) => events.Any(e => e.Type == type);

        [Test]
        public void BeginDrag_StartsClock_WhenWaitingForFirstMove()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules());
            Assert.That(sim.IsClockRunning, Is.False);

            sim.Tick(MaxFrameDelta);
            Assert.That(sim.TimeLeft, Is.EqualTo(60));

            Assert.That(sim.BeginDrag(Find(sim, "A")), Is.True);
            Assert.That(sim.IsClockRunning, Is.True);
            Assert.That(HasEvent(sim.Tick(MaxFrameDelta), BoardEventType.DragStarted), Is.True);
            Assert.That(sim.TimeLeft, Is.LessThan(60));
        }

        [Test]
        public void MoveDrag_IntoMatchingGate_StartsExit_AndClearsDrag()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules());
            var block = PushAIntoLeftGate(sim);

            Assert.That(block.IsExiting, Is.True);
            Assert.That(block.ExitGate, Is.SameAs(sim.Gates[0]));
            Assert.That(sim.DraggedBlock, Is.Null);

            var exit = sim.Tick(MaxFrameDelta).Single(e => e.Type == BoardEventType.BlockExitStarted);
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
        public void Tick_AfterExitSeconds_RemovesBlock_EmitsBlockRemoved()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules());
            var block = PushAIntoLeftGate(sim);

            var early = TickFor(sim, ExitSeconds / 2);
            Assert.That(sim.Blocks, Has.Member(block));
            Assert.That(HasEvent(early, BoardEventType.BlockRemoved), Is.False);

            var later = TickFor(sim, ExitSeconds);
            Assert.That(sim.Blocks, Has.No.Member(block));
            Assert.That(later.Single(e => e.Type == BoardEventType.BlockRemoved).Block, Is.SameAs(block));
            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Playing));
            Assert.That(sim.BlockAt(new BoardPoint(0.5, 0.5)), Is.Null);
        }

        [Test]
        public void Tick_LastBlockRemoved_Won()
        {
            var sim = new BoardSimulation(OneBlockOneGate(60), Rules());
            PushAIntoLeftGate(sim);

            var events = TickFor(sim, ExitSeconds * 2);

            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Won));
            Assert.That(sim.Blocks, Is.Empty);
            Assert.That(events.Count(e => e.Type == BoardEventType.Won), Is.EqualTo(1));
        }

        [Test]
        public void Tick_ClockRunsOut_WithBlocksLeft_Lost()
        {
            var sim = new BoardSimulation(TwoBlocksTwoGates(), Rules(clockWaitsForFirstMove: false));

            var events = sim.Tick(61);

            Assert.That(sim.TimeLeft, Is.EqualTo(0));
            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Lost));
            Assert.That(HasEvent(events, BoardEventType.Lost), Is.True);
        }

        [Test]
        public void Tick_ClockRunsOut_WhileLastBlockExiting_Won()
        {
            var sim = new BoardSimulation(OneBlockOneGate(1), Rules());
            PushAIntoLeftGate(sim);

            // The clock is not clamped to MaxFrameDelta, so it runs out while the exit is still under way.
            sim.Tick(5);
            Assert.That(sim.TimeLeft, Is.EqualTo(0));
            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Playing));

            TickFor(sim, ExitSeconds * 2);
            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Won));
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
        public void Tick_AfterWon_NoFurtherEvents()
        {
            var sim = new BoardSimulation(OneBlockOneGate(60), Rules());
            PushAIntoLeftGate(sim);
            TickFor(sim, ExitSeconds * 2);
            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Won));

            Assert.That(sim.Tick(MaxFrameDelta), Is.Empty);
            Assert.That(sim.Tick(100), Is.Empty);
            Assert.That(sim.Status, Is.EqualTo(BoardStatus.Won));
        }
    }
}

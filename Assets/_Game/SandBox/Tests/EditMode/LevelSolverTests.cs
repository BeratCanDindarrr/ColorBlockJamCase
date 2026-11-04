using System.Linq;
using ColorBlockJam.Core.Level;
using NUnit.Framework;

namespace ColorBlockJam.SandBox.Board.Tests
{
    public class LevelSolverTests
    {
        private static BlockData Block(string id, int col, int row, BlockColor color, params (int, int)[] cells) =>
            new BlockData
            {
                Id = id, Col = col, Row = row, Color = color,
                Cells = (cells.Length == 0 ? new[] { (0, 0) } : cells).Select(c => new CellCoord(c.Item1, c.Item2)).ToArray()
            };

        private static GateData Gate(BoardEdge edge, int index, int length, BlockColor color) =>
            new GateData { Edge = edge, Index = index, Length = length, Color = color };

        private static SolveResult Solve(int cols, int rows, BlockData[] blocks, params GateData[] gates) =>
            LevelSolver.Solve(new BoardLevelDefinition(cols, rows, 60, blocks, gates));

        [Test]
        public void BlockBesideItsGate_LeavesInOneStep()
        {
            var result = Solve(3, 3, new[] { Block("A", 0, 0, BlockColor.Red) }, Gate(BoardEdge.Left, 2, 1, BlockColor.Red));

            Assert.That(result.Verdict, Is.EqualTo(SolveVerdict.Solvable));
            Assert.That(result.Steps.Single().Exits, Is.True);
            Assert.That(result.Steps.Single().To, Is.EqualTo(new CellCoord(0, 2)));
            Assert.That(result.Steps.Single().Path,
                Is.EqualTo(new[] { new CellCoord(0, 0), new CellCoord(0, 1), new CellCoord(0, 2) }));
            Assert.That(result.Steps.Single().ExitEdge, Is.EqualTo(BoardEdge.Left));
        }

        [Test]
        public void BlockWiderThanItsGate_IsUnsolvable()
        {
            var result = Solve(3, 3, new[] { Block("A", 0, 0, BlockColor.Red, (0, 0), (1, 0)) },
                Gate(BoardEdge.Top, 0, 1, BlockColor.Red));

            Assert.That(result.Verdict, Is.EqualTo(SolveVerdict.Unsolvable));
            Assert.That(result.StuckBlocks, Is.EqualTo(new[] { "A" }));
        }

        [Test]
        public void BlockInTheWay_LeavesFirst()
        {
            // A one-column board: B stands between A and A's gate at the top; B has its own gate on the left.
            var result = Solve(1, 3,
                new[] { Block("A", 0, 2, BlockColor.Red), Block("B", 0, 1, BlockColor.Blue) },
                Gate(BoardEdge.Top, 0, 1, BlockColor.Red), Gate(BoardEdge.Left, 1, 1, BlockColor.Blue));

            Assert.That(result.Verdict, Is.EqualTo(SolveVerdict.Solvable));
            Assert.That(result.Steps.Select(s => s.BlockId), Is.EqualTo(new[] { "B", "A" }));
        }

        [Test]
        public void IcedBlock_WaitsForAnotherToLeave()
        {
            var iced = Block("A", 0, 0, BlockColor.Red);
            iced.Ice = 1;
            var result = Solve(2, 1, new[] { iced, Block("B", 1, 0, BlockColor.Blue) },
                Gate(BoardEdge.Left, 0, 1, BlockColor.Red), Gate(BoardEdge.Right, 0, 1, BlockColor.Blue));

            Assert.That(result.Verdict, Is.EqualTo(SolveVerdict.Solvable));
            Assert.That(result.Steps.Select(s => s.BlockId), Is.EqualTo(new[] { "B", "A" }));
        }

        [Test]
        public void IcedGate_OpensAfterAnotherBlockLeaves()
        {
            var iced = Gate(BoardEdge.Left, 0, 1, BlockColor.Red);
            iced.Ice = 1;
            var result = Solve(2, 1, new[] { Block("A", 0, 0, BlockColor.Red), Block("B", 1, 0, BlockColor.Blue) },
                iced, Gate(BoardEdge.Right, 0, 1, BlockColor.Blue));

            Assert.That(result.Verdict, Is.EqualTo(SolveVerdict.Solvable));
            Assert.That(result.Steps.Select(s => s.BlockId), Is.EqualTo(new[] { "B", "A" }));
        }

        [Test]
        public void OnlyGateIcedForever_IsUnsolvable()
        {
            var iced = Gate(BoardEdge.Left, 0, 1, BlockColor.Red);
            iced.Ice = 1;
            var result = Solve(2, 1, new[] { Block("A", 0, 0, BlockColor.Red) }, iced);

            Assert.That(result.Verdict, Is.EqualTo(SolveVerdict.Unsolvable));
        }

        [Test]
        public void NoUsableGate_IsUnsolvableWithoutSearching()
        {
            // A's gate is iced and B has none: nothing can ever leave, however the blocks are moved.
            var iced = Gate(BoardEdge.Left, 0, 1, BlockColor.Red);
            iced.Ice = 1;
            var result = Solve(4, 4, new[] { Block("A", 0, 0, BlockColor.Red), Block("B", 3, 3, BlockColor.Blue) }, iced);

            Assert.That(result.Verdict, Is.EqualTo(SolveVerdict.Unsolvable));
            Assert.That(result.StatesExplored, Is.Zero);
        }

        [Test]
        public void HorizontalBlockWithOnlyATopGate_IsUnsolvable()
        {
            var block = Block("A", 0, 0, BlockColor.Red);
            block.Axis = BlockAxis.Horizontal;
            var result = Solve(3, 3, new[] { block }, Gate(BoardEdge.Top, 0, 1, BlockColor.Red));

            Assert.That(result.Verdict, Is.EqualTo(SolveVerdict.Unsolvable));
        }

        [Test]
        public void BlockingEachOther_OneMovesAsideFirst()
        {
            // A needs the bottom-left cell (B's) to leave down; B needs the top-left cell (A's) to leave up. One of them
            // steps right first, then both leave.
            var result = Solve(2, 2,
                new[] { Block("A", 0, 0, BlockColor.Red), Block("B", 0, 1, BlockColor.Green) },
                Gate(BoardEdge.Bottom, 0, 1, BlockColor.Red), Gate(BoardEdge.Top, 0, 1, BlockColor.Green));

            Assert.That(result.Verdict, Is.EqualTo(SolveVerdict.Solvable));
            Assert.That(result.Steps.Count, Is.EqualTo(3));
            Assert.That(result.Steps[0].Exits, Is.False);
            Assert.That(result.Steps.Skip(1).All(s => s.Exits), Is.True);
        }
    }
}

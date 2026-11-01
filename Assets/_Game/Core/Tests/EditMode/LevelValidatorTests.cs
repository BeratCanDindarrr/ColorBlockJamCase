using System.Linq;
using NUnit.Framework;

namespace ColorBlockJam.Core.Level.Tests
{
    public class LevelValidatorTests
    {
        private static BlockData Block(string id, int col, int row, BlockColor color, params (int, int)[] cells) =>
            new BlockData
            {
                Id = id, Col = col, Row = row, Color = color,
                Cells = cells.Select(c => new CellCoord(c.Item1, c.Item2)).ToArray()
            };

        private static GateData Gate(BoardEdge edge, int index, int length, BlockColor color, int depth = 0) =>
            new GateData { Edge = edge, Index = index, Length = length, Color = color, Depth = depth };

        private static LevelIssue[] Errors(BlockData[] blocks, GateData[] gates, int cols = 4, int rows = 4) =>
            LevelValidator.Validate(cols, rows, 60, blocks, gates).Where(i => i.IsError).ToArray();

        [Test]
        public void ValidLevel_HasNoIssues()
        {
            var issues = LevelValidator.Validate(4, 4, 60,
                new[] { Block("A", 0, 0, BlockColor.Red, (0, 0), (1, 0)) },
                new[] { Gate(BoardEdge.Left, 0, 1, BlockColor.Red) });

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void BlockOutsideBoard_IsError()
        {
            var errors = Errors(new[] { Block("A", 3, 0, BlockColor.Red, (0, 0), (1, 0)) },
                new[] { Gate(BoardEdge.Left, 0, 1, BlockColor.Red) });

            Assert.That(errors.Single().BlockIndex, Is.EqualTo(0));
        }

        [Test]
        public void OverlappingBlocks_IsError()
        {
            var errors = Errors(new[]
            {
                Block("A", 0, 0, BlockColor.Red, (0, 0), (1, 0)),
                Block("B", 1, 0, BlockColor.Red, (0, 0))
            }, new[] { Gate(BoardEdge.Left, 0, 1, BlockColor.Red) });

            Assert.That(errors.Single().BlockIndex, Is.EqualTo(1));
        }

        [Test]
        public void DisconnectedBlock_IsError()
        {
            var errors = Errors(new[] { Block("A", 0, 0, BlockColor.Red, (0, 0), (2, 0)) },
                new[] { Gate(BoardEdge.Left, 0, 1, BlockColor.Red) });

            Assert.That(errors.Single().Message, Does.Contain("one piece"));
        }

        [Test]
        public void GatePastWallEnd_IsError()
        {
            var errors = Errors(new[] { Block("A", 0, 0, BlockColor.Red, (0, 0)) },
                new[] { Gate(BoardEdge.Top, 3, 2, BlockColor.Red) });

            Assert.That(errors.Single().GateIndex, Is.EqualTo(0));
        }

        [Test]
        public void OverlappingGatesOnSameWall_IsError()
        {
            var errors = Errors(new[] { Block("A", 0, 0, BlockColor.Red, (0, 0)) }, new[]
            {
                Gate(BoardEdge.Top, 0, 2, BlockColor.Red),
                Gate(BoardEdge.Top, 1, 1, BlockColor.Blue)
            });

            Assert.That(errors.Single().GateIndex, Is.EqualTo(1));
        }

        [Test]
        public void BlockWithoutGateOfItsColour_IsWarning()
        {
            var issues = LevelValidator.Validate(4, 4, 60,
                new[] { Block("A", 0, 0, BlockColor.Blue, (0, 0)) },
                new[] { Gate(BoardEdge.Left, 0, 1, BlockColor.Red) });

            Assert.That(issues.Single().IsError, Is.False);
        }

        [Test]
        public void BlockOnRemovedCell_IsError()
        {
            var issues = LevelValidator.Validate(4, 4, 60,
                new[] { Block("A", 1, 1, BlockColor.Red, (0, 0)) },
                new[] { Gate(BoardEdge.Left, 0, 1, BlockColor.Red) },
                new[] { new CellCoord(1, 1) });

            Assert.That(issues.Single(i => i.IsError).BlockIndex, Is.EqualTo(0));
        }

        [Test]
        public void InnerGateOnRemovedCellsEdge_IsValid()
        {
            // Cell (1,1) removed: the Top side of (1,2) is a wall, a gate at depth 2 may stand there.
            var issues = LevelValidator.Validate(4, 4, 60,
                new[] { Block("A", 3, 3, BlockColor.Red, (0, 0)) },
                new[] { Gate(BoardEdge.Top, 1, 1, BlockColor.Red, depth: 2) },
                new[] { new CellCoord(1, 1) });

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void InnerGateWithBoardOnBothSides_IsError()
        {
            var issues = LevelValidator.Validate(4, 4, 60,
                new[] { Block("A", 3, 3, BlockColor.Red, (0, 0)) },
                new[] { Gate(BoardEdge.Top, 1, 1, BlockColor.Red, depth: 2) });

            Assert.That(issues.Single(i => i.IsError).GateIndex, Is.EqualTo(0));
        }

        [Test]
        public void GateOnRemovedCell_IsError()
        {
            var issues = LevelValidator.Validate(4, 4, 60,
                new[] { Block("A", 2, 2, BlockColor.Red, (0, 0)) },
                new[] { Gate(BoardEdge.Left, 0, 2, BlockColor.Red) },
                new[] { new CellCoord(0, 1) });

            Assert.That(issues.Single(i => i.IsError).GateIndex, Is.EqualTo(0));
        }

        [Test]
        public void RemovedCellOffBoard_IsError()
        {
            var issues = LevelValidator.Validate(4, 4, 60,
                new[] { Block("A", 0, 0, BlockColor.Red, (0, 0)) },
                new[] { Gate(BoardEdge.Left, 0, 1, BlockColor.Red) },
                new[] { new CellCoord(4, 0) });

            Assert.That(issues.Single(i => i.IsError).Message, Does.Contain("off the board"));
        }
    }
}

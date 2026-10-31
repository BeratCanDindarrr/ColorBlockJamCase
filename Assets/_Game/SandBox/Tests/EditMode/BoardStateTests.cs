using ColorBlockJam.Core.Level;
using System.Collections.Generic;
using NUnit.Framework;

namespace ColorBlockJam.SandBox.Board.Tests
{
    public class BoardStateTests
    {
        [Test]
        public void BlockState_OccupiedCells_AddsOffsetsToOrigin()
        {
            var data = new BlockData
            {
                Id = "A",
                Col = 2,
                Row = 3,
                Cells = new[] { new CellCoord(0, 0), new CellCoord(1, 0) },
                Color = BlockColor.Red
            };
            var block = new BlockState(data);

            var occupied = new List<CellCoord>(block.OccupiedCells(block.Origin));

            CollectionAssert.AreEquivalent(new[] { new CellCoord(2, 3), new CellCoord(3, 3) }, occupied);
        }
    }
}

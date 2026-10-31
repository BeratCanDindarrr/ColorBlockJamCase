using ColorBlockJam.Core.Level;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ColorBlockJam.SandBox.Board.Tests")]

namespace ColorBlockJam.SandBox.Board
{
    public sealed class BlockState
    {
        public string Id { get; }
        public BlockColor Color { get; }
        public CellCoord Origin { get; private set; }
        // Cell offsets from Origin that make up this block's footprint.
        public IReadOnlyList<CellCoord> Cells { get; }

        public bool IsExiting { get; internal set; }
        public GateState ExitGate { get; internal set; }

        public BlockState(BlockData data)
        {
            Id = data.Id;
            Color = data.Color;
            Origin = new CellCoord(data.Col, data.Row);
            Cells = (CellCoord[])(data.Cells ?? Array.Empty<CellCoord>()).Clone();
        }

        internal void MoveTo(CellCoord origin) => Origin = origin;

        // The board cells this block would occupy if its origin were at the given cell.
        public IEnumerable<CellCoord> OccupiedCells(CellCoord origin)
        {
            for (var i = 0; i < Cells.Count; i++)
                yield return origin + Cells[i];
        }
    }

    public sealed class GateState
    {
        public BoardEdge Edge { get; }
        public int Index { get; }
        public int Length { get; }
        public BlockColor Color { get; }

        public GateState(GateData data)
        {
            Edge = data.Edge;
            Index = data.Index;
            Length = data.Length;
            Color = data.Color;
        }
    }
}

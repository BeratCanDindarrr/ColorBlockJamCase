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
        public IReadOnlyList<CellCoord> Cells { get; private set; }

        // Frozen (cannot be picked up) until this many more blocks have left the board.
        public int Ice { get; internal set; }
        public BlockAxis Axis { get; }
        public bool IsExiting { get; internal set; }
        public GateState ExitGate { get; internal set; }

        public BlockState(BlockData data)
        {
            Id = data.Id;
            Color = data.Color;
            Origin = new CellCoord(data.Col, data.Row);
            Ice = Math.Max(0, data.Ice);
            Axis = data.Axis;
            Cells = (CellCoord[])(data.Cells ?? Array.Empty<CellCoord>()).Clone();
        }

        internal void MoveTo(CellCoord origin) => Origin = origin;

        // Rocket: the block's footprint after a piece broke off (offsets from the same Origin).
        internal void Reshape(IEnumerable<CellCoord> cells) => Cells = new List<CellCoord>(cells).ToArray();

        // The finger's target with the locked axis held at the block's current position.
        public BoardPoint LockToAxis(BoardPoint current, BoardPoint target)
        {
            switch (Axis)
            {
                case BlockAxis.Horizontal: return new BoardPoint(target.Col, current.Row);
                case BlockAxis.Vertical: return new BoardPoint(current.Col, target.Row);
                default: return target;
            }
        }

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
        // Cells in from the outer edge; 0 is the outer wall (see GateGeometry).
        public int Depth { get; }

        public GateState(GateData data)
        {
            Depth = data.Depth;
            Edge = data.Edge;
            Index = data.Index;
            Length = data.Length;
            Color = data.Color;
        }
    }
}

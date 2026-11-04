using System;

namespace ColorBlockJam.Core.Level
{
    public enum BlockColor
    {
        Red = 0,
        Blue = 1,
        Green = 2,
        Yellow = 3,
        Purple = 4,
        Orange = 5,
        Pink = 6,
        Cyan = 7
    }

    public enum BoardEdge
    {
        Top = 0,
        Bottom = 1,
        Left = 2,
        Right = 3
    }

    // The one direction a block may slide in; Free moves both ways.
    public enum BlockAxis
    {
        Free = 0,
        Horizontal = 1,
        Vertical = 2
    }

    [Serializable]
    public struct CellCoord : IEquatable<CellCoord>
    {
        public int Col;
        public int Row;

        public CellCoord(int col, int row)
        {
            Col = col;
            Row = row;
        }

        public static CellCoord operator +(CellCoord a, CellCoord b) => new CellCoord(a.Col + b.Col, a.Row + b.Row);
        public static bool operator ==(CellCoord a, CellCoord b) => a.Equals(b);
        public static bool operator !=(CellCoord a, CellCoord b) => !a.Equals(b);

        public bool Equals(CellCoord other) => Col == other.Col && Row == other.Row;
        public override bool Equals(object obj) => obj is CellCoord other && Equals(other);
        public override int GetHashCode() => (Col * 397) ^ Row;
        public override string ToString() => $"({Col},{Row})";
    }

    [Serializable]
    public class BlockData
    {
        public string Id;
        public int Col;
        public int Row;
        public CellCoord[] Cells;
        public BlockColor Color;
        // Frozen until this many blocks have left through gates; 0 = no ice.
        public int Ice;
        public BlockAxis Axis;
    }

    // A gate in a wall: every cell from Index to Index + Length - 1 along the wall accepts a block of its Color.
    // Depth 0 is the board's outer wall on Edge; a deeper gate sits that many cells further in, on the wall between
    // board cells and removed cells (see GateGeometry).
    [Serializable]
    public class GateData
    {
        public BoardEdge Edge;
        public int Index;
        public int Length;
        public BlockColor Color;
        public int Depth;
    }
}

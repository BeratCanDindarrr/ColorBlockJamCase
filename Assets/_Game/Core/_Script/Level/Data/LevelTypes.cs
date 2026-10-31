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
    }

    // A gate on one wall of the board: every cell from Index to Index + Length - 1 along that
    // edge accepts a block of its Color.
    [Serializable]
    public class GateData
    {
        public BoardEdge Edge;
        public int Index;
        public int Length;
        public BlockColor Color;
    }
}

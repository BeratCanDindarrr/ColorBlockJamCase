using System;

namespace ColorBlockJam.SandBox.Board
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

    public enum BoardStatus
    {
        Playing = 0,
        Won = 1,
        Lost = 2
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

    // A point on the board in fractional coordinates, used while a block is being dragged
    // between cells. Row 0 is the top row; +Row moves down.
    public readonly struct BoardPoint
    {
        public readonly double Col;
        public readonly double Row;

        public BoardPoint(double col, double row)
        {
            Col = col;
            Row = row;
        }

        public static BoardPoint operator +(BoardPoint a, BoardPoint b) => new BoardPoint(a.Col + b.Col, a.Row + b.Row);
        public static BoardPoint operator -(BoardPoint a, BoardPoint b) => new BoardPoint(a.Col - b.Col, a.Row - b.Row);
        public static BoardPoint From(CellCoord cell) => new BoardPoint(cell.Col, cell.Row);

        public override string ToString() => $"({Col},{Row})";
    }
}

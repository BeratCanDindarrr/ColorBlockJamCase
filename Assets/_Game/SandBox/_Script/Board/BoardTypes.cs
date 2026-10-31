using ColorBlockJam.Core.Level;

namespace ColorBlockJam.SandBox.Board
{
    public enum BoardStatus
    {
        Playing = 0,
        Won = 1,
        Lost = 2
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

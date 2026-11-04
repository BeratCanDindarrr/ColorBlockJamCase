using ColorBlockJam.Core.Level;

namespace ColorBlockJam.SandBox.Board
{
    public enum BoardEventType
    {
        DragStarted,
        DragEnded,
        // Block was pushed into Gate and left the board; its view slides out through the gate.
        BlockExitStarted,
        // A booster (Hammer / Vacuum) took the block off the board; it counts as an exit, its view breaks apart in place.
        BlockDestroyed,
        // Rocket broke the block's piece on Cell; the block kept its other cells (Block.Cells is the new shape).
        BlockBroken,
        // A broken block fell apart: Block is a new block made of cells that no longer touch the rest.
        BlockAdded,
        // A frozen block's ice count went down (Block.Ice is the new count; 0 = thawed).
        IceChanged,
        Won,
        Lost
    }

    public readonly struct BoardEvent
    {
        public readonly BoardEventType Type;
        public readonly BlockState Block;
        public readonly GateState Gate;
        // The board cell a BlockBroken event broke.
        public readonly CellCoord Cell;

        public BoardEvent(BoardEventType type, BlockState block = null, GateState gate = null, CellCoord cell = default)
        {
            Type = type;
            Block = block;
            Gate = gate;
            Cell = cell;
        }
    }
}

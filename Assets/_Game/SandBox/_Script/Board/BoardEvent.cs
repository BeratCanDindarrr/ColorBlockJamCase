namespace ColorBlockJam.SandBox.Board
{
    public enum BoardEventType
    {
        DragStarted,
        DragEnded,
        // Block was pushed into Gate and left the board; its view slides out through the gate.
        BlockExitStarted,
        Won,
        Lost
    }

    public readonly struct BoardEvent
    {
        public readonly BoardEventType Type;
        public readonly BlockState Block;
        public readonly GateState Gate;

        public BoardEvent(BoardEventType type, BlockState block = null, GateState gate = null)
        {
            Type = type;
            Block = block;
            Gate = gate;
        }
    }
}

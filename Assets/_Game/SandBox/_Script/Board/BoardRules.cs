namespace ColorBlockJam.SandBox.Board
{
    public sealed class BoardRules
    {
        // How far off a lane, in cells, a blocked block may be pulled into it.
        public double AssistRange { get; }
        // The level's clock stands still until the player first lifts a block.
        public bool ClockWaitsForFirstMove { get; }
        // How far, in cells, the finger has to push a fitting block past the wall before it exits.
        public double PushThreshold { get; }

        public BoardRules(double assistRange, bool clockWaitsForFirstMove, double pushThreshold)
        {
            AssistRange = assistRange;
            ClockWaitsForFirstMove = clockWaitsForFirstMove;
            PushThreshold = pushThreshold;
        }
    }
}

namespace ColorBlockJam.SandBox.Board
{
    public sealed class BoardRules
    {
        // How far off a lane, in cells, a blocked block may be pulled into it.
        public double AssistRange { get; }
        // The longest step one Tick advances exits by; the clock always takes the full delta.
        public double MaxFrameDelta { get; }
        // The level's clock stands still until the player first lifts a block.
        public bool ClockWaitsForFirstMove { get; }
        // How far, in cells, the finger has to push a fitting block past the wall before it exits.
        public double PushThreshold { get; }
        // How long an exiting block slides through its gate before it is removed.
        public double ExitSeconds { get; }

        public BoardRules(double assistRange, double maxFrameDelta, bool clockWaitsForFirstMove,
            double pushThreshold, double exitSeconds)
        {
            AssistRange = assistRange;
            MaxFrameDelta = maxFrameDelta;
            ClockWaitsForFirstMove = clockWaitsForFirstMove;
            PushThreshold = pushThreshold;
            ExitSeconds = exitSeconds;
        }
    }
}

using System;
using System.Collections.Generic;

namespace ColorBlockJam.SandBox.Board
{
    // Rules of the board: the player drags blocks; one pushed into a same-colour gate it fits slides out
    // and is removed. Clear every block before the clock runs out. State is owned here and exposed read-only.
    public sealed class BoardSimulation
    {
        private readonly BoardRules _rules;
        // Shared with _movement, so removed blocks stop being obstacles there too.
        private readonly List<BlockState> _blocks;
        private readonly List<GateState> _gates;
        private readonly BoardMovement _movement;
        private readonly List<BoardEvent> _events = new List<BoardEvent>();
        // Events raised outside Tick (drag start/end, exit start); the next Tick reports them first.
        private readonly List<BoardEvent> _pendingEvents = new List<BoardEvent>();

        public int Cols { get; }
        public int Rows { get; }
        public BoardStatus Status { get; private set; }
        public double TimeLeft { get; private set; }
        // False while the clock still waits for the player's first move.
        public bool IsClockRunning { get; private set; }
        public IReadOnlyList<BlockState> Blocks => _blocks;
        public IReadOnlyList<GateState> Gates => _gates;
        public BlockState DraggedBlock { get; private set; }
        public BoardPoint DragPosition { get; private set; }

        public BoardSimulation(BoardLevelDefinition level, BoardRules rules)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));

            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Cols = level.Cols;
            Rows = level.Rows;
            TimeLeft = level.TimeLimitSeconds;
            IsClockRunning = !rules.ClockWaitsForFirstMove;
            Status = BoardStatus.Playing;

            _blocks = new List<BlockState>(level.Blocks.Count);
            for (var i = 0; i < level.Blocks.Count; i++)
                _blocks.Add(new BlockState(level.Blocks[i]));

            _gates = new List<GateState>(level.Gates.Count);
            for (var i = 0; i < level.Gates.Count; i++)
                _gates.Add(new GateState(level.Gates[i]));

            _movement = new BoardMovement(_blocks, Cols, Rows, rules.AssistRange);
        }

        public BlockState BlockAt(BoardPoint point) => _movement.BlockAt(point.Col, point.Row);

        public bool BeginDrag(BlockState block)
        {
            if (Status != BoardStatus.Playing || DraggedBlock != null || block == null || block.IsExiting ||
                !_blocks.Contains(block))
                return false;

            DraggedBlock = block;
            DragPosition = BoardPoint.From(block.Origin);
            IsClockRunning = true;
            _pendingEvents.Add(new BoardEvent(BoardEventType.DragStarted, block));
            return true;
        }

        // Slides the held block toward the target as far as walls and other blocks allow, then lets it
        // exit if the finger pushes on past a gate it fits.
        public BoardPoint MoveDrag(BoardPoint target)
        {
            if (DraggedBlock == null)
                return DragPosition;

            DragPosition = _movement.SlideBlock(DraggedBlock, DragPosition, target);
            var position = DragPosition;
            TryStartExit(target);
            return position;
        }

        // Where the held block would land if released now.
        public CellCoord PreviewDrop()
        {
            if (DraggedBlock == null)
                return default;

            return _movement.Snap(DraggedBlock, DragPosition, DraggedBlock.Origin);
        }

        public void EndDrag()
        {
            if (DraggedBlock == null)
                return;

            var block = DraggedBlock;
            block.MoveTo(PreviewDrop());
            ClearDrag();
            _pendingEvents.Add(new BoardEvent(BoardEventType.DragEnded, block));
        }

        // Reports queued events, then advances exits and the clock. The returned list is reused; it is
        // only valid until the next Tick.
        public IReadOnlyList<BoardEvent> Tick(double deltaTime)
        {
            _events.Clear();
            if (Status != BoardStatus.Playing)
                return _events;

            _events.AddRange(_pendingEvents);
            _pendingEvents.Clear();

            var seconds = double.IsNaN(deltaTime) || double.IsInfinity(deltaTime) ? 0 : Math.Max(deltaTime, 0);
            AdvanceExits(Math.Min(seconds, _rules.MaxFrameDelta));
            if (_blocks.Count == 0)
            {
                EndLevel(BoardStatus.Won);
                return _events;
            }

            AdvanceClock(seconds);
            return _events;
        }

        // The finger's target lies past the wall the held block stands against: the gate it fits that is
        // pushed into furthest, by at least the threshold, takes it.
        private void TryStartExit(BoardPoint target)
        {
            var block = DraggedBlock;
            var drop = PreviewDrop();
            GateState best = null;
            var bestPush = _rules.PushThreshold;
            for (var i = 0; i < _gates.Count; i++)
            {
                var gate = _gates[i];
                if (!GateFit.Fits(block, drop, gate, Cols, Rows))
                    continue;

                var push = GateFit.Push(DragPosition, target, gate.Edge);
                if (push < bestPush)
                    continue;

                best = gate;
                bestPush = push;
            }

            if (best == null)
                return;

            block.MoveTo(drop);
            block.IsExiting = true;
            block.ExitGate = best;
            block.ExitElapsed = 0;
            ClearDrag();
            _pendingEvents.Add(new BoardEvent(BoardEventType.BlockExitStarted, block, best));
        }

        private void AdvanceExits(double step)
        {
            for (var i = 0; i < _blocks.Count; i++)
            {
                var block = _blocks[i];
                if (!block.IsExiting)
                    continue;

                block.ExitElapsed += step;
                if (block.ExitElapsed < _rules.ExitSeconds)
                    continue;

                _blocks.RemoveAt(i);
                i--;
                _events.Add(new BoardEvent(BoardEventType.BlockRemoved, block, block.ExitGate));
            }
        }

        // Running out of time loses the level, unless every block left is already exiting: that board
        // is cleared and wins once they are gone.
        private void AdvanceClock(double seconds)
        {
            if (!IsClockRunning)
                return;

            TimeLeft = Math.Max(0, TimeLeft - seconds);
            if (TimeLeft <= 0 && HasPlacedBlock())
                EndLevel(BoardStatus.Lost);
        }

        private bool HasPlacedBlock()
        {
            for (var i = 0; i < _blocks.Count; i++)
                if (!_blocks[i].IsExiting)
                    return true;
            return false;
        }

        private void ClearDrag()
        {
            DraggedBlock = null;
            DragPosition = default;
        }

        // Nothing runs once the level is over, so the held block is let go.
        private void EndLevel(BoardStatus status)
        {
            Status = status;
            ClearDrag();
            _pendingEvents.Clear();
            _events.Add(new BoardEvent(status == BoardStatus.Won ? BoardEventType.Won : BoardEventType.Lost));
        }
    }
}

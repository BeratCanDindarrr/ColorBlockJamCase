using ColorBlockJam.Core.Level;
using System;
using System.Collections.Generic;

namespace ColorBlockJam.SandBox.Board
{
    // Rules of the board: the player drags blocks; one pushed into a same-colour gate it fits slides out
    // and is removed. Clear every block before the clock runs out. State is owned here and exposed read-only.
    public sealed partial class BoardSimulation
    {
        private readonly BoardRules _rules;
        // Shared with _movement, so removed blocks stop being obstacles there too.
        private readonly List<BlockState> _blocks;
        private readonly List<GateState> _gates;
        private readonly BoardMovement _movement;
        private readonly List<BoardEvent> _events = new List<BoardEvent>();
        // Events raised since the last TakeEvents.
        private readonly List<BoardEvent> _pendingEvents = new List<BoardEvent>();
        // Numbers the blocks a Rocket split off, so their ids stay unique.
        private int _splitCount;

        public int Cols { get; }
        public int Rows { get; }
        public BoardStatus Status { get; private set; }
        public double TimeLeft { get; private set; }
        // False while the clock still waits for the player's first move.
        public bool IsClockRunning { get; private set; }
        // Seconds the clock still stands still for (Timer booster); counted down before TimeLeft.
        public double FrozenTimeLeft { get; private set; }
        public bool IsClockFrozen => FrozenTimeLeft > 0;
        public IReadOnlyList<BlockState> Blocks => _blocks;
        public IReadOnlyList<GateState> Gates => _gates;
        // Removed board cells.
        public IReadOnlyCollection<CellCoord> Holes { get; }
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

            Holes = level.Holes;
            _movement = new BoardMovement(_blocks, Cols, Rows, rules.AssistRange, Holes);
        }

        public BlockState BlockAt(BoardPoint point) => _movement.BlockAt(point.Col, point.Row);

        public bool BeginDrag(BlockState block)
        {
            if (Status != BoardStatus.Playing || DraggedBlock != null || block == null || block.IsExiting ||
                block.Ice > 0 ||
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

            target = DraggedBlock.LockToAxis(DragPosition, target);
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

        // Hands over the events raised since the last call. The returned list is reused; it is only valid until the
        // next call.
        public IReadOnlyList<BoardEvent> TakeEvents()
        {
            _events.Clear();
            _events.AddRange(_pendingEvents);
            _pendingEvents.Clear();
            return _events;
        }

        // Runs the level clock down; running out of time with blocks left loses the level.
        public void AdvanceClock(double seconds)
        {
            if (Status != BoardStatus.Playing || !IsClockRunning)
                return;

            var passed = Sanitize(seconds);
            var frozen = Math.Min(FrozenTimeLeft, passed);
            FrozenTimeLeft -= frozen;
            TimeLeft = Math.Max(0, TimeLeft - (passed - frozen));
            if (TimeLeft <= 0)
                EndLevel(BoardStatus.Lost);
        }

        // Stops the running clock for the given seconds (Timer booster). Refused before the first move, once the level is
        // over, or while it is already frozen.
        public bool FreezeClock(double seconds)
        {
            if (Status != BoardStatus.Playing || !IsClockRunning || IsClockFrozen || Sanitize(seconds) <= 0)
                return false;

            FrozenTimeLeft = seconds;
            return true;
        }

        // Puts more time on the clock ("Play On"); a level lost on time resumes where it stopped.
        public void AddTime(double seconds)
        {
            if (Status == BoardStatus.Won || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0)
                return;

            TimeLeft += seconds;
            if (Status == BoardStatus.Lost)
                Status = BoardStatus.Playing;
        }

        private static double Sanitize(double seconds) =>
            double.IsNaN(seconds) || double.IsInfinity(seconds) ? 0 : Math.Max(seconds, 0);

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
                if (gate.Ice > 0 || !GateFit.Fits(block, drop, gate, Cols, Rows))
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
            block.ExitGate = best;
            ClearDrag();
            LeaveBoard(block, new BoardEvent(BoardEventType.BlockExitStarted, block, best));
        }

        // Hammer / Vacuum: the block leaves as if through a gate (cracks ice, may win). Not for an iced or held block.
        public bool RemoveBlock(BlockState block)
        {
            if (Status != BoardStatus.Playing || block == null || block.IsExiting || block.Ice > 0 ||
                block == DraggedBlock || !_blocks.Contains(block))
                return false;

            LeaveBoard(block, new BoardEvent(BoardEventType.BlockDestroyed, block));
            return true;
        }

        // Rocket: breaks the block's piece on the given board cell. A one-cell block leaves like a gate exit; otherwise the
        // block keeps its other cells, and groups of them that no longer touch become separate blocks of its colour.
        // Not for an iced or held block, nor a cell the block does not cover.
        public bool BreakCell(BlockState block, CellCoord cell)
        {
            if (Status != BoardStatus.Playing || block == null || block.IsExiting || block.Ice > 0 ||
                block == DraggedBlock || !_blocks.Contains(block))
                return false;

            var offset = new CellCoord(cell.Col - block.Origin.Col, cell.Row - block.Origin.Row);
            var rest = new List<CellCoord>(block.Cells);
            if (!rest.Remove(offset))
                return false;

            if (rest.Count == 0)
            {
                LeaveBoard(block, new BoardEvent(BoardEventType.BlockDestroyed, block));
                return true;
            }

            var parts = SplitConnected(rest);
            block.Reshape(parts[0]);
            _pendingEvents.Add(new BoardEvent(BoardEventType.BlockBroken, block, cell: cell));
            for (var i = 1; i < parts.Count; i++)
            {
                var piece = new BlockState(new BlockData
                {
                    Id = $"{block.Id}~{++_splitCount}", Col = block.Origin.Col, Row = block.Origin.Row,
                    Cells = parts[i].ToArray(), Color = block.Color, Axis = block.Axis
                });
                _blocks.Add(piece);
                _pendingEvents.Add(new BoardEvent(BoardEventType.BlockAdded, piece));
            }

            return true;
        }

        // Groups of cells joined side to side.
        private static List<List<CellCoord>> SplitConnected(List<CellCoord> cells)
        {
            var left = new HashSet<CellCoord>(cells);
            var parts = new List<List<CellCoord>>();
            var stack = new Stack<CellCoord>();
            foreach (var start in cells)
            {
                if (!left.Remove(start)) continue;

                var part = new List<CellCoord>();
                stack.Push(start);
                while (stack.Count > 0)
                {
                    var cell = stack.Pop();
                    part.Add(cell);
                    foreach (var next in Neighbours(cell))
                    {
                        if (left.Remove(next)) stack.Push(next);
                    }
                }

                parts.Add(part);
            }

            return parts;
        }

        private static IEnumerable<CellCoord> Neighbours(CellCoord cell)
        {
            yield return new CellCoord(cell.Col + 1, cell.Row);
            yield return new CellCoord(cell.Col - 1, cell.Row);
            yield return new CellCoord(cell.Col, cell.Row + 1);
            yield return new CellCoord(cell.Col, cell.Row - 1);
        }

        // The rules are done with a leaving block at once; how it goes is the view's animation.
        private void LeaveBoard(BlockState block, BoardEvent leaveEvent)
        {
            block.IsExiting = true;
            _blocks.Remove(block);
            _pendingEvents.Add(leaveEvent);
            CrackIce();
            if (_blocks.Count == 0)
                EndLevel(BoardStatus.Won);
        }

        // Every block that leaves takes one off each frozen block's and each iced gate's count.
        private void CrackIce()
        {
            for (var i = 0; i < _blocks.Count; i++)
            {
                var block = _blocks[i];
                if (block.Ice == 0)
                    continue;

                block.Ice--;
                _pendingEvents.Add(new BoardEvent(BoardEventType.IceChanged, block));
            }

            for (var i = 0; i < _gates.Count; i++)
            {
                var gate = _gates[i];
                if (gate.Ice == 0)
                    continue;

                gate.Ice--;
                _pendingEvents.Add(new BoardEvent(BoardEventType.IceChanged, gate: gate));
            }
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
            _pendingEvents.Add(new BoardEvent(status == BoardStatus.Won ? BoardEventType.Won : BoardEventType.Lost));
        }
    }
}

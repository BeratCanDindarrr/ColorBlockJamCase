using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColorBlockJam.Core.Level;

namespace ColorBlockJam.SandBox.Board
{
    public enum SolveVerdict
    {
        Solvable,
        // Some blocks can never leave, however the others are moved (StuckBlocks).
        Unsolvable,
        // The search stopped at its state limit before deciding.
        TooBig
    }

    // One drag: the block walks cell by cell along Path (From first, To last); when Exits, it is pushed on from there
    // through a gate on ExitEdge and leaves.
    public readonly struct SolveStep
    {
        public string BlockId { get; }
        public CellCoord From { get; }
        public CellCoord To { get; }
        public bool Exits { get; }
        public IReadOnlyList<CellCoord> Path { get; }
        public BoardEdge ExitEdge { get; }

        public SolveStep(string blockId, IReadOnlyList<CellCoord> path, bool exits, BoardEdge exitEdge = default)
        {
            BlockId = blockId;
            Path = path;
            From = path[0];
            To = path[path.Count - 1];
            Exits = exits;
            ExitEdge = exitEdge;
        }
    }

    public sealed class SolveResult
    {
        public SolveVerdict Verdict { get; }
        // The drags played, in order (up to where it got stuck, when it did).
        public IReadOnlyList<SolveStep> Steps { get; }
        public IReadOnlyList<string> StuckBlocks { get; }
        public int StatesExplored { get; }

        public SolveResult(SolveVerdict verdict, IReadOnlyList<SolveStep> steps, IReadOnlyList<string> stuckBlocks,
            int statesExplored)
        {
            Verdict = verdict;
            Steps = steps;
            StuckBlocks = stuckBlocks;
            StatesExplored = statesExplored;
        }
    }

    // Plays a level like a player, with the board's own movement and gate (GateFit) rules: whenever some
    // block can walk to a gate it fits, it leaves; otherwise the fewest drags that move blocks aside until one can are
    // searched for. Leaving only ever frees room and thaws ice, and a drag can always be walked back, so a level solvable
    // at the start can never be locked by the order it is played in, and taking any exit that is open is safe.
    public static class LevelSolver
    {
        // Enough for the levels made so far; a level that runs past it is reported TooBig rather than freezing the editor.
        public const int DefaultMaxStates = 50000;

        private static readonly CellCoord[] Steps =
            { new CellCoord(1, 0), new CellCoord(-1, 0), new CellCoord(0, 1), new CellCoord(0, -1) };

        public static SolveResult Solve(BoardLevelDefinition level, int maxStates = DefaultMaxStates)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));

            var board = new Board(level);
            var steps = new List<SolveStep>();
            var explored = 0;
            while (board.PresentCount > 0)
            {
                var found = FindNextExit(board, maxStates - explored, ref explored, out var path, out var exhausted);
                if (!found)
                {
                    var verdict = exhausted ? SolveVerdict.Unsolvable : SolveVerdict.TooBig;
                    return new SolveResult(verdict, steps, board.PresentIds(), explored);
                }

                steps.AddRange(path);
                board.Commit(path);
            }

            return new SolveResult(SolveVerdict.Solvable, steps, Array.Empty<string>(), explored);
        }

        // Breadth-first over arrangements of the blocks now on the board, nearest first, until one lets a block leave.
        // `exhausted` is true when every arrangement was tried (none does), false when the state budget ran out.
        private static bool FindNextExit(Board board, int budget, ref int explored, out List<SolveStep> path,
            out bool exhausted)
        {
            var start = board.Origins();
            var parents = new Dictionary<string, (string Parent, SolveStep Step)> { [Key(start)] = (null, default) };
            var queue = new Queue<CellCoord[]>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                if (budget-- <= 0)
                {
                    path = null;
                    exhausted = false;
                    return false;
                }

                explored++;
                var state = queue.Dequeue();
                var stateKey = Key(state);
                board.Arrange(state);
                var reach = new List<(int Block, List<CellCoord> Places, Dictionary<CellCoord, CellCoord> CameFrom)>();
                foreach (var i in board.Movable())
                {
                    var places = board.Reachable(i, out var cameFrom);
                    foreach (var place in places)
                    {
                        var gate = board.GateFrom(i, place);
                        if (gate == null) continue;
                        path = Rebuild(parents, stateKey);
                        path.Add(new SolveStep(board.Id(i), Walk(cameFrom, place), true, gate.Edge));
                        exhausted = false;
                        return true;
                    }

                    reach.Add((i, places, cameFrom));
                }

                foreach (var (i, places, cameFrom) in reach)
                {
                    foreach (var place in places)
                    {
                        if (place == state[i]) continue;
                        var next = (CellCoord[])state.Clone();
                        next[i] = place;
                        var key = Key(next);
                        if (parents.ContainsKey(key)) continue;
                        parents[key] = (stateKey, new SolveStep(board.Id(i), Walk(cameFrom, place), false));
                        queue.Enqueue(next);
                    }
                }
            }

            path = null;
            exhausted = true;
            return false;
        }

        private static List<SolveStep> Rebuild(Dictionary<string, (string Parent, SolveStep Step)> parents, string key)
        {
            var path = new List<SolveStep>();
            while (parents[key].Parent != null)
            {
                path.Add(parents[key].Step);
                key = parents[key].Parent;
            }

            path.Reverse();
            return path;
        }

        // The cells walked from the start of a Reachable search to `end`, start first.
        private static List<CellCoord> Walk(Dictionary<CellCoord, CellCoord> cameFrom, CellCoord end)
        {
            var path = new List<CellCoord> { end };
            while (cameFrom[end] != end)
            {
                end = cameFrom[end];
                path.Add(end);
            }

            path.Reverse();
            return path;
        }

        private static string Key(CellCoord[] origins)
        {
            var key = new StringBuilder(origins.Length * 6);
            foreach (var origin in origins) key.Append(origin.Col).Append(',').Append(origin.Row).Append(';');
            return key.ToString();
        }

        // The board while solving: the game's BlockState / GateState, with blocks that left marked as exiting and ice
        // cracked by one per block gone, as in BoardSimulation. Placement uses a cell grid of who stands where (the same
        // test as BoardMovement.IsPlacementFree for whole cells, but a lookup per cell instead of a scan of every block,
        // which the search does millions of times).
        private sealed class Board
        {
            private const int Empty = -1;
            private const int Hole = -2;

            private readonly List<BlockState> _blocks;
            private readonly int[] _ice;
            private readonly bool[] _gone;
            private readonly List<GateState> _gates;
            private readonly int[] _grid;
            private readonly int[] _holeGrid;
            private readonly int _cols;
            private readonly int _rows;
            private int _exits;

            public Board(BoardLevelDefinition level)
            {
                _cols = level.Cols;
                _rows = level.Rows;
                _blocks = level.Blocks.Select(b => new BlockState(b)).ToList();
                _ice = _blocks.Select(b => b.Ice).ToArray();
                _gone = new bool[_blocks.Count];
                _gates = level.Gates.Select(g => new GateState(g)).ToList();
                _holeGrid = Enumerable.Repeat(Empty, _cols * _rows).ToArray();
                foreach (var hole in level.Holes)
                    if (Inside(hole.Col, hole.Row)) _holeGrid[hole.Row * _cols + hole.Col] = Hole;
                _grid = (int[])_holeGrid.Clone();
                Fill();
            }

            public int PresentCount => _gone.Count(g => !g);

            public string Id(int i) => _blocks[i].Id;

            public IReadOnlyList<string> PresentIds() =>
                Enumerable.Range(0, _blocks.Count).Where(i => !_gone[i]).Select(Id).ToList();

            // Every block's origin (gone blocks keep where they left; they are not in the way).
            public CellCoord[] Origins() => _blocks.Select(b => b.Origin).ToArray();

            public void Arrange(CellCoord[] origins)
            {
                for (var i = 0; i < _blocks.Count; i++) _blocks[i].MoveTo(origins[i]);
                Fill();
            }

            private void Fill()
            {
                Array.Copy(_holeGrid, _grid, _grid.Length);
                for (var i = 0; i < _blocks.Count; i++)
                {
                    if (_gone[i]) continue;
                    var block = _blocks[i];
                    foreach (var cell in block.Cells)
                    {
                        var col = block.Origin.Col + cell.Col;
                        var row = block.Origin.Row + cell.Row;
                        if (Inside(col, row)) _grid[row * _cols + col] = i;
                    }
                }
            }

            private bool Inside(int col, int row) => col >= 0 && row >= 0 && col < _cols && row < _rows;

            // Every cell of block i at `origin` is on the board and empty or already its own.
            private bool IsFree(int i, CellCoord origin)
            {
                foreach (var cell in _blocks[i].Cells)
                {
                    var col = origin.Col + cell.Col;
                    var row = origin.Row + cell.Row;
                    if (!Inside(col, row)) return false;
                    var owner = _grid[row * _cols + col];
                    if (owner != Empty && owner != i) return false;
                }

                return true;
            }

            // Blocks on the board that can be picked up: not gone, not iced.
            public IEnumerable<int> Movable() =>
                Enumerable.Range(0, _blocks.Count).Where(i => !_gone[i] && _blocks[i].Ice == 0);

            // Every origin the block can be dragged to, a cell at a time along its axis, its own included, nearest first;
            // cameFrom holds the cell each was reached from (the start points at itself).
            public List<CellCoord> Reachable(int i, out Dictionary<CellCoord, CellCoord> cameFrom)
            {
                var block = _blocks[i];
                var places = new List<CellCoord> { block.Origin };
                cameFrom = new Dictionary<CellCoord, CellCoord> { [block.Origin] = block.Origin };
                for (var k = 0; k < places.Count; k++)
                {
                    foreach (var step in Steps)
                    {
                        var next = places[k] + step;
                        if (!GateFit.AlongAxis(block.Axis, step) || cameFrom.ContainsKey(next) || !IsFree(i, next))
                            continue;
                        cameFrom[next] = places[k];
                        places.Add(next);
                    }
                }

                return places;
            }

            // The gate the block can be pushed into along its axis from `origin` (GateFit.CanPushInto), or null.
            public GateState GateFrom(int i, CellCoord origin)
            {
                var block = _blocks[i];
                foreach (var gate in _gates)
                {
                    if (GateFit.CanPushInto(block, origin, gate, _cols, _rows)) return gate;
                }

                return null;
            }

            // Plays the found drags for real: the moves, then the block that left.
            public void Commit(List<SolveStep> path)
            {
                foreach (var step in path)
                {
                    // Ids are unique in a valid level (LevelValidator).
                    var i = _blocks.FindIndex(b => b.Id == step.BlockId);
                    _blocks[i].MoveTo(step.To);
                    if (!step.Exits) continue;

                    _gone[i] = true;
                    _blocks[i].IsExiting = true;
                    _exits++;
                    for (var k = 0; k < _blocks.Count; k++) _blocks[k].Ice = Math.Max(0, _ice[k] - _exits);
                }

                Fill();
            }
        }
    }
}

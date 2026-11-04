using System.Collections.Generic;
using ColorBlockJam.Core.Level;

namespace ColorBlockJam.SandBox.Board
{
    public sealed partial class BoardSimulation
    {
        private static readonly CellCoord[] Steps =
            { new CellCoord(1, 0), new CellCoord(-1, 0), new CellCoord(0, 1), new CellCoord(0, -1) };

        // The shortest walk, a cell at a time (the block's axis kept), from where the block lies to a place where it
        // fits a gate it can be pushed into, then one cell on into that gate. Starts with its origin; null when the
        // block cannot get out from here (the first-level tutorial hand follows it).
        public IReadOnlyList<CellCoord> RouteToGate(BlockState block)
        {
            if (block == null || block.IsExiting || !_blocks.Contains(block)) return null;

            var cameFrom = new Dictionary<CellCoord, CellCoord> { [block.Origin] = block.Origin };
            var queue = new Queue<CellCoord>();
            queue.Enqueue(block.Origin);
            while (queue.Count > 0)
            {
                var origin = queue.Dequeue();
                var gate = GateToPushInto(block, origin);
                if (gate != null) return Walk(cameFrom, origin, origin + GateFit.Normal(gate.Edge));

                foreach (var step in Steps)
                {
                    var next = origin + step;
                    if (!AlongAxis(block.Axis, step) || cameFrom.ContainsKey(next) ||
                        !_movement.IsPlacementFree(block, BoardPoint.From(next)))
                        continue;

                    cameFrom[next] = origin;
                    queue.Enqueue(next);
                }
            }

            return null;
        }

        private GateState GateToPushInto(BlockState block, CellCoord origin)
        {
            foreach (var gate in _gates)
            {
                if (AlongAxis(block.Axis, GateFit.Normal(gate.Edge)) && GateFit.Fits(block, origin, gate, Cols, Rows))
                    return gate;
            }

            return null;
        }

        private static bool AlongAxis(BlockAxis axis, CellCoord step)
        {
            switch (axis)
            {
                case BlockAxis.Horizontal: return step.Row == 0;
                case BlockAxis.Vertical: return step.Col == 0;
                default: return true;
            }
        }

        private static List<CellCoord> Walk(Dictionary<CellCoord, CellCoord> cameFrom, CellCoord end, CellCoord into)
        {
            var route = new List<CellCoord> { into };
            var cell = end;
            while (true)
            {
                route.Add(cell);
                var previous = cameFrom[cell];
                if (previous == cell) break;
                cell = previous;
            }

            route.Reverse();
            return route;
        }
    }
}

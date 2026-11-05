using System.Collections.Generic;
using System.Linq;
using ColorBlockJam.Core.Level;
using ColorBlockJam.Core.Settings;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Gate edits on a level: gates sit on the walls, Index counted along the wall (cols for Top/Bottom, rows for
    // Left/Right).
    internal static class LevelEditorGateOps
    {
        public static int GateAt(LevelData level, BoardEdge edge, int index, int depth)
        {
            var gates = level.Gates ?? new GateData[0];
            for (var i = 0; i < gates.Length; i++)
            {
                var gate = gates[i];
                if (gate.Edge == edge && gate.Depth == depth && index >= gate.Index && index < gate.Index + gate.Length)
                    return i;
            }

            return -1;
        }

        public static int Add(LevelData level, BoardEdge edge, int index, int depth, BlockColor color)
        {
            var gates = (level.Gates ?? new GateData[0]).ToList();
            gates.Add(new GateData { Edge = edge, Index = index, Length = 1, Color = color, Depth = depth });
            level.Gates = gates.ToArray();
            return gates.Count - 1;
        }

        public static void Delete(LevelData level, int index) => DeleteMany(level, new List<int> { index });

        // Takes one wall place out of a gate: an end shortens it, a middle place splits it in two (same colour, ice,
        // edge and depth), the only place deletes it. False when the slot is not the gate's.
        public static bool RemoveSlot(LevelData level, int index, int slot)
        {
            var gate = level.Gates[index];
            var end = gate.Index + gate.Length - 1;
            if (slot < gate.Index || slot > end) return false;

            if (gate.Length == 1)
            {
                Delete(level, index);
                return true;
            }

            if (slot == gate.Index || slot == end)
            {
                if (slot == gate.Index) gate.Index++;
                gate.Length--;
                return true;
            }

            var tail = new GateData
            {
                Edge = gate.Edge, Index = slot + 1, Length = end - slot, Color = gate.Color, Depth = gate.Depth, Ice = gate.Ice
            };
            gate.Length = slot - gate.Index;
            level.Gates = level.Gates.Append(tail).ToArray();
            return true;
        }

        public static void DeleteMany(LevelData level, List<int> indices)
        {
            var gates = level.Gates.ToList();
            foreach (var i in indices.OrderByDescending(i => i)) gates.RemoveAt(i);
            level.Gates = gates.ToArray();
        }

        // Carries a gate to another wall place; it stays whole on the wall line (the validator flags a line that is not
        // wall all along).
        public static bool Move(LevelData level, int index, BoardEdge edge, int slot, int depth)
        {
            var gate = level.Gates[index];
            var wall = LevelValidator.EdgeLength(edge, level.Cols, level.Rows);
            var length = Mathf.Clamp(gate.Length, 1, wall);
            var start = Mathf.Clamp(slot, 0, wall - length);
            depth = Mathf.Clamp(depth, 0, GateGeometry.MaxDepth(edge, level.Cols, level.Rows) - 1);
            if (gate.Edge == edge && gate.Index == start && gate.Length == length && gate.Depth == depth) return false;

            gate.Edge = edge;
            gate.Index = start;
            gate.Length = length;
            gate.Depth = depth;
            return true;
        }

        // Free wall places right before and after the gate on its wall line: where it can grow.
        public static List<int> GrowSlots(LevelData level, int index)
        {
            var gate = level.Gates[index];
            var wall = LevelValidator.EdgeLength(gate.Edge, level.Cols, level.Rows);
            var holes = new HashSet<CellCoord>(level.Holes ?? new CellCoord[0]);
            var slots = new List<int>();
            foreach (var slot in new[] { gate.Index - 1, gate.Index + gate.Length })
            {
                if (slot < 0 || slot >= wall || GateAt(level, gate.Edge, slot, gate.Depth) >= 0) continue;
                var attached = GateGeometry.Attached(gate.Edge, gate.Depth, slot, level.Cols, level.Rows);
                if (GateGeometry.IsWallSide(attached, gate.Edge, level.Cols, level.Rows, holes)) slots.Add(slot);
            }

            return slots;
        }

        // Grows the gate by one wall cell on the side of the slot (one of its "+" places).
        public static bool Grow(LevelData level, int index, int slot)
        {
            var gate = level.Gates[index];
            if (!GrowSlots(level, index).Contains(slot)) return false;
            if (slot < gate.Index) gate.Index = slot;
            gate.Length++;
            return true;
        }

        // After the board shrinks, keeps every gate on its wall.
        public static void ClampAll(LevelData level)
        {
            for (var i = 0; i < (level.Gates?.Length ?? 0); i++)
                Move(level, i, level.Gates[i].Edge, level.Gates[i].Index, level.Gates[i].Depth);
        }
    }
}

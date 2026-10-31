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
        public static int GateAt(LevelData level, BoardEdge edge, int index)
        {
            var gates = level.Gates ?? new GateData[0];
            for (var i = 0; i < gates.Length; i++)
            {
                if (gates[i].Edge == edge && index >= gates[i].Index && index < gates[i].Index + gates[i].Length)
                    return i;
            }

            return -1;
        }

        public static int Add(LevelData level, BoardEdge edge, int index, BlockColor color)
        {
            var gates = (level.Gates ?? new GateData[0]).ToList();
            gates.Add(new GateData { Edge = edge, Index = index, Length = 1, Color = color });
            level.Gates = gates.ToArray();
            return gates.Count - 1;
        }

        public static void Delete(LevelData level, int index) => DeleteMany(level, new List<int> { index });

        public static void DeleteMany(LevelData level, List<int> indices)
        {
            var gates = level.Gates.ToList();
            foreach (var i in indices.OrderByDescending(i => i)) gates.RemoveAt(i);
            level.Gates = gates.ToArray();
        }

        // Carries a gate to another wall or place; it stays whole on the wall.
        public static bool Move(LevelData level, int index, BoardEdge edge, int slot)
        {
            var gate = level.Gates[index];
            var wall = LevelValidator.EdgeLength(edge, level.Cols, level.Rows);
            var length = Mathf.Clamp(gate.Length, 1, wall);
            var start = Mathf.Clamp(slot, 0, wall - length);
            if (gate.Edge == edge && gate.Index == start && gate.Length == length) return false;

            gate.Edge = edge;
            gate.Index = start;
            gate.Length = length;
            return true;
        }

        // After the board shrinks, keeps every gate on its wall.
        public static void ClampAll(LevelData level)
        {
            for (var i = 0; i < (level.Gates?.Length ?? 0); i++)
                Move(level, i, level.Gates[i].Edge, level.Gates[i].Index);
        }
    }
}

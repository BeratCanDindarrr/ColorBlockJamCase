using System;
using System.Collections.Generic;
using System.Linq;
using ColorBlockJam.Core.Level;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // A gate in the selection, by its wall line (edge and depth) and the first wall place it covers.
    [Serializable]
    internal struct GateKey : IEquatable<GateKey>
    {
        public BoardEdge Edge;
        public int Index;
        public int Depth;

        public static GateKey Of(GateData gate) => new GateKey { Edge = gate.Edge, Index = gate.Index, Depth = gate.Depth };
        public bool Equals(GateKey other) => Edge == other.Edge && Index == other.Index && Depth == other.Depth;
        public override bool Equals(object obj) => obj is GateKey other && Equals(other);
        public override int GetHashCode() => (((int)Edge * 397) ^ Index) * 31 + Depth;
    }

    // What is picked. Blocks go by id, so deleting another block does not move the selection onto a different one;
    // the last picked block or gate is the one the single-item inspector shows.
    public sealed partial class LevelEditorSession
    {
        [SerializeField] private List<string> selectedBlocks = new List<string>();
        [SerializeField] private List<GateKey> selectedGates = new List<GateKey>();

        private bool HasSelection => selectedBlocks.Count > 0 || selectedGates.Count > 0;

        private List<int> SelectedBlockIndices()
        {
            var indices = new List<int>();
            for (var i = 0; i < working.Blocks.Length; i++)
            {
                if (selectedBlocks.Contains(working.Blocks[i].Id)) indices.Add(i);
            }

            return indices;
        }

        private List<int> SelectedGateIndices()
        {
            var indices = new List<int>();
            for (var i = 0; i < working.Gates.Length; i++)
            {
                if (selectedGates.Contains(GateKey.Of(working.Gates[i]))) indices.Add(i);
            }

            return indices;
        }

        // The block the inspector and the Block tool work on: the last one picked.
        private int PrimaryBlock => selectedBlocks.Count == 0
            ? -1
            : Array.FindIndex(working.Blocks, b => b.Id == selectedBlocks[selectedBlocks.Count - 1]);

        private int PrimaryGate => selectedGates.Count == 0
            ? -1
            : Array.FindIndex(working.Gates, g => GateKey.Of(g).Equals(selectedGates[selectedGates.Count - 1]));

        private bool IsBlockSelected(int index) => selectedBlocks.Contains(working.Blocks[index].Id);

        private bool IsGateSelected(int index) => selectedGates.Contains(GateKey.Of(working.Gates[index]));

        private void ClearSelection()
        {
            selectedBlocks.Clear();
            selectedGates.Clear();
            GUI.FocusControl(null);
            RaiseStateChanged();
        }

        // Picks only this block and/or gate (-1 for none).
        private void SelectOnly(int block, int gate)
        {
            ClearSelection();
            AddToSelection(block >= 0 ? new[] { block } : new int[0], gate >= 0 ? new[] { gate } : new int[0]);
        }

        private void AddToSelection(IEnumerable<int> blocks, IEnumerable<int> gates)
        {
            foreach (var i in blocks)
            {
                var id = working.Blocks[i].Id;
                selectedBlocks.Remove(id);
                selectedBlocks.Add(id);
            }

            foreach (var i in gates)
            {
                var key = GateKey.Of(working.Gates[i]);
                selectedGates.Remove(key);
                selectedGates.Add(key);
            }

            RaiseStateChanged();
        }

        private void ToggleBlock(int index)
        {
            if (!selectedBlocks.Remove(working.Blocks[index].Id)) selectedBlocks.Add(working.Blocks[index].Id);
            RaiseStateChanged();
        }

        private void ToggleGate(int index)
        {
            var key = GateKey.Of(working.Gates[index]);
            if (!selectedGates.Remove(key)) selectedGates.Add(key);
            RaiseStateChanged();
        }

        // A selected gate moved: it keeps being selected under its new place.
        private void FollowGate(GateKey before, int index)
        {
            var at = selectedGates.IndexOf(before);
            if (at >= 0 && index >= 0 && index < working.Gates.Length) selectedGates[at] = GateKey.Of(working.Gates[index]);
        }

        // A selected block was renamed: it stays selected.
        private void FollowBlock(string before, int index)
        {
            var at = selectedBlocks.IndexOf(before);
            if (at >= 0) selectedBlocks[at] = working.Blocks[index].Id;
        }

        // Drops what is no longer in the level (after undo, delete or a raw data edit).
        private void PruneSelection()
        {
            if (working == null) return;
            selectedBlocks.RemoveAll(id => !working.Blocks.Any(b => b.Id == id));
            selectedGates.RemoveAll(key => !working.Gates.Any(g => GateKey.Of(g).Equals(key)));
        }

        // What a box from min to max covers: blocks with a cell in it and gates with a wall cell in it.
        private void SelectInBox(CellCoord min, CellCoord max)
        {
            bool In(CellCoord c) => c.Col >= min.Col && c.Col <= max.Col && c.Row >= min.Row && c.Row <= max.Row;

            var blocks = Enumerable.Range(0, working.Blocks.Length)
                .Where(i => LevelValidator.AbsoluteCells(working.Blocks[i]).Any(In));
            var gates = Enumerable.Range(0, working.Gates.Length).Where(i =>
            {
                var gate = working.Gates[i];
                return Enumerable.Range(gate.Index, gate.Length)
                    .Any(k => In(GateGeometry.Outside(gate.Edge, gate.Depth, k, working.Cols, working.Rows)));
            });
            AddToSelection(blocks.ToList(), gates.ToList());
        }
    }
}

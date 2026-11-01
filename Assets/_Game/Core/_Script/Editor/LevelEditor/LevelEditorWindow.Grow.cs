using System.Collections.Generic;
using ColorBlockJam.Core.Level;

namespace ColorBlockJam.LevelEditor
{
    // "+" places: with the Block or Gate tool, a picked block or gate shows the free places next to it; clicking one
    // grows that block or gate instead of adding a new one. Clicking elsewhere or switching tool leaves it.
    public sealed partial class LevelEditorWindow
    {
        // Grid positions of the "+" places (wall ring at -1 / Cols / Rows for gates); empty when nothing grows.
        internal List<CellCoord> GrowPlaces()
        {
            var places = new List<CellCoord>();
            if (working == null || selectedBlocks.Count + selectedGates.Count != 1) return places;

            if (tool == LevelEditorTool.Block && PrimaryBlock >= 0)
            {
                places.AddRange(LevelEditorBlockOps.GrowCells(working, PrimaryBlock));
            }
            else if (tool == LevelEditorTool.Gate && PrimaryGate >= 0)
            {
                var gate = working.Gates[PrimaryGate];
                foreach (var slot in LevelEditorGateOps.GrowSlots(working, PrimaryGate))
                    places.Add(LevelEditorBoardGui.SlotCell(gate.Edge, slot, working.Cols, working.Rows));
            }

            return places;
        }

        private bool IsGrowPlace(CellCoord point) => GrowPlaces().Contains(point);

        private void GrowBlock(CellCoord cell)
        {
            var block = PrimaryBlock;
            Edit("Grow Block", l => LevelEditorBlockOps.Grow(l, block, cell));
        }

        private void GrowGate(BoardHit hit)
        {
            var gate = PrimaryGate;
            var before = GateKey.Of(working.Gates[gate]);
            Edit("Grow Gate", l => LevelEditorGateOps.Grow(l, gate, hit.Index));
            FollowGate(before, gate);
            RaiseStateChanged();
        }
    }
}

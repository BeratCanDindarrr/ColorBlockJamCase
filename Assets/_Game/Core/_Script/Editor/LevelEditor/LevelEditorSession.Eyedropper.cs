using ColorBlockJam.Core.Level;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // The palette's eyedropper: the next press on the board takes the colour of the block or gate under it, as if it
    // were picked from the palette (brush, and recolours what is selected). A press on anything else, Esc or a click
    // off the board cancels it.
    public sealed partial class LevelEditorSession
    {
        private static readonly GUIContent EyedropperHint = new GUIContent("Click a block or gate to copy its colour");

        private bool _pickingColor;
        // The press that picked the colour: its drag and release do nothing else.
        private bool _eyedropperPress;

        internal bool IsPickingColor => _pickingColor;

        internal void ToggleEyedropper()
        {
            _pickingColor = !_pickingColor;
            if (_pickingColor) ShowNotification(EyedropperHint);
            RaiseStateChanged();
        }

        private void CancelEyedropper()
        {
            if (!_pickingColor) return;
            _pickingColor = false;
            RaiseStateChanged();
        }

        // True when the press was the eyedropper's.
        private bool PressEyedropper(BoardHit hit)
        {
            if (!_pickingColor) return false;
            _pickingColor = false;
            _eyedropperPress = true;

            var block = hit.Kind == BoardHitKind.Cell ? LevelEditorBlockOps.BlockAt(working, hit.Cell) : -1;
            var gate = hit.Kind == BoardHitKind.Wall ? LevelEditorGateOps.GateAt(working, hit.Edge, hit.Index, hit.Depth) : -1;
            if (block >= 0) PickColor(working.Blocks[block].Color);
            else if (gate >= 0) PickColor(working.Gates[gate].Color);
            RaiseStateChanged();
            return true;
        }
    }
}

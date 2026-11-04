using System.Collections.Generic;
using ColorBlockJam.Pool;
using ColorBlockJam.SandBox.Board;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // One door mesh per gate cell, laid along the gate's local x. An iced gate looks like a frozen block (ice tint and
    // texture, its count lying on top) and has no arrow until it thaws.
    public class GateView : ColorBlockJam.Views.BaseView, IPoolable
    {
        private const float Half = 0.5f;
        // Gap between the arrow and the top of the doors, in cells.
        private const float ArrowLift = 0.01f;

        [SerializeField] private MeshRenderer doorTemplate;
        // White triangle lying on top of the gate, its tip pointing out of the board.
        [SerializeField] private SpriteRenderer arrow;
        // The ice count, the frozen block's own view; its height is authored in the prefab.
        [SerializeField] private BlockIceView iceView;

        private readonly List<MeshRenderer> _doors = new List<MeshRenderer>();
        private BoardSettings _settings;
        private Color _color;

        public GateState Gate { get; private set; }

        public void Init(GateState gate, BoardLayout layout, BoardSettings settings)
        {
            Gate = gate;
            _settings = settings;
            _color = settings.GetColor(gate.Color);
            transform.localPosition = layout.GateLocal(gate.Edge, gate.Index, gate.Length, gate.Depth);
            transform.localRotation = layout.GateRotation(gate.Edge);
            transform.localScale = Vector3.one * layout.CellSize;

            if (_doors.Count == 0) _doors.Add(doorTemplate);
            // Extra doors are created once and stay with the pooled gate, so a reused gate allocates nothing.
            while (_doors.Count < gate.Length) _doors.Add(Instantiate(doorTemplate, transform));

            for (var i = 0; i < _doors.Count; i++)
            {
                var door = _doors[i];
                var used = i < gate.Length;
                door.gameObject.SetActive(used);
                if (!used) continue;

                var position = door.transform.localPosition;
                door.transform.localPosition = new Vector3(i - (gate.Length - 1) * Half, position.y, position.z);
            }

            PlaceArrow(layout.OutwardLocal(gate.Edge), settings.GateArrowSize);
            // Upright on screen like a block's count, whichever wall the gate is on.
            iceView.transform.rotation = BoardRotation();
            iceView.Show(gate.Ice, Vector3.zero);
            ApplyIce();
        }

        // A block left the board: the count drops with a punch; at 0 the gate looks and works as usual again.
        public void CrackIce()
        {
            iceView.Crack(Gate.Ice, _settings.IcePunch, _settings.IcePunchDuration);
            if (Gate.Ice == 0) ApplyIce();
        }

        public void OnSpawned()
        {
        }

        public void OnDespawned()
        {
            iceView.Hide();
            Gate = null;
        }

        private void ApplyIce()
        {
            var iced = Gate.Ice > 0;
            var color = iced ? Color.Lerp(_color, _settings.IceColor, _settings.IceTint) : _color;
            var texture = iced ? _settings.IceTexture : null;
            for (var i = 0; i < Gate.Length; i++)
                RendererTint.Apply(_doors[i], color, texture);
            if (arrow != null) arrow.gameObject.SetActive(!iced);
        }

        // In the middle of the gate, just above its doors, lying flat.
        private void PlaceArrow(Vector3 outwardLocal, float size)
        {
            if (arrow == null) return;

            var top = doorTemplate.bounds.max.y;
            var center = transform.position;
            arrow.transform.position = new Vector3(center.x, top + ArrowLift * transform.lossyScale.y, center.z);
            var board = transform.parent;
            var outward = board != null ? board.TransformDirection(outwardLocal) : outwardLocal;
            arrow.transform.rotation = Quaternion.LookRotation(Vector3.down, outward);
            arrow.transform.localScale = Vector3.one * size;
        }

        private Quaternion BoardRotation() => transform.parent != null ? transform.parent.rotation : Quaternion.identity;
    }
}

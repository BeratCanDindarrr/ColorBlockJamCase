using System.Collections.Generic;
using ColorBlockJam.Pool;
using ColorBlockJam.SandBox.Board;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // One door mesh per gate cell, laid along the gate's local x.
    public class GateView : MonoBehaviour, IPoolable
    {
        private const float Half = 0.5f;

        [SerializeField] private MeshRenderer doorTemplate;

        private readonly List<MeshRenderer> _doors = new List<MeshRenderer>();

        public GateState Gate { get; private set; }

        public void Init(GateState gate, BoardLayout layout, Color color)
        {
            Gate = gate;
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
                RendererTint.Apply(door, color);
            }
        }

        public void OnSpawned()
        {
        }

        public void OnDespawned() => Gate = null;
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // The art's half-cell wall piece made as long as a whole straight run: its rounded ends (|x| past the innermost
    // vertex) move out unchanged and only the flat middle grows, so a run is one piece with rounded ends at its two
    // ends only. Meshes are kept per source and length for the app's lifetime; a level only uses a few lengths.
    public static class WallMeshStretch
    {
        private const float KeyPrecision = 1000f;

        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        // length: how many times the source's own length (model x) the result is.
        public static Mesh Get(Mesh source, float length)
        {
            var key = $"{source.GetInstanceID()}_{Mathf.RoundToInt(length * KeyPrecision)}";
            if (Cache.TryGetValue(key, out var mesh) && mesh != null) return mesh;

            mesh = Build(source, length);
            Cache[key] = mesh;
            return mesh;
        }

        private static Mesh Build(Mesh source, float length)
        {
            if (!source.isReadable)
            {
                Debug.LogError($"{nameof(WallMeshStretch)}: turn on Read/Write in the import settings of the model holding {source.name}; walls are drawn unstretched.");
                return source;
            }

            var vertices = source.vertices;
            var half = source.bounds.extents.x;
            var center = source.bounds.center.x;
            var capStart = half;
            foreach (var vertex in vertices) capStart = Mathf.Min(capStart, Mathf.Abs(vertex.x - center));

            // Each end moves out by the extra half length; a vertex inside the caps (none in the art) scales evenly.
            var grow = half * (length - 1f);
            for (var i = 0; i < vertices.Length; i++)
            {
                var x = vertices[i].x - center;
                var offset = Mathf.Abs(x);
                var moved = offset >= capStart
                    ? offset + grow
                    : capStart > 0f ? offset * (capStart + grow) / capStart : offset;
                vertices[i].x = center + Mathf.Sign(x) * moved;
            }

            var mesh = Object.Instantiate(source);
            mesh.name = $"{source.name}_x{length:0.##}";
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

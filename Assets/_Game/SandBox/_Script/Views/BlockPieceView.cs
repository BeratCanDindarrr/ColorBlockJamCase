using ColorBlockJam.Pool;
using ColorBlockJam.SandBox.Board;
using DG.Tweening;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // One quarter-cell piece of a block. The kit tiles span [0,1] in mesh x/y (the inner corner spans [-1,1]),
    // so the mesh child is shifted to centre the tile on this transform and to put its bottom on the floor.
    public class BlockPieceView : MonoBehaviour, IPoolable
    {
        // Measured from BlockParts.fbx at the prefab's 0.5 kit scale: tile centre (0.25, 0.25), bottom 0.33 below the pivot.
        private const float TileShift = 0.25f;
        private const float FloorLift = 0.33f;

        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Mesh centerMesh;
        [SerializeField] private Mesh edgeMesh;
        [SerializeField] private Mesh innerCornerMesh;
        [SerializeField] private Mesh outerCornerMesh;

        public void Set(BlockPieceKind kind, Color color, Texture texture = null)
        {
            meshFilter.sharedMesh = GetMesh(kind);
            var shift = kind == BlockPieceKind.InnerCorner ? 0f : TileShift;
            meshFilter.transform.localPosition = new Vector3(-shift, FloorLift, -shift);
            Tint(color, texture);
        }

        public void Tint(Color color, Texture texture = null) => RendererTint.Apply(meshRenderer, color, texture);

        public void OnSpawned()
        {
        }

        public void OnDespawned()
        {
            // A shatter may still be running (block cleared mid-exit); the pool must get an intact piece back.
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
        }

        private Mesh GetMesh(BlockPieceKind kind)
        {
            switch (kind)
            {
                case BlockPieceKind.Edge: return edgeMesh;
                case BlockPieceKind.InnerCorner: return innerCornerMesh;
                case BlockPieceKind.OuterCorner: return outerCornerMesh;
                default: return centerMesh;
            }
        }
    }
}

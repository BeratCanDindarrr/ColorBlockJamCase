using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // A wall piece from the art: a corner, or a straight run of wall stretched to its length (WallMeshStretch) so the
    // run reads as one piece. thickness scales it across the wall (the straight piece is flat across, so filling a
    // removed cell keeps its look).
    public class WallPieceView : ColorBlockJam.Views.BaseView
    {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private Mesh wallMesh;
        [SerializeField] private Mesh cornerMesh;
        [SerializeField] private MeshRenderer meshRenderer;

        public void SetCorner()
        {
            meshFilter.sharedMesh = cornerMesh;
        }

        // length: in half-cell pieces (the art's piece is half a cell long).
        public void SetStraight(float length, float thickness)
        {
            meshFilter.sharedMesh = WallMeshStretch.Get(wallMesh, length);
            var scale = transform.localScale;
            transform.localScale = new Vector3(scale.x, scale.y, scale.z * thickness);
        }

        public void SetColor(Color color) => RendererTint.Apply(meshRenderer, color);
    }
}

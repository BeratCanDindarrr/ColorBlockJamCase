using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // Half a cell of wall: a straight piece, or the rounded piece that closes an outer corner.
    public class WallPieceView : ColorBlockJam.Views.BaseView
    {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private Mesh wallMesh;
        [SerializeField] private Mesh cornerMesh;

        public void SetCorner(bool isCorner) => meshFilter.sharedMesh = isCorner ? cornerMesh : wallMesh;
    }
}

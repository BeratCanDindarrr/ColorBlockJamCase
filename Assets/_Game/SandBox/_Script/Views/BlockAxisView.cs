using ColorBlockJam.Core.Level;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // The arrow on a block that may only slide one way.
    public class BlockAxisView : MonoBehaviour
    {
        // The arrow meshes run along board rows (local z); a horizontal block turns them a quarter.
        private const float HorizontalYaw = 90f;

        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        // Shortest first: the arrow that fits a block 1, 2, 3, ... cells long along its axis.
        [SerializeField] private Mesh[] arrowMeshes;

        // centre: the block-local point the arrow is centred on; span: the block's length in cells along its axis.
        public void Show(BlockAxis axis, Vector3 centre, int span, Color color)
        {
            var visible = axis != BlockAxis.Free && arrowMeshes.Length > 0;
            gameObject.SetActive(visible);
            if (!visible) return;

            meshFilter.sharedMesh = arrowMeshes[Mathf.Clamp(span - 1, 0, arrowMeshes.Length - 1)];
            transform.localPosition = new Vector3(centre.x, transform.localPosition.y, centre.z);
            transform.localRotation = Quaternion.Euler(0f, axis == BlockAxis.Horizontal ? HorizontalYaw : 0f, 0f);
            RendererTint.Apply(meshRenderer, color);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}

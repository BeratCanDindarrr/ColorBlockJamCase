using UnityEngine;

namespace ColorBlockJam.SandBox
{
    public class FloorTileView : MonoBehaviour
    {
        [SerializeField] private MeshRenderer meshRenderer;

        public void SetColor(Color color) => RendererTint.Apply(meshRenderer, color);
    }
}

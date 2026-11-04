using UnityEngine;

namespace ColorBlockJam.SandBox
{
    public class FloorTileView : ColorBlockJam.Views.BaseView
    {
        [SerializeField] private MeshRenderer meshRenderer;

        public void SetColor(Color color) => RendererTint.Apply(meshRenderer, color);
    }
}

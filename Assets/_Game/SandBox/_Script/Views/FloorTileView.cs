using UnityEngine;

namespace ColorBlockJam.SandBox
{
    public class FloorTileView : ColorBlockJam.Views.BaseView
    {
        [SerializeField] private MeshRenderer meshRenderer;
        // A full-cell quad just under the tile, filling the gaps between tiles (else the background shows through).
        [SerializeField] private MeshRenderer gapRenderer;

        public void SetColor(Color color, Color gapColor)
        {
            RendererTint.Apply(meshRenderer, color);
            RendererTint.Apply(gapRenderer, gapColor);
        }
    }
}

using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // Tints a renderer without instancing its material; the kit meshes use the built-in Standard shader.
    public static class RendererTint
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static MaterialPropertyBlock _block;

        public static void Apply(Renderer target, Color color)
        {
            _block ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(_block);
            _block.SetColor(ColorId, color);
            // The gate material ships with a white emission that would wash the tint out.
            _block.SetColor(EmissionColorId, Color.black);
            target.SetPropertyBlock(_block);
        }
    }
}

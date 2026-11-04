using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // Tints a renderer without instancing its material; the kit meshes use the built-in Standard shader.
    public static class RendererTint
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static MaterialPropertyBlock _block;

        // texture replaces the material's main texture; null keeps the material's own.
        public static void Apply(Renderer target, Color color, Texture texture = null)
        {
            _block ??= new MaterialPropertyBlock();
            // Built from scratch each time, so a texture set earlier (ice) goes once it is no longer passed.
            _block.Clear();
            _block.SetColor(ColorId, color);
            // The gate material ships with a white emission that would wash the tint out.
            _block.SetColor(EmissionColorId, Color.black);
            if (texture) _block.SetTexture(MainTexId, texture);
            target.SetPropertyBlock(_block);
        }
    }
}

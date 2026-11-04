using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // Small cubes bursting out of a gate where an exiting block is cut off. One particle system per board; each burst
    // only emits, so nothing is spawned per exit.
    public class ExitBurstView : ColorBlockJam.Views.BaseView
    {
        // The cubes start scattered this far (world units) round the cut-off piece.
        private const float JitterRadius = 0.1f;

        [SerializeField] private ParticleSystem particles;

        public void Burst(Vector3 position, Vector3 direction, Color color, int count, float speed, float spread,
            float lift)
        {
            var emit = new ParticleSystem.EmitParams { startColor = color, applyShapeToPosition = false };
            for (var i = 0; i < count; i++)
            {
                var random = Random.insideUnitSphere * spread;
                random.y = Mathf.Abs(random.y);
                emit.position = position + Random.insideUnitSphere * JitterRadius;
                emit.velocity = direction * speed + random + Vector3.up * lift;
                emit.rotation3D = Random.insideUnitSphere * 180f;
                particles.Emit(emit, 1);
            }
        }
    }
}

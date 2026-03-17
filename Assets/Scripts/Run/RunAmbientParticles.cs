using UnityEngine;
using RoguelikeCardBattler.Core.VFX;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Spawns ambient dust particles in RunScene.
    /// Attach to any GameObject in the scene.
    ///
    /// <para><b>Onboarding:</b> This is a thin bootstrapper — it creates a child
    /// GameObject with <see cref="AmbientParticleController"/> set to the Dust preset.
    /// No configuration needed; tweak emissionRate / particleColor on the child's
    /// AmbientParticleController component if desired.</para>
    /// </summary>
    public class RunAmbientParticles : MonoBehaviour
    {
        private void Start()
        {
            GameObject child = new GameObject("AmbientDust");
            child.transform.SetParent(transform, false);
            child.AddComponent<AmbientParticleController>();
            // Dust is the default preset — no SetPreset call needed.
        }
    }
}

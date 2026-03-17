using UnityEngine;
using RoguelikeCardBattler.Core.VFX;

namespace RoguelikeCardBattler.Gameplay.Combat
{
    /// <summary>
    /// Spawns ambient ember particles in BattleScene.
    /// Attach to any GameObject in the scene.
    ///
    /// <para><b>Onboarding:</b> This is a thin bootstrapper — it creates a child
    /// GameObject with <see cref="AmbientParticleController"/> set to the Embers preset.
    /// The Embers preset adds noise and alpha fade-out for a more dynamic look.
    /// Tweak emissionRate / particleColor on the child's AmbientParticleController
    /// component if desired.</para>
    /// </summary>
    public class CombatAmbientParticles : MonoBehaviour
    {
        private void Start()
        {
            GameObject child = new GameObject("AmbientEmbers");
            child.transform.SetParent(transform, false);
            AmbientParticleController controller = child.AddComponent<AmbientParticleController>();
            controller.SetPreset(ParticlePreset.Embers);
        }
    }
}

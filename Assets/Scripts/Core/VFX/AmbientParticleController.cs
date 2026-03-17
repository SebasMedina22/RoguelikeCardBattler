using UnityEngine;

namespace RoguelikeCardBattler.Core.VFX
{
    /// <summary>
    /// Reusable ambient particle controller that configures a ParticleSystem entirely
    /// via code. Supports two presets: Dust (slow, floating motes for the map) and
    /// Embers (noisier, fading sparks for combat).
    ///
    /// <para><b>Onboarding:</b> Attach this to any GameObject or let a spawner script
    /// (RunAmbientParticles / CombatAmbientParticles) create it at runtime. Tweak
    /// <see cref="preset"/>, <see cref="emissionRate"/> and <see cref="particleColor"/>
    /// from the Inspector. The ParticleSystem child is created automatically in Awake.</para>
    /// </summary>
    public enum ParticlePreset
    {
        Dust,
        Embers
    }

    public class AmbientParticleController : MonoBehaviour
    {
        [Tooltip("Visual style: Dust = slow floating motes, Embers = noisy fading sparks.")]
        [SerializeField] private ParticlePreset preset = ParticlePreset.Dust;

        [Tooltip("Particles emitted per second.")]
        [Range(1f, 20f)]
        [SerializeField] private float emissionRate = 8f;

        [Tooltip("Base tint and opacity of each particle.")]
        [SerializeField] private Color particleColor = new Color(1f, 1f, 1f, 0.3f);

        private ParticleSystem _ps;

        // ──────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────

        private void Awake()
        {
            CreateParticleSystem();
            ApplyPreset(preset);
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        /// <summary>
        /// Switch preset at runtime. Stops, reconfigures and restarts the particle system.
        /// </summary>
        public void SetPreset(ParticlePreset newPreset)
        {
            preset = newPreset;
            if (_ps != null)
            {
                _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ApplyPreset(preset);
                _ps.Play();
            }
        }

        // ──────────────────────────────────────────────
        // Internal helpers
        // ──────────────────────────────────────────────

        /// <summary>
        /// Creates the child GameObject that holds the ParticleSystem and its renderer.
        /// </summary>
        private void CreateParticleSystem()
        {
            GameObject psGo = new GameObject("_AmbientPS");
            psGo.transform.SetParent(transform, false);
            _ps = psGo.AddComponent<ParticleSystem>();

            // Stop the default auto-play so we configure first
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Renderer setup — Sprites/Default material, no shadow, correct sorting
            ParticleSystemRenderer psRenderer = psGo.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = new Material(Shader.Find("Sprites/Default"));
            psRenderer.sortingOrder = 5;
        }

        /// <summary>
        /// Applies all ParticleSystem module settings for the given preset.
        /// </summary>
        private void ApplyPreset(ParticlePreset targetPreset)
        {
            // ── Viewport size for the emission shape ──
            float camHeight = 10f; // fallback
            float camWidth = 16f;
            Camera cam = Camera.main;
            if (cam != null)
            {
                camHeight = cam.orthographicSize * 2f;
                camWidth = camHeight * cam.aspect;
            }

            // ── Main module ──
            var main = _ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;
            main.startColor = particleColor;
            main.playOnAwake = false;

            // ── Shape module — box covering the camera viewport ──
            var shape = _ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(camWidth, camHeight, 1f);

            // ── Emission module ──
            var emission = _ps.emission;
            emission.enabled = true;
            emission.rateOverTime = emissionRate;

            // ── Collision module — disabled so particles never block UI raycasts ──
            var collision = _ps.collision;
            collision.enabled = false;

            // ── Preset-specific tuning ──
            if (targetPreset == ParticlePreset.Dust)
            {
                ApplyDust(main);
            }
            else
            {
                ApplyEmbers(main);
            }

            _ps.Play();
        }

        /// <summary>
        /// Dust preset: slow, floaty, low-opacity motes for the map scene.
        /// </summary>
        private void ApplyDust(ParticleSystem.MainModule main)
        {
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.gravityModifier = -0.01f;

            // Disable modules not needed for Dust
            var colorOverLifetime = _ps.colorOverLifetime;
            colorOverLifetime.enabled = false;

            var noise = _ps.noise;
            noise.enabled = false;
        }

        /// <summary>
        /// Embers preset: noisier, fading sparks for the combat scene.
        /// Adds color-over-lifetime fade-out and noise for organic movement.
        /// </summary>
        private void ApplyEmbers(ParticleSystem.MainModule main)
        {
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
            main.gravityModifier = -0.02f;

            // ── Color over lifetime: fade out alpha in the last 30% ──
            var colorOverLifetime = _ps.colorOverLifetime;
            colorOverLifetime.enabled = true;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            // ── Noise module: organic movement ──
            var noise = _ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 1.5f;
        }
    }
}

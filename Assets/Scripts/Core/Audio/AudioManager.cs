using UnityEngine;

namespace RoguelikeCardBattler.Core.Audio
{
    /// <summary>
    /// Singleton audio manager that persists across scenes (DontDestroyOnLoad).
    /// Provides SFX and music playback with configurable volumes.
    ///
    /// <para><b>Onboarding:</b> Auto-instantiated by <see cref="AudioManagerBootstrap"/>
    /// before any scene loads — no manual setup required. Access via
    /// <c>AudioManager.Instance</c>. All placeholder clips are generated
    /// programmatically in Awake (no external audio files needed).</para>
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        /// <summary>Returns the singleton instance (null if not yet created).</summary>
        public static AudioManager Instance => _instance;

        [Tooltip("Volume for one-shot sound effects.")]
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 0.7f;

        [Tooltip("Volume for looping background music.")]
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.5f;

        private AudioSource _sfxSource;
        private AudioSource _musicSource;

        // ── Placeholder clips (generated programmatically) ──

        /// <summary>Short high-pitched click (menu buttons).</summary>
        public AudioClip ClickSFX { get; private set; }
        /// <summary>Medium tone for playing a card.</summary>
        public AudioClip CardPlaySFX { get; private set; }
        /// <summary>Short noise burst for hit feedback.</summary>
        public AudioClip HitSFX { get; private set; }
        /// <summary>Ascending sweep for victory.</summary>
        public AudioClip VictorySFX { get; private set; }
        /// <summary>Descending sweep for defeat.</summary>
        public AudioClip DefeatSFX { get; private set; }
        /// <summary>Double-pulse tone for world change.</summary>
        public AudioClip WorldChangeSFX { get; private set; }
        /// <summary>Looping low-frequency ambient clip used by the Campfire node.</summary>
        public AudioClip CampfireAmbientClip { get; private set; }

        // ──────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Create AudioSources
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;

            // Generate placeholder clips
            ClickSFX = GenerateTone("ClickSFX", 800f, 0.05f, ToneType.Sine);
            CardPlaySFX = GenerateTone("CardPlaySFX", 400f, 0.1f, ToneType.Sine);
            HitSFX = GenerateTone("HitSFX", 0f, 0.08f, ToneType.Noise);
            VictorySFX = GenerateTone("VictorySFX", 400f, 0.3f, ToneType.SweepUp);
            DefeatSFX = GenerateTone("DefeatSFX", 400f, 0.4f, ToneType.SweepDown);
            WorldChangeSFX = GenerateTone("WorldChangeSFX", 600f, 0.15f, ToneType.DoublePulse);
            CampfireAmbientClip = GenerateCampfireAmbient("CampfireAmbient", 3f);
        }

        /// <summary>
        /// Genera un clip ambiente loop-friendly: ruido filtrado modulado por
        /// dos sinusoides de baja frecuencia (suma 80 Hz + 140 Hz). Sin envelope
        /// de fade para evitar el clic al loopear; volumen moderado.
        /// </summary>
        private AudioClip GenerateCampfireAmbient(string clipName, float duration)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float low = 0.5f * Mathf.Sin(2f * Mathf.PI * 80f * t)
                          + 0.5f * Mathf.Sin(2f * Mathf.PI * 140f * t);
                float noise = Random.Range(-1f, 1f) * 0.15f;
                samples[i] = (low * 0.4f + noise) * 0.35f;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        /// <summary>Plays a one-shot sound effect at the current SFX volume.</summary>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip, sfxVolume);
        }

        /// <summary>Starts playing a music clip (optionally looping).</summary>
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null) return;
            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = musicVolume;
            _musicSource.Play();
        }

        /// <summary>Stops the currently playing music.</summary>
        public void StopMusic()
        {
            _musicSource.Stop();
        }

        /// <summary>Updates the SFX volume (0–1).</summary>
        public void SetSFXVolume(float vol)
        {
            sfxVolume = Mathf.Clamp01(vol);
        }

        /// <summary>Updates the music volume (0–1) and applies it immediately.</summary>
        public void SetMusicVolume(float vol)
        {
            musicVolume = Mathf.Clamp01(vol);
            _musicSource.volume = musicVolume;
        }

        // ──────────────────────────────────────────────
        // Tone generation
        // ──────────────────────────────────────────────

        private enum ToneType
        {
            Sine,
            Noise,
            SweepUp,
            SweepDown,
            DoublePulse
        }

        /// <summary>
        /// Generates a short AudioClip programmatically using simple waveforms.
        /// <paramref name="frequency"/> is the base frequency in Hz (ignored for Noise).
        /// </summary>
        private AudioClip GenerateTone(string clipName, float frequency, float duration, ToneType type)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float normalizedT = (float)i / sampleCount; // 0..1 over clip duration

                switch (type)
                {
                    case ToneType.Sine:
                        samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t);
                        break;

                    case ToneType.Noise:
                        samples[i] = Random.Range(-1f, 1f);
                        break;

                    case ToneType.SweepUp:
                        // Sweep from frequency to frequency*2
                        float freqUp = Mathf.Lerp(frequency, frequency * 2f, normalizedT);
                        samples[i] = Mathf.Sin(2f * Mathf.PI * freqUp * t);
                        break;

                    case ToneType.SweepDown:
                        // Sweep from frequency to frequency*0.5
                        float freqDown = Mathf.Lerp(frequency, frequency * 0.5f, normalizedT);
                        samples[i] = Mathf.Sin(2f * Mathf.PI * freqDown * t);
                        break;

                    case ToneType.DoublePulse:
                        // Two pulses: first half and second half each have a sine burst
                        bool inPulse = normalizedT < 0.4f || (normalizedT > 0.6f && normalizedT < 1f);
                        samples[i] = inPulse ? Mathf.Sin(2f * Mathf.PI * frequency * t) : 0f;
                        break;
                }

                // Apply a simple fade-out envelope to avoid clicks
                float envelope = 1f - normalizedT;
                samples[i] *= envelope * 0.5f; // 0.5 to keep volume moderate
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

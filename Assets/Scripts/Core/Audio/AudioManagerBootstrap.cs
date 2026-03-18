using UnityEngine;

namespace RoguelikeCardBattler.Core.Audio
{
    /// <summary>
    /// Auto-instantiates <see cref="AudioManager"/> before any scene loads using
    /// [RuntimeInitializeOnLoadMethod]. No manual setup in the editor required.
    ///
    /// <para><b>Onboarding:</b> This class runs automatically at game start.
    /// It checks whether an AudioManager already exists and creates one if needed.
    /// The AudioManager persists across scenes via DontDestroyOnLoad.</para>
    /// </summary>
    public static class AudioManagerBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (AudioManager.Instance != null)
            {
                return;
            }

            GameObject go = new GameObject("AudioManager");
            go.AddComponent<AudioManager>();
        }
    }
}

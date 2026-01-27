using UnityEngine;

namespace RoguelikeCardBattler.Save
{
    /// <summary>
    /// Debug only, remove after verification.
    /// Ejecuta un smoke test simple de guardado/carga en Play Mode.
    /// </summary>
    public class SaveSmokeTest : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = true;

        private void Start()
        {
            if (!runOnStart)
            {
                return;
            }

            SaveData save = new SaveData();
            save.meta.totalRuns = 3;
            SaveService.Instance.SaveMeta(save);

            if (SaveService.Instance.TryLoadMeta(out SaveData loaded))
            {
                Debug.Log($"[SaveSmokeTest] Loaded runs: {loaded.meta.totalRuns}");
            }
            else
            {
                Debug.LogError("[SaveSmokeTest] Load failed");
            }
        }
    }
}

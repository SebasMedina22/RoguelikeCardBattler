using UnityEngine;
using UnityEngine.SceneManagement;
using RoguelikeCardBattler.Gameplay.Combat;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Reporta el resultado del combate a la RunSession y vuelve a RunScene.
    /// </summary>
    public class BattleFlowController : MonoBehaviour
    {
        private TurnManager _turnManager;
        private bool _reported;

        private void Awake()
        {
#if UNITY_EDITOR
            Debug.Log("[BattleFlow] Awake");
#endif
            BattleFlowController[] all = Object.FindObjectsByType<BattleFlowController>(FindObjectsSortMode.None);
            if (all.Length > 1)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != this)
                    {
                        all[i].enabled = false;
                        Destroy(all[i].gameObject);
                    }
                }
#if UNITY_EDITOR
                Debug.Log("[BattleFlow] Duplicate detected, destroying extras.");
#endif
            }
        }

        private void Update()
        {
            if (_reported)
            {
                return;
            }

            if (_turnManager == null)
            {
                _turnManager = Object.FindFirstObjectByType<TurnManager>();
                if (_turnManager == null)
                {
                    return;
                }
#if UNITY_EDITOR
                Debug.Log("[BattleFlow] Found TurnManager");
#endif
            }

            bool finished = _turnManager.IsCombatFinished
                || _turnManager.CurrentPhase == TurnManager.CombatPhase.Victory
                || _turnManager.CurrentPhase == TurnManager.CombatPhase.Defeat;
            if (!finished)
            {
                return;
            }

            bool victory = _turnManager.CurrentPhase == TurnManager.CombatPhase.Victory;
#if UNITY_EDITOR
            Debug.Log($"[BattleFlow] Phase={_turnManager.CurrentPhase}");
#endif
            ReportOutcome(victory);
        }

        private void ReportOutcome(bool victory)
        {
            _reported = true;
            RunSession session = RunSession.GetOrCreate();
            session.State.LastNodeOutcome = victory ? RunState.NodeOutcome.Victory : RunState.NodeOutcome.Defeat;
            session.State.PendingReturnFromBattle = true;
            session.State.RunFailed = !victory;
            if (!IsSceneInBuild("RunScene"))
            {
#if UNITY_EDITOR
                Debug.LogError("[BattleFlow] RunScene no está en Build Settings.");
#endif
                return;
            }
#if UNITY_EDITOR
            Debug.Log("[BattleFlow] Loading RunScene");
#endif
            SceneManager.LoadScene("RunScene");
        }

        private bool IsSceneInBuild(string sceneName)
        {
            int total = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < total; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (path.EndsWith($"/{sceneName}.unity"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

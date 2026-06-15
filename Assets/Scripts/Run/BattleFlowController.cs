using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RoguelikeCardBattler.Core;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;
using RoguelikeCardBattler.Gameplay.Relics;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Reporta el resultado del combate a la RunSession y vuelve a RunScene.
    /// Detecta si el enemigo es un Boss y dispara lógica de fin de acto.
    /// </summary>
    public class BattleFlowController : MonoBehaviour
    {
        private const string RunSceneName = "RunScene";
        private TurnManager _turnManager;
        private bool _reported;
        private bool _configured;
        private bool _configErrorLogged;
        [SerializeField] private RunCombatConfig fallbackCombatConfig;
        private bool _isBossBattle;
        private bool _isEliteBattle;

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

            TryConfigureCombat();
        }

        private void Update()
        {
            if (_reported)
            {
                return;
            }

            if (!_configured)
            {
                TryConfigureCombat();
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
            ApplyCombatResult(session, victory);

            if (!IsSceneInBuild(RunSceneName))
            {
#if UNITY_EDITOR
                Debug.LogError("[BattleFlow] RunScene no está en Build Settings.");
#endif
                return;
            }
#if UNITY_EDITOR
            Debug.Log($"[BattleFlow] Loading RunScene (HP saved: {session.State.PlayerCurrentHP}/{session.State.PlayerMaxHP})");
#endif
            SceneTransitionManager.LoadScene(RunSceneName);
        }

        /// <summary>
        /// Mutaciones de RunState que produce el resultado de un combate (flags de
        /// outcome, acto completado, drops de Retazos) SIN sincronizar HP y SIN cargar
        /// escena. El HP ya lo escribió TurnManager.DispatchCombatEnd (sync actor→RunState
        /// pre-dispatch, Opción B del spec fix_combat_end_hp_sync): este método NO debe
        /// volver a pisarlo. Es público como seam de testeo (T1 verifica la no-pisada de
        /// HP); el proyecto no usa InternalsVisibleTo. El tail de transición de escena
        /// queda en ReportOutcome.
        /// </summary>
        public void ApplyCombatResult(RunSession session, bool victory)
        {
            session.State.LastNodeOutcome = victory ? RunState.NodeOutcome.Victory : RunState.NodeOutcome.Defeat;
            session.State.PendingReturnFromBattle = true;
            session.State.RunFailed = !victory;

            // Si es una batalla de boss y el jugador ganó, marca el acto como completado
            if (_isBossBattle && victory)
            {
                session.State.ActoCompleted = true;
#if UNITY_EDITOR
                Debug.Log("[BattleFlow] Boss derrotado - Acto completado marcado");
#endif
            }

            // Drops de Retazos (Sub-PR 3B): Elite garantizado random sin duplicados,
            // Boss garantizado y único. RelicInventoryView (HUD) los detecta vía
            // polling y dispara el pulse on-acquire. Se aplican después de mutar
            // RunState.PendingReturnFromBattle para que RunFlowController los vea.
            if (victory)
            {
                TryDropRelics(session);
            }
        }

        private void TryConfigureCombat()
        {
            RunSession session = RunSession.GetOrCreate();
            if (session.CombatConfig == null && fallbackCombatConfig != null)
            {
                session.ConfigureCombat(fallbackCombatConfig);
            }

            RunCombatConfig config = session.CombatConfig;
            if (config == null || config.DefaultEnemy == null)
            {
#if UNITY_EDITOR
                if (!_configErrorLogged)
                {
                    Debug.LogError("[BattleFlow] RunCombatConfig o enemigo default no asignado.");
                    _configErrorLogged = true;
                }
#endif
                return;
            }

            if (_turnManager == null)
            {
                _turnManager = Object.FindFirstObjectByType<TurnManager>();
                if (_turnManager == null)
                {
                    return;
                }
            }

            var deck = session.State.GetDeckSnapshot();
            if (deck.Count == 0)
            {
#if UNITY_EDITOR
                if (!_configErrorLogged)
                {
                    Debug.LogError("[BattleFlow] Deck de run vacío.");
                    _configErrorLogged = true;
                }
#endif
                return;
            }

            // Determina el enemigo a usar: primero intenta cargar desde el nodo específico
            EnemyDefinition enemyToUse = GetEnemyForCurrentNode(session) ?? config.DefaultEnemy;
            
            // Detecta si es una batalla de boss basado en el tipo de nodo
            _isBossBattle = IsCurrentNodeBoss(session);
            _isEliteBattle = IsCurrentNodeElite(session);

            session.State.EnsurePlayerHpInitialized(_turnManager.PlayerMaxHP);
            int currentHp = session.State.PlayerCurrentHP;
            int maxHp = session.State.PlayerMaxHP;

#if UNITY_EDITOR
            Debug.Log($"[BattleFlow] Deck size: {deck.Count}, Enemy: {enemyToUse.EnemyName}, IsBoss: {_isBossBattle}, HP: {currentHp}/{maxHp}");
#endif
            _turnManager.ConfigureCombat(
                deck,
                enemyToUse,
                playerCurrentHpOverride: currentHp,
                playerMaxHpOverride: maxHp,
                initializeImmediately: true,
                playerWorldAType: session.State.PlayerWorldAType,
                playerWorldBType: session.State.PlayerWorldBType,
                isElite: _isEliteBattle,
                isBoss: _isBossBattle);
            _configured = true;
        }

        /// <summary>
        /// Obtiene el enemigo específico del nodo actual, si está definido.
        /// De lo contrario, retorna null para que se use el enemigo default.
        /// </summary>
        private EnemyDefinition GetEnemyForCurrentNode(RunSession session)
        {
            if (session.Map == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[BattleFlow] session.Map es null");
#endif
                return null;
            }

            int currentNodeId = session.State.CurrentNodeId;
#if UNITY_EDITOR
            Debug.Log($"[BattleFlow] CurrentNodeId: {currentNodeId}, Total nodes: {session.Map.Nodes.Count}");
#endif
            if (currentNodeId < 0 || currentNodeId >= session.Map.Nodes.Count)
            {
#if UNITY_EDITOR
                Debug.LogError($"[BattleFlow] CurrentNodeId {currentNodeId} fuera de rango");
#endif
                return null;
            }

            MapNode node = session.Map.GetNode(currentNodeId);
#if UNITY_EDITOR
            Debug.Log($"[BattleFlow] Node obtenido: {node}, SpecificEnemy: {node?.SpecificEnemy}");
#endif
            if (node == null || node.SpecificEnemy == null)
            {
                return null;
            }

            return node.SpecificEnemy;
        }

        /// <summary>
        /// Aplica drops de Retazos para Elite (random sin duplicados desde el pool)
        /// y Boss (drop único). Si el pool de Elite está agotado o el SO de Boss no
        /// está asignado, hace nothing. Los duplicados se evitan comparando contra
        /// los Definitions ya presentes en RunState.Relics.
        /// </summary>
        private void TryDropRelics(RunSession session)
        {
            RunCombatConfig config = session.CombatConfig;
            if (config == null) return;

            if (_isEliteBattle)
            {
                RelicDefinition drop = PickRelicDrop(config.EliteRelicDropPool, session.State);
                if (drop != null)
                {
                    session.State.AddRelic(drop);
#if UNITY_EDITOR
                    Debug.Log($"[BattleFlow] Elite drop: {drop.DisplayName}");
#endif
                }
            }

            if (_isBossBattle && config.BossRelicDrop != null)
            {
                if (!HasRelic(session.State, config.BossRelicDrop))
                {
                    session.State.AddRelic(config.BossRelicDrop);
#if UNITY_EDITOR
                    Debug.Log($"[BattleFlow] Boss drop: {config.BossRelicDrop.DisplayName}");
#endif
                }
            }
        }

        private static RelicDefinition PickRelicDrop(IReadOnlyList<RelicDefinition> pool, RunState state)
        {
            if (pool == null || pool.Count == 0) return null;
            List<RelicDefinition> available = new List<RelicDefinition>();
            for (int i = 0; i < pool.Count; i++)
            {
                RelicDefinition candidate = pool[i];
                if (candidate == null) continue;
                if (HasRelic(state, candidate)) continue;
                available.Add(candidate);
            }
            if (available.Count == 0) return null;
            int index = Random.Range(0, available.Count);
            return available[index];
        }

        private static bool HasRelic(RunState state, RelicDefinition def)
        {
            if (state == null || def == null) return false;
            for (int i = 0; i < state.Relics.Count; i++)
            {
                RelicInstance inst = state.Relics[i];
                if (inst != null && inst.Definition == def) return true;
            }
            return false;
        }

        /// <summary>
        /// Determina si el nodo actual es de tipo Elite.
        /// </summary>
        private bool IsCurrentNodeElite(RunSession session)
        {
            if (session.Map == null)
            {
                return false;
            }

            int currentNodeId = session.State.CurrentNodeId;
            if (currentNodeId < 0 || currentNodeId >= session.Map.Nodes.Count)
            {
                return false;
            }

            MapNode node = session.Map.GetNode(currentNodeId);
            return node != null && node.Type == NodeType.Elite;
        }

        /// <summary>
        /// Determina si el nodo actual es de tipo Boss.
        /// </summary>
        private bool IsCurrentNodeBoss(RunSession session)
        {
            if (session.Map == null)
            {
                return false;
            }

            int currentNodeId = session.State.CurrentNodeId;
            if (currentNodeId < 0 || currentNodeId >= session.Map.Nodes.Count)
            {
                return false;
            }

            MapNode node = session.Map.GetNode(currentNodeId);
            return node != null && node.Type == NodeType.Boss;
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

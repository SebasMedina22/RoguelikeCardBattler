using UnityEngine;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Contenedor persistente del estado de run entre escenas.
    /// </summary>
    public class RunSession : MonoBehaviour
    {
        public static RunSession Instance { get; private set; }

        public RunState State { get; } = new RunState();
        public ActMap Map { get; private set; }
        public RunCombatConfig CombatConfig { get; private set; }
        [SerializeField] private EnemyDefinition bossAct1Enemy;

        public static RunSession GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var existing = Object.FindFirstObjectByType<RunSession>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            GameObject go = new GameObject("RunSession");
            Instance = go.AddComponent<RunSession>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Instance.TryInheritBossFrom(this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (Map == null)
            {
                Map = RunMapGenerator.GenerateAct1();
            }
            State.EnsureInitialized(Map);

            // Asigna BossAct1 al nodo jefe si está configurado
            AssignBossAct1();
        }

        /// <summary>
        /// Reinicia la run en memoria para comenzar desde cero.
        /// </summary>
        public void ResetForNewRun()
        {
            Map = RunMapGenerator.GenerateAct1();
            State.Reset(Map);
            CombatConfig = null;
            AssignBossAct1();
        }

        private void AssignBossAct1()
        {
            if (bossAct1Enemy != null && Map != null && Map.Nodes.Count > 7)
            {
                MapNode bossNode = Map.GetNode(7);
                if (bossNode != null && bossNode.Type == NodeType.Boss)
                {
                    bossNode.SpecificEnemy = bossAct1Enemy;
                }
            }
        }

        private void TryInheritBossFrom(RunSession source)
        {
            if (source == null)
            {
                return;
            }

            if (bossAct1Enemy == null && source.bossAct1Enemy != null)
            {
                bossAct1Enemy = source.bossAct1Enemy;
                AssignBossAct1();
            }
        }

        public void ConfigureCombat(RunCombatConfig config)
        {
            if (config == null)
            {
                return;
            }

            CombatConfig = config;
            State.InitializeDeck(config.StarterDeck);
        }
    }
}

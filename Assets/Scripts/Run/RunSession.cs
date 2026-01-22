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
            if (bossAct1Enemy != null && Map != null && Map.Nodes.Count > 7)
            {
                MapNode bossNode = Map.GetNode(7);
                if (bossNode != null && bossNode.Type == NodeType.Boss)
                {
                    bossNode.SpecificEnemy = bossAct1Enemy;
                }
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

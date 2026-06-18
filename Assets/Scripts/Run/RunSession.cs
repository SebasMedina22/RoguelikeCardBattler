using System;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Enemies;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Run.Events;

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

        // Bus de eventos de Retazos. Se instancia con referencia al State actual
        // y persiste con la RunSession (cruza escenas). Sobrevive a State.Reset
        // porque lee Relics por referencia: tras un reset, la lista queda vacía
        // y GetSubscribers devuelve 0 hasta ganar nuevos Retazos.
        public RelicHookDispatcher RelicDispatcher { get; private set; }
        [SerializeField] private EnemyDefinition bossAct1Enemy;
        [SerializeField] private Act1MapConfig mapConfig;
        [SerializeField] private EnemyPoolConfig enemyPoolConfig;
        [SerializeField] private EventPoolConfig eventPoolConfig;

        public EventPoolConfig EventPoolConfig => eventPoolConfig;

        public static RunSession GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var existing = UnityEngine.Object.FindFirstObjectByType<RunSession>();
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
                Map = GenerateMap();
            }
            State.EnsureInitialized(Map);
            AssignBossAct1();
            RelicDispatcher = new RelicHookDispatcher(State);
        }

        /// <summary>
        /// Reinicia la run en memoria para comenzar desde cero.
        /// </summary>
        public void ResetForNewRun()
        {
            Map = GenerateMap();
            State.Reset(Map);
            CombatConfig = null;
            AssignBossAct1();
        }

        /// <summary>
        /// Generates the map and assigns enemies using a shared seed for determinism.
        /// The same seed produces the same topology + types AND the same enemy placement.
        /// </summary>
        private ActMap GenerateMap()
        {
            int seed = mapConfig != null ? mapConfig.Seed : 0;
            if (seed == 0)
            {
                seed = Environment.TickCount;
            }

            ActMap map = mapConfig != null
                ? RunMapGenerator.Generate(mapConfig, seed)
                : RunMapGenerator.GenerateAct1();

            if (enemyPoolConfig != null)
            {
                RunMapGenerator.AssignEnemies(map, enemyPoolConfig, seed);
            }

            // Eventos asignados con la MISMA seed → "misma seed = mismo mapa,
            // mismos enemigos, mismos eventos". No-op si no hay pool (los nodos
            // Event caen al panel genérico).
            if (eventPoolConfig != null)
            {
                RunMapGenerator.AssignEvents(map, eventPoolConfig, seed);
            }

            return map;
        }

        private void AssignBossAct1()
        {
            if (bossAct1Enemy == null || Map == null)
            {
                return;
            }

            foreach (MapNode node in Map.Nodes)
            {
                if (node.Type == NodeType.Boss)
                {
                    node.SpecificEnemy = bossAct1Enemy;
                    break;
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

            bool copiedConfigs = false;
            if (mapConfig == null && source.mapConfig != null)
            {
                mapConfig = source.mapConfig;
                copiedConfigs = true;
            }

            if (enemyPoolConfig == null && source.enemyPoolConfig != null)
            {
                enemyPoolConfig = source.enemyPoolConfig;
                copiedConfigs = true;
            }

            if (eventPoolConfig == null && source.eventPoolConfig != null)
            {
                eventPoolConfig = source.eventPoolConfig;
                copiedConfigs = true;
            }

            if (copiedConfigs)
            {
                Map = GenerateMap();
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

            // 4a: resolver afinidad SOLO al poblar el mazo por primera vez. El guard
            // Deck.Count==0 evita re-crear (y descartar) ScriptableObjects runtime en
            // cada combate — InitializeDeck ya no-opea tras el primero, así que sin
            // este guard resolveríamos y tiraríamos SOs cada vez (leak de instancias).
            // Los tipos por mundo ya están en RunState (los escribe NewRunScene vía
            // StarterDraft.ApplySelectionToState antes de cargar RunScene); si se
            // entra directo a BattleScene caen a los defaults de RunState (no crashea).
            if (State.Deck.Count == 0)
            {
                var resolved = AffinityResolver.ResolveDeck(
                    config.StarterDeck, State.PlayerWorldAType, State.PlayerWorldBType);
                State.InitializeDeck(resolved);
            }
        }
    }
}

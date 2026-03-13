using System;
using System.Collections.Generic;
using RoguelikeCardBattler.Gameplay.Enemies;
using UnityEngine;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Genera mapas de acto para el run. Soporta generación aleatoria con pesos
    /// para tipos de nodo y múltiples templates de topología.
    /// También asigna enemigos a nodos Combat/Elite basándose en profundidad BFS.
    /// Seed-based: misma seed produce el mismo mapa y mismos enemigos.
    /// </summary>
    public static class RunMapGenerator
    {
        /// <summary>
        /// Templates de topología: cada uno es un array de aristas dirigidas {from, to}.
        /// Todos garantizan: 8 nodos (0-7), DAG, al menos 1 bifurcación + 1 convergencia,
        /// nodo 0 = start (sin entrantes), nodo 7 = end (sin salientes).
        /// </summary>
        private static readonly int[][][] Topologies =
        {
            // A: doble diamante — 0→(1,2)→3→(4,5)→6→7
            new[]
            {
                new[] { 0, 1 }, new[] { 0, 2 }, new[] { 1, 3 }, new[] { 2, 3 },
                new[] { 3, 4 }, new[] { 3, 5 }, new[] { 4, 6 }, new[] { 5, 6 },
                new[] { 6, 7 }
            },
            // B: split tardío — 0→1→(2,3)→4→(5,6)→7
            new[]
            {
                new[] { 0, 1 }, new[] { 1, 2 }, new[] { 1, 3 }, new[] { 2, 4 },
                new[] { 3, 4 }, new[] { 4, 5 }, new[] { 4, 6 }, new[] { 5, 7 },
                new[] { 6, 7 }
            },
            // C: triple temprano — 0→(1,2,3)→(4,5)→6→7
            new[]
            {
                new[] { 0, 1 }, new[] { 0, 2 }, new[] { 0, 3 }, new[] { 1, 4 },
                new[] { 2, 4 }, new[] { 3, 5 }, new[] { 4, 6 }, new[] { 5, 6 },
                new[] { 6, 7 }
            },
        };

        /// <summary>
        /// Genera un mapa usando la configuración data-driven (pesos, seed, constraints).
        /// Si seed es 0, genera uno distinto cada run.
        /// </summary>
        public static ActMap Generate(Act1MapConfig config, int? seedOverride = null)
        {
            int seed = seedOverride ?? config.Seed;
            if (seed == 0)
            {
                seed = Environment.TickCount;
            }

            System.Random rng = new System.Random(seed);
            ActMap map = new ActMap(startNodeId: 0);

            int templateIndex = rng.Next(Topologies.Length);
            int[][] edges = Topologies[templateIndex];

            NodeType[] types = AssignNodeTypes(config, rng);

            for (int i = 0; i < config.TotalNodes; i++)
            {
                map.Nodes.Add(new MapNode(i, types[i]));
            }

            foreach (int[] edge in edges)
            {
                MapNode from = map.GetNode(edge[0]);
                if (from != null)
                {
                    from.Connections.Add(edge[1]);
                }
            }

#if UNITY_EDITOR
            Debug.Log($"[MapGen] seed={seed}, template={templateIndex}, types=[{string.Join(",", types)}]");
#endif

            return map;
        }

        /// <summary>
        /// Backward compatible: genera un mapa con defaults si no hay config asignado.
        /// </summary>
        public static ActMap GenerateAct1()
        {
            Act1MapConfig config = ScriptableObject.CreateInstance<Act1MapConfig>();
            ActMap map = Generate(config);
            UnityEngine.Object.DestroyImmediate(config);
            return map;
        }

        /// <summary>
        /// Asigna tipos a los nodos respetando tipos forzados (start/end)
        /// y la restricción de mínimo de nodos Combat.
        /// </summary>
        private static NodeType[] AssignNodeTypes(Act1MapConfig config, System.Random rng)
        {
            int total = config.TotalNodes;
            NodeType[] types = new NodeType[total];

            types[0] = config.ForcedStartType;
            types[total - 1] = config.ForcedEndType;

            List<NodeTypeWeight> pool = new List<NodeTypeWeight>();
            float totalWeight = 0f;
            foreach (NodeTypeWeight nw in config.NodeTypeWeights)
            {
                if (nw.type == NodeType.Boss)
                {
                    continue;
                }

                pool.Add(nw);
                totalWeight += nw.weight;
            }

            for (int i = 1; i < total - 1; i++)
            {
                types[i] = WeightedRandom(pool, totalWeight, rng);
            }

            int combatCount = 0;
            for (int i = 0; i < total; i++)
            {
                if (types[i] == NodeType.Combat)
                {
                    combatCount++;
                }
            }

            int needed = config.MinCombatNodes - combatCount;
            if (needed > 0)
            {
                List<int> candidates = new List<int>();
                for (int i = 1; i < total - 1; i++)
                {
                    if (types[i] != NodeType.Combat)
                    {
                        candidates.Add(i);
                    }
                }

                Shuffle(candidates, rng);
                for (int j = 0; j < needed && j < candidates.Count; j++)
                {
                    types[candidates[j]] = NodeType.Combat;
                }
            }

            return types;
        }

        private static NodeType WeightedRandom(List<NodeTypeWeight> pool,
            float totalWeight, System.Random rng)
        {
            float roll = (float)(rng.NextDouble() * totalWeight);
            float cumulative = 0f;
            foreach (NodeTypeWeight nw in pool)
            {
                cumulative += nw.weight;
                if (roll <= cumulative)
                {
                    return nw.type;
                }
            }

            return pool.Count > 0 ? pool[pool.Count - 1].type : NodeType.Combat;
        }

        private static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Assigns enemies to Combat/Elite nodes based on BFS depth and weighted pools.
        /// Boss nodes are skipped (handled by RunSession.AssignBossAct1).
        /// Uses its own System.Random from the provided seed for determinism
        /// independent of the map topology generator.
        /// </summary>
        public static void AssignEnemies(ActMap map, EnemyPoolConfig pool, int seed)
        {
            if (pool == null || map == null)
            {
                return;
            }

            System.Random rng = new System.Random(seed);
            Dictionary<int, int> depths = ComputeNodeDepths(map);

            foreach (MapNode node in map.Nodes)
            {
                if (node.Type == NodeType.Boss)
                {
                    continue;
                }

                if (node.Type != NodeType.Combat && node.Type != NodeType.Elite)
                {
                    continue;
                }

                int depth = depths.ContainsKey(node.Id) ? depths[node.Id] : 0;
                DepthPool dp = pool.GetPoolForDepth(depth);

                if (dp != null && dp.entries.Count > 0)
                {
                    node.SpecificEnemy = WeightedRandomEnemy(dp.entries, rng);
                }
                else if (pool.FallbackEnemy != null)
                {
                    node.SpecificEnemy = pool.FallbackEnemy;
                }
            }

#if UNITY_EDITOR
            Debug.Log($"[MapGen] AssignEnemies seed={seed}, depths=[{FormatDepths(depths)}]");
#endif
        }

        /// <summary>
        /// BFS from StartNodeId to compute each node's depth (distance from start).
        /// Same algorithm as RunMapView.ComputeDepths, duplicated here to keep
        /// the generator independent of UI code.
        /// </summary>
        private static Dictionary<int, int> ComputeNodeDepths(ActMap map)
        {
            Dictionary<int, int> depths = new Dictionary<int, int>();
            Queue<int> queue = new Queue<int>();
            depths[map.StartNodeId] = 0;
            queue.Enqueue(map.StartNodeId);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                MapNode node = map.GetNode(current);
                if (node == null)
                {
                    continue;
                }

                int nextDepth = depths[current] + 1;
                foreach (int conn in node.Connections)
                {
                    if (!depths.ContainsKey(conn))
                    {
                        depths[conn] = nextDepth;
                        queue.Enqueue(conn);
                    }
                }
            }

            return depths;
        }

        private static EnemyDefinition WeightedRandomEnemy(
            List<EnemyWeightEntry> entries, System.Random rng)
        {
            float totalWeight = 0f;
            foreach (EnemyWeightEntry e in entries)
            {
                totalWeight += e.weight;
            }

            float roll = (float)(rng.NextDouble() * totalWeight);
            float cumulative = 0f;
            foreach (EnemyWeightEntry e in entries)
            {
                cumulative += e.weight;
                if (roll <= cumulative)
                {
                    return e.enemy;
                }
            }

            return entries.Count > 0 ? entries[entries.Count - 1].enemy : null;
        }

#if UNITY_EDITOR
        private static string FormatDepths(Dictionary<int, int> depths)
        {
            List<string> parts = new List<string>();
            foreach (KeyValuePair<int, int> kv in depths)
            {
                parts.Add($"{kv.Key}:d{kv.Value}");
            }

            return string.Join(",", parts);
        }
#endif
    }
}

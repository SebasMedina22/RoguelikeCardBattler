using System;
using System.Collections.Generic;
using RoguelikeCardBattler.Gameplay.Enemies;
using UnityEngine;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Weighted entry pairing an EnemyDefinition with a selection weight.
    /// Used inside <see cref="DepthPool"/> to control spawn probability.
    /// </summary>
    [Serializable]
    public struct EnemyWeightEntry
    {
        public EnemyDefinition enemy;
        [Min(0f)] public float weight;
    }

    /// <summary>
    /// Pool of enemies for a depth range. Nodes whose BFS depth falls within
    /// [minDepth, maxDepth] draw from this pool's weighted entries.
    /// </summary>
    [Serializable]
    public class DepthPool
    {
        public string label;
        public int minDepth;
        public int maxDepth;
        public List<EnemyWeightEntry> entries = new List<EnemyWeightEntry>();
    }

    /// <summary>
    /// Data-driven configuration for enemy selection per map node.
    /// Depth pools map BFS distance from the start node to weighted enemy lists,
    /// allowing early nodes to spawn easier enemies and late nodes harder ones.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyPoolConfig", menuName = "Run/Map/EnemyPoolConfig")]
    public class EnemyPoolConfig : ScriptableObject
    {
        [SerializeField] private List<DepthPool> depthPools = new List<DepthPool>();

        [Tooltip("Used when a node's depth doesn't match any pool")]
        [SerializeField] private EnemyDefinition fallbackEnemy;

        public IReadOnlyList<DepthPool> DepthPools => depthPools;
        public EnemyDefinition FallbackEnemy => fallbackEnemy;

        /// <summary>
        /// Returns the first pool whose depth range contains <paramref name="depth"/>, or null.
        /// </summary>
        public DepthPool GetPoolForDepth(int depth)
        {
            foreach (DepthPool pool in depthPools)
            {
                if (depth >= pool.minDepth && depth <= pool.maxDepth)
                {
                    return pool;
                }
            }

            return null;
        }
    }
}

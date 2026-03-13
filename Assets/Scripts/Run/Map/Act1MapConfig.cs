using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Peso para un tipo de nodo en la generación aleatoria del mapa.
    /// Mayor weight = más probable que aparezca.
    /// </summary>
    [Serializable]
    public struct NodeTypeWeight
    {
        public NodeType type;
        [Min(0f)] public float weight;
    }

    /// <summary>
    /// Configuración data-driven para la generación de mapas del Acto 1.
    /// Controla cantidad de nodos, pesos de tipos, seed y restricciones
    /// estructurales. Asignable en Inspector para iterar sin tocar código.
    /// </summary>
    [CreateAssetMenu(fileName = "Act1MapConfig", menuName = "Run/Map/Act1MapConfig")]
    public class Act1MapConfig : ScriptableObject
    {
        [Header("Structure")]
        [SerializeField] private int totalNodes = 8;
        [SerializeField] private int minCombatNodes = 3;
        [SerializeField] private NodeType forcedStartType = NodeType.Combat;
        [SerializeField] private NodeType forcedEndType = NodeType.Boss;

        [Header("Randomization")]
        [Tooltip("0 = random each run, >0 = fixed seed for reproducibility")]
        [SerializeField] private int seed;

        [Header("Type Weights")]
        [SerializeField] private List<NodeTypeWeight> nodeTypeWeights = new List<NodeTypeWeight>
        {
            new NodeTypeWeight { type = NodeType.Combat, weight = 40f },
            new NodeTypeWeight { type = NodeType.Event, weight = 20f },
            new NodeTypeWeight { type = NodeType.Shop, weight = 15f },
            new NodeTypeWeight { type = NodeType.Campfire, weight = 20f },
            new NodeTypeWeight { type = NodeType.Elite, weight = 5f },
        };

        public int TotalNodes => totalNodes;
        public int MinCombatNodes => minCombatNodes;
        public NodeType ForcedStartType => forcedStartType;
        public NodeType ForcedEndType => forcedEndType;
        public int Seed => seed;
        public IReadOnlyList<NodeTypeWeight> NodeTypeWeights => nodeTypeWeights;
    }
}

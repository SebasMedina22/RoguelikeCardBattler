using System.Collections.Generic;
using RoguelikeCardBattler.Gameplay.Enemies;
using RoguelikeCardBattler.Run.Events;

namespace RoguelikeCardBattler.Run
{
    public class MapNode
    {
        public int Id { get; }
        public NodeType Type { get; }
        public List<int> Connections { get; } = new List<int>();
        public EnemyDefinition SpecificEnemy { get; set; }

        // Evento asignado a este nodo (sólo NodeType.Event). Lo fija
        // RunMapGenerator.AssignEvents por seed al generar el mapa (paralelo a
        // SpecificEnemy). Null en nodos no-Event o si no hay EventPoolConfig.
        public EventDefinition AssignedEvent { get; set; }

        public MapNode(int id, NodeType type)
        {
            Id = id;
            Type = type;
        }
    }
}

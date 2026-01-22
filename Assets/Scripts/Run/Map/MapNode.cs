using System.Collections.Generic;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Run
{
    public class MapNode
    {
        public int Id { get; }
        public NodeType Type { get; }
        public List<int> Connections { get; } = new List<int>();
        public EnemyDefinition SpecificEnemy { get; set; }

        public MapNode(int id, NodeType type)
        {
            Id = id;
            Type = type;
        }
    }
}

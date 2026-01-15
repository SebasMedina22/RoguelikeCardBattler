using System.Collections.Generic;

namespace RoguelikeCardBattler.Run
{
    public class MapNode
    {
        public int Id { get; }
        public NodeType Type { get; }
        public List<int> Connections { get; } = new List<int>();

        public MapNode(int id, NodeType type)
        {
            Id = id;
            Type = type;
        }
    }
}

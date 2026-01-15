using System.Collections.Generic;
using System.Linq;

namespace RoguelikeCardBattler.Run
{
    public class ActMap
    {
        public int StartNodeId { get; }
        public List<MapNode> Nodes { get; } = new List<MapNode>();

        public ActMap(int startNodeId)
        {
            StartNodeId = startNodeId;
        }

        public MapNode GetNode(int id) => Nodes.FirstOrDefault(node => node.Id == id);

        public IEnumerable<MapNode> GetAvailableNodes(RunState state)
        {
            foreach (MapNode node in Nodes)
            {
                if (state.IsNodeAvailable(node.Id))
                {
                    yield return node;
                }
            }
        }
    }
}

using System.Collections.Generic;

namespace RoguelikeCardBattler.Run.Quests
{
    /// <summary>
    /// Resuelve el nodo destino del quest mediante BFS forward desde el nodo del evento.
    /// Static puro (testeable sin UI). Golden Rule §7: RunState no calcula BFS.
    ///
    /// Regla de selección (spec §Resolución del nodo destino):
    ///   1. BFS forward desde questNodeId por MapNode.Connections.
    ///   2. Candidatos = alcanzables, != questNodeId, ∉ completedNodes, tipo != Boss.
    ///   3. Preferido: mayor forwardDepth; desempate = menor Id (determinista, sin RNG).
    ///   4. Fallback (sin candidatos): el Boss, siempre alcanzable por construcción.
    /// </summary>
    public static class QuestDestinationResolver
    {
        public static int SelectDestination(ActMap map, int questNodeId, ISet<int> completedNodes)
        {
            if (map == null) return -1;

            // BFS forward desde questNodeId
            var forwardDepth = new Dictionary<int, int>();
            var queue = new Queue<int>();
            forwardDepth[questNodeId] = 0;
            queue.Enqueue(questNodeId);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                MapNode node = map.GetNode(current);
                if (node == null) continue;
                int nextDepth = forwardDepth[current] + 1;
                foreach (int conn in node.Connections)
                {
                    if (!forwardDepth.ContainsKey(conn))
                    {
                        forwardDepth[conn] = nextDepth;
                        queue.Enqueue(conn);
                    }
                }
            }

            // Candidatos: alcanzables, != questNodeId, no completados, no Boss
            int bestId = -1;
            int bestDepth = -1;
            foreach (KeyValuePair<int, int> kvp in forwardDepth)
            {
                int nodeId = kvp.Key;
                int depth = kvp.Value;
                if (nodeId == questNodeId) continue;
                if (completedNodes != null && completedNodes.Contains(nodeId)) continue;
                MapNode candidate = map.GetNode(nodeId);
                if (candidate == null || candidate.Type == NodeType.Boss) continue;

                // Preferir el más lejano (mayor depth); desempatar por menor Id
                if (depth > bestDepth || (depth == bestDepth && (bestId < 0 || nodeId < bestId)))
                {
                    bestDepth = depth;
                    bestId = nodeId;
                }
            }

            // Fallback al Boss (siempre alcanzable)
            if (bestId < 0)
            {
                foreach (MapNode node in map.Nodes)
                {
                    if (node.Type == NodeType.Boss) return node.Id;
                }
                return 7; // convención del proyecto: nodo 7 = Boss/end
            }

            return bestId;
        }
    }
}

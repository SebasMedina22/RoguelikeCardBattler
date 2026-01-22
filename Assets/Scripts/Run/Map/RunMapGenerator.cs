namespace RoguelikeCardBattler.Run
{
    public static class RunMapGenerator
    {
        public static ActMap GenerateAct1()
        {
            var map = new ActMap(startNodeId: 0);

            // 8 nodos con bifurcación y convergencia:
            // 0 -> (1,2) -> 3 -> (4,5) -> 6 -> 7
            map.Nodes.Add(new MapNode(0, NodeType.Combat));
            map.Nodes.Add(new MapNode(1, NodeType.Event));
            map.Nodes.Add(new MapNode(2, NodeType.Shop));
            map.Nodes.Add(new MapNode(3, NodeType.Combat));
            map.Nodes.Add(new MapNode(4, NodeType.Campfire));
            map.Nodes.Add(new MapNode(5, NodeType.Combat));
            map.Nodes.Add(new MapNode(6, NodeType.Event));
            
            // Nodo Boss - el enemigo específico será asignado desde RunSession
            MapNode bossNode = new MapNode(7, NodeType.Boss);
            map.Nodes.Add(bossNode);

            map.GetNode(0).Connections.Add(1);
            map.GetNode(0).Connections.Add(2);
            map.GetNode(1).Connections.Add(3);
            map.GetNode(2).Connections.Add(3);
            map.GetNode(3).Connections.Add(4);
            map.GetNode(3).Connections.Add(5);
            map.GetNode(4).Connections.Add(6);
            map.GetNode(5).Connections.Add(6);
            map.GetNode(6).Connections.Add(7);

            return map;
        }
    }
}

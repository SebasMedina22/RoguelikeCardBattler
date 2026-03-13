using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Construye y gestiona el mapa visual 2D ramificado dentro de RunScene.
    /// Posiciona nodos automáticamente por profundidad BFS y dibuja edges
    /// entre nodos conectados. RunFlowController delega la presentación aquí.
    /// </summary>
    public class RunMapView
    {
        private const float RowSpacing = 130f;
        private const float HorizontalSpacing = 200f;
        private const float TopPadding = 50f;
        private const float BottomPadding = 50f;
        private const float NodeWidth = 130f;
        private const float NodeHeight = 50f;
        private const float EdgeThickness = 3f;

        public event Action<int> OnNodeClicked;

        private readonly Dictionary<int, RunMapNodeView> _nodeViews =
            new Dictionary<int, RunMapNodeView>();
        private readonly List<RunMapEdgeView> _edgeViews =
            new List<RunMapEdgeView>();

        /// <summary>
        /// Construye todo el mapa visual: calcula layout, crea edges y nodos.
        /// </summary>
        public RunMapView(RectTransform parent, ActMap map, RunState state,
            Font font, Sprite whiteSprite)
        {
            RectTransform content = CreateScrollView(parent, whiteSprite);

            Dictionary<int, int> depthMap = ComputeDepths(map);
            int maxDepth = 0;
            Dictionary<int, List<int>> depthBuckets = new Dictionary<int, List<int>>();

            foreach (KeyValuePair<int, int> kvp in depthMap)
            {
                if (kvp.Value > maxDepth)
                {
                    maxDepth = kvp.Value;
                }

                if (!depthBuckets.ContainsKey(kvp.Value))
                {
                    depthBuckets[kvp.Value] = new List<int>();
                }

                depthBuckets[kvp.Value].Add(kvp.Key);
            }

            float contentHeight = TopPadding + (maxDepth + 1) * RowSpacing + BottomPadding;
            content.sizeDelta = new Vector2(0f, contentHeight);

            foreach (MapNode node in map.Nodes)
            {
                if (!depthMap.ContainsKey(node.Id))
                {
                    continue;
                }

                Vector2 fromPos = GetNodePosition(node.Id, depthMap, depthBuckets);
                foreach (int connId in node.Connections)
                {
                    if (!depthMap.ContainsKey(connId))
                    {
                        continue;
                    }

                    Vector2 toPos = GetNodePosition(connId, depthMap, depthBuckets);
                    RunMapEdgeView edge = RunMapEdgeView.Create(
                        content, fromPos, toPos, EdgeThickness, whiteSprite);
                    _edgeViews.Add(edge);
                }
            }

            foreach (MapNode node in map.Nodes)
            {
                if (!depthMap.ContainsKey(node.Id))
                {
                    continue;
                }

                Vector2 pos = GetNodePosition(node.Id, depthMap, depthBuckets);
                RunMapNodeView nodeView = new RunMapNodeView(
                    node.Id, node.Type, content, pos,
                    new Vector2(NodeWidth, NodeHeight), font, whiteSprite,
                    id => OnNodeClicked?.Invoke(id));
                _nodeViews[node.Id] = nodeView;
            }

            Refresh(state);
        }

        /// <summary>
        /// Actualiza los estados visuales de todos los nodos sin reconstruir el mapa.
        /// Se llama cada vez que el mapa se muestra (ShowMap) o tras completar un nodo.
        /// </summary>
        public void Refresh(RunState state)
        {
            foreach (KeyValuePair<int, RunMapNodeView> kvp in _nodeViews)
            {
                NodeState vs;
                if (state.IsNodeCompleted(kvp.Key))
                {
                    vs = NodeState.Completed;
                }
                else if (state.IsNodeAvailable(kvp.Key))
                {
                    vs = NodeState.Available;
                }
                else
                {
                    vs = NodeState.Locked;
                }

                kvp.Value.ApplyState(vs);
            }
        }

        /// <summary>
        /// BFS desde startNode para calcular la profundidad de cada nodo.
        /// Se usa para posicionar nodos en filas (Y) por profundidad.
        /// </summary>
        private static Dictionary<int, int> ComputeDepths(ActMap map)
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

        /// <summary>
        /// Calcula la posición de un nodo en coordenadas del content (anchor 0.5, 1).
        /// X=0 es el centro horizontal; Y negativo va hacia abajo.
        /// Nodos del mismo depth se distribuyen simétricamente alrededor del centro.
        /// </summary>
        private static Vector2 GetNodePosition(int nodeId,
            Dictionary<int, int> depthMap, Dictionary<int, List<int>> depthBuckets)
        {
            int depth = depthMap[nodeId];
            List<int> bucket = depthBuckets[depth];
            int index = bucket.IndexOf(nodeId);
            int count = bucket.Count;

            float x = (index - (count - 1) / 2f) * HorizontalSpacing;
            float y = -(TopPadding + depth * RowSpacing);

            return new Vector2(x, y);
        }

        private static RectTransform CreateScrollView(RectTransform parent,
            Sprite whiteSprite)
        {
            GameObject scrollGO = new GameObject("MapScrollView",
                typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGO.transform.SetParent(parent, false);

            RectTransform scrollRect = scrollGO.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.02f, 0.02f);
            scrollRect.anchorMax = new Vector2(0.98f, 0.78f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            Image bg = scrollGO.GetComponent<Image>();
            bg.sprite = whiteSprite;
            bg.type = Image.Type.Simple;
            bg.color = new Color(0.03f, 0.04f, 0.07f, 0.35f);

            ScrollRect scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewportGO = new GameObject("Viewport",
                typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            RectTransform viewportRect = viewportGO.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage = viewportGO.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            viewportImage.raycastTarget = true;

            GameObject contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            RectTransform contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 800f);

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            return contentRect;
        }
    }
}

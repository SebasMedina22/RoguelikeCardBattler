using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Construye y gestiona el mapa visual 2D ramificado dentro de RunScene.
    /// Posiciona nodos automáticamente por profundidad BFS y dibuja edges
    /// entre nodos conectados. RunFlowController delega la presentación aquí.
    /// Also orchestrates juice animations: staggered entrance, pulse on
    /// available nodes, edge highlights on unlock.
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
        private const float EntranceDelayPerDepth = 0.08f;

        public event Action<int> OnNodeClicked;

        private readonly Dictionary<int, RunMapNodeView> _nodeViews =
            new Dictionary<int, RunMapNodeView>();
        private readonly List<RunMapEdgeView> _edgeViews =
            new List<RunMapEdgeView>();
        private readonly Dictionary<int, List<RunMapEdgeView>> _edgesByTarget =
            new Dictionary<int, List<RunMapEdgeView>>();
        private readonly Dictionary<int, int> _depthMap;

        /// <summary>
        /// Construye todo el mapa visual: calcula layout, crea edges y nodos,
        /// then plays the staggered entrance animation.
        /// </summary>
        public RunMapView(RectTransform parent, ActMap map, RunState state,
            Font font, Sprite whiteSprite)
        {
            RectTransform content = CreateScrollView(parent, whiteSprite);

            _depthMap = ComputeDepths(map);
            int maxDepth = 0;
            Dictionary<int, List<int>> depthBuckets = new Dictionary<int, List<int>>();

            foreach (KeyValuePair<int, int> kvp in _depthMap)
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
                if (!_depthMap.ContainsKey(node.Id))
                {
                    continue;
                }

                Vector2 fromPos = GetNodePosition(node.Id, _depthMap, depthBuckets);
                foreach (int connId in node.Connections)
                {
                    if (!_depthMap.ContainsKey(connId))
                    {
                        continue;
                    }

                    Vector2 toPos = GetNodePosition(connId, _depthMap, depthBuckets);
                    RunMapEdgeView edge = RunMapEdgeView.Create(
                        content, fromPos, toPos, EdgeThickness, whiteSprite);
                    _edgeViews.Add(edge);

                    if (!_edgesByTarget.ContainsKey(connId))
                    {
                        _edgesByTarget[connId] = new List<RunMapEdgeView>();
                    }

                    _edgesByTarget[connId].Add(edge);
                }
            }

            foreach (MapNode node in map.Nodes)
            {
                if (!_depthMap.ContainsKey(node.Id))
                {
                    continue;
                }

                Vector2 pos = GetNodePosition(node.Id, _depthMap, depthBuckets);
                RunMapNodeView nodeView = new RunMapNodeView(
                    node.Id, node.Type, content, pos,
                    new Vector2(NodeWidth, NodeHeight), font, whiteSprite,
                    id => OnNodeClicked?.Invoke(id));
                _nodeViews[node.Id] = nodeView;
            }

            // Play entrance FIRST so _entrancePlaying is true before Refresh.
            // Refresh applies colors only (skips PulseLoop) during entrance.
            // When each node's entrance completes, it re-calls ApplyState to start PulseLoop.
            PlayEntranceAnimation();
            Refresh(state);
        }

        /// <summary>
        /// Actualiza los estados visuales de todos los nodos sin reconstruir el mapa.
        /// Detects state transitions: when a node becomes Available, incoming edges
        /// get a golden highlight animation.
        /// </summary>
        public void Refresh(RunState state)
        {
            foreach (KeyValuePair<int, RunMapNodeView> kvp in _nodeViews)
            {
                NodeState newState;
                if (state.IsNodeCompleted(kvp.Key))
                {
                    newState = NodeState.Completed;
                }
                else if (state.IsNodeAvailable(kvp.Key))
                {
                    newState = NodeState.Available;
                }
                else
                {
                    newState = NodeState.Locked;
                }

                NodeState oldState = kvp.Value.CurrentState;
                kvp.Value.ApplyState(newState);

                if (newState == NodeState.Available && oldState != NodeState.Available)
                {
                    if (_edgesByTarget.TryGetValue(kvp.Key, out List<RunMapEdgeView> edges))
                    {
                        foreach (RunMapEdgeView edge in edges)
                        {
                            edge.AnimateHighlight();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Kills all DOTween tweens on nodes and edges. Call before transitioning
        /// to another scene to avoid "destroyed RectTransform" / Safe Mode warnings.
        /// </summary>
        public void Cleanup()
        {
            foreach (RunMapNodeView nodeView in _nodeViews.Values)
            {
                DOTween.Kill(nodeView.Rect);
            }

            foreach (RunMapEdgeView edge in _edgeViews)
            {
                edge.KillTweens();
            }
        }

        /// <summary>
        /// Plays the staggered entrance: nodes and edges appear sequentially
        /// by BFS depth (start node first, boss last). Each depth layer is
        /// delayed by <see cref="EntranceDelayPerDepth"/> seconds.
        /// </summary>
        private void PlayEntranceAnimation()
        {
            foreach (KeyValuePair<int, RunMapNodeView> kvp in _nodeViews)
            {
                int depth = _depthMap.ContainsKey(kvp.Key) ? _depthMap[kvp.Key] : 0;
                kvp.Value.PlayEntrance(depth * EntranceDelayPerDepth);
            }

            int edgeIndex = 0;
            foreach (RunMapEdgeView edge in _edgeViews)
            {
                float delay = edgeIndex * EntranceDelayPerDepth * 0.5f;
                edge.PlayEntrance(delay);
                edgeIndex++;
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

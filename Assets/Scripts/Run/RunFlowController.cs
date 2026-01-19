using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Controlador mínimo del loop de run: mapa -> nodo -> resultado -> mapa.
    /// Construye UI simple en runtime leyendo/escribiendo RunState.
    /// </summary>
    public class RunFlowController : MonoBehaviour
    {
        [SerializeField] private Font uiFont;

        private RunState _state;
        private ActMap _map;
        private Canvas _canvas;
        private RectTransform _root;
        private RectTransform _mapPanel;
        private RectTransform _resolvePanel;
        private Text _statusText;
        private Text _titleText;
        private readonly List<Button> _nodeButtons = new List<Button>();
        private Text _resolveTitleText;
        private Text _resolveBodyText;
        private Button _resolveCompleteButton;
        private Button _resolveBackButton;
        private int _resolveNodeId = -1;
        private static Sprite _whiteSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInRunScene()
        {
            if (SceneManager.GetActiveScene().name != "RunScene")
            {
                return;
            }

            if (Object.FindFirstObjectByType<RunFlowController>() != null)
            {
                return;
            }

            GameObject go = new GameObject("RunFlowController");
            go.AddComponent<RunFlowController>();
        }

        private void Awake()
        {
            RunSession session = RunSession.GetOrCreate();
            _state = session.State;
            _map = session.Map;
            _state.EnsureInitialized(_map);

            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            EnsureEventSystem();
            BuildUI();
            ShowMap();
            PrintMapDebugOnce();
        }

        private void BuildUI()
        {
            _canvas = CreateCanvas("RunCanvas");
            _root = _canvas.GetComponent<RectTransform>();

            _mapPanel = CreatePanel("MapPanel", _root, new Color(0f, 0f, 0f, 0f), false);
            _resolvePanel = CreatePanel("NodeResolvePanel", _root, new Color(0f, 0f, 0f, 0.6f), true);
            _resolvePanel.gameObject.SetActive(false);

            _titleText = CreateText("Title", _mapPanel, "Run - Acto 1 (placeholder)", 28, TextAnchor.UpperCenter);
            RectTransform titleRect = _titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.88f);
            titleRect.anchorMax = new Vector2(0.9f, 0.98f);

            _statusText = CreateText("Status", _mapPanel, "Gold: 0 | Completados: 0/3", 20, TextAnchor.UpperCenter);
            RectTransform statusRect = _statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.1f, 0.8f);
            statusRect.anchorMax = new Vector2(0.9f, 0.88f);

            CreateNodeButtons(CreateNodeScrollView(_mapPanel));
            BuildResolvePanel();

#if UNITY_EDITOR
            RectTransform contentRect = _mapPanel.transform.Find("NodeScrollView/Viewport/Content") as RectTransform;
            if (contentRect != null)
            {
                Debug.Log($"[RunUI Debug] Content height: {contentRect.rect.height}, children: {contentRect.childCount}");
            }
#endif
        }

        private void EnsureEventSystem()
        {
            EventSystem[] all = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            EventSystem existing = all.Length > 0 ? all[0] : null;

#if UNITY_EDITOR
            if (all.Length > 1)
            {
                Debug.LogWarning($"[RunUI] Multiple EventSystems detected: {all.Length}. Keeping the first one.");
            }
#endif

            for (int i = 1; i < all.Length; i++)
            {
                if (all[i] != null)
                {
                    Destroy(all[i].gameObject);
                }
            }

            GameObject target = existing != null ? existing.gameObject : new GameObject("EventSystem", typeof(EventSystem));

            System.Type inputSystemType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemType != null)
            {
                StandaloneInputModule legacy = target.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                {
                    Destroy(legacy);
                }

                if (target.GetComponent(inputSystemType) == null)
                {
                    target.AddComponent(inputSystemType);
                }

#if UNITY_EDITOR
                Debug.Log("[RunUI] EventSystem module: InputSystemUIInputModule");
#endif
                return;
            }

            if (target.GetComponent<StandaloneInputModule>() == null)
            {
                foreach (Component component in target.GetComponents<Component>())
                {
                    if (component != null && component.GetType().FullName == "UnityEngine.InputSystem.UI.InputSystemUIInputModule")
                    {
                        Destroy(component);
                    }
                }

                target.AddComponent<StandaloneInputModule>();
            }

#if UNITY_EDITOR
            Debug.Log("[RunUI] EventSystem module: StandaloneInputModule");
#endif
        }

        private RectTransform CreateNodeScrollView(RectTransform parent)
        {
            GameObject scrollGO = new GameObject("NodeScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGO.transform.SetParent(parent, false);

            RectTransform scrollRect = scrollGO.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.1f, 0.25f);
            scrollRect.anchorMax = new Vector2(0.9f, 0.78f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            Image bg = scrollGO.GetComponent<Image>();
            bg.sprite = GetWhiteSprite();
            bg.type = Image.Type.Simple;
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.3f);

            ScrollRect scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            RectTransform viewportRect = viewportGO.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage = viewportGO.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            viewportImage.raycastTarget = true;

            GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            RectTransform contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentGO.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childControlWidth = true;

            ContentSizeFitter fitter = contentGO.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            return contentRect;
        }

        private void CreateNodeButtons(RectTransform parent)
        {
            for (int i = 0; i < _map.Nodes.Count; i++)
            {
                MapNode node = _map.Nodes[i];
                int nodeId = node.Id;
                Button button = CreateNodeButton(parent, $"Node_{nodeId}", $"Nodo {nodeId + 1}");
                button.onClick.AddListener(() => EnterNode(nodeId));
                _nodeButtons.Add(button);
            }
        }

        private Button CreateNodeButton(RectTransform parent, string name, string label)
        {
            GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGO.transform.SetParent(parent, false);
            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            LayoutElement layout = buttonGO.GetComponent<LayoutElement>();
            layout.preferredHeight = 70f;
            layout.minHeight = 60f;

            Image image = buttonGO.GetComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.type = Image.Type.Simple;
            image.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            image.raycastTarget = true;

            GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(buttonGO.transform, false);
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textGO.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            text.raycastTarget = false;

            Outline outline = textGO.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(1f, -1f);

            return buttonGO.GetComponent<Button>();
        }

        private void ShowMap()
        {
            _resolvePanel.gameObject.SetActive(false);
            _mapPanel.gameObject.SetActive(true);
            _titleText.text = "Mapa Acto 1 (placeholder)";

            int completed = 0;
            foreach (MapNode node in _map.Nodes)
            {
                if (_state.IsNodeCompleted(node.Id))
                {
                    completed++;
                }
            }

            _statusText.text = $"Gold: {_state.Gold} | Completados: {completed}/{_map.Nodes.Count}";

            for (int i = 0; i < _nodeButtons.Count; i++)
            {
                Button button = _nodeButtons[i];
                int nodeId = _map.Nodes[i].Id;
                bool completedNode = _state.IsNodeCompleted(nodeId);
                bool available = _state.IsNodeAvailable(nodeId);

                button.gameObject.SetActive(true);
                button.interactable = available;

                Text label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    NodeType type = _map.Nodes[i].Type;
                    string typeLabel = $"{type}";
                    label.text = completedNode
                        ? $"Nodo {nodeId + 1} ({typeLabel}) ✓"
                        : $"Nodo {nodeId + 1} ({typeLabel})";
                }

                Image image = button.GetComponent<Image>();
                if (image != null)
                {
                    if (completedNode)
                    {
                        image.color = new Color(0.2f, 0.5f, 0.25f, 1f);
                    }
                    else if (available)
                    {
                        image.color = new Color(0.2f, 0.35f, 0.6f, 1f);
                    }
                    else
                    {
                        image.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
                    }
                }
            }
        }

        private void EnterNode(int index)
        {
            if (index < 0 || index >= _map.Nodes.Count)
            {
                return;
            }

            if (_state.IsNodeCompleted(index))
            {
                return;
            }

            if (!_state.IsNodeAvailable(index))
            {
                return;
            }

            _state.CurrentNodeId = index;
            _resolveNodeId = index;
            ShowResolvePanel(index);
        }

        private void OnContinue()
        {
            if (_state.CurrentNodeId < 0 || _state.CurrentNodeId >= _map.Nodes.Count)
            {
                ShowMap();
                return;
            }

            CompleteNode(_state.CurrentNodeId);
            _state.CurrentNodeId = -1;
            _state.Gold += 10;
            ShowMap();
        }

        private void ShowResolvePanel(int nodeId)
        {
            _mapPanel.gameObject.SetActive(false);
            _resolvePanel.gameObject.SetActive(true);
            _resolvePanel.SetAsLastSibling();

            MapNode node = _map.GetNode(nodeId);
            if (node != null)
            {
                _resolveTitleText.text = $"Nodo {node.Id + 1}";
                _resolveBodyText.text = $"Tipo: {node.Type}\n\nContenido placeholder.";
            }
        }

        private void OnResolveComplete()
        {
            if (_resolveNodeId < 0)
            {
                ShowMap();
                return;
            }

            CompleteNode(_resolveNodeId);
            _state.CurrentNodeId = -1;
            _resolveNodeId = -1;
            _state.Gold += 10;
            ShowMap();
        }

        private void OnResolveBack()
        {
            _resolveNodeId = -1;
            _state.CurrentNodeId = -1;
            ShowMap();
        }

        private void CompleteNode(int nodeId)
        {
            if (_state.IsNodeCompleted(nodeId))
            {
                return;
            }

            _state.CompletedNodes.Add(nodeId);
            _state.AvailableNodes.Clear();
            _state.CurrentPositionNodeId = nodeId;

            MapNode node = _map.GetNode(nodeId);
            if (node == null)
            {
                return;
            }

            foreach (int connection in node.Connections)
            {
                if (!_state.IsNodeCompleted(connection))
                {
                    _state.AvailableNodes.Add(connection);
                }
            }

            if (_state.AvailableNodes.Count == 0 && _state.CompletedNodes.Count < _map.Nodes.Count)
            {
                Debug.LogWarning("No available nodes remaining. Check map connections.");
            }
        }

        private void PrintMapDebugOnce()
        {
            if (_map == null)
            {
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("[ActMap Debug] Nodes:");
            foreach (MapNode node in _map.Nodes)
            {
                string connections = node.Connections.Count > 0
                    ? string.Join(", ", node.Connections)
                    : "none";
                sb.AppendLine($"- {node.Id}: {node.Type} -> [{connections}]");
            }
            Debug.Log(sb.ToString());
        }

        private Canvas CreateCanvas(string name)
        {
            GameObject canvasGO = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private Text CreateText(string name, RectTransform parent, string text, int fontSize, TextAnchor alignment)
        {
            GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(parent, false);
            RectTransform rect = textGO.GetComponent<RectTransform>();
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text label = textGO.GetComponent<Text>();
            label.font = uiFont;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.text = text;
            return label;
        }

        private Button CreateButton(string name, RectTransform parent, string label)
        {
            GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(parent, false);
            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.35f, 0.2f);
            rect.anchorMax = new Vector2(0.65f, 0.28f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonGO.GetComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.type = Image.Type.Simple;
            image.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(buttonGO.transform, false);
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textGO.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;

            return buttonGO.GetComponent<Button>();
        }

        private RectTransform CreatePanel(string name, RectTransform parent, Color color, bool blockRaycasts)
        {
            GameObject panelGO = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(parent, false);
            RectTransform rect = panelGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panelGO.GetComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = blockRaycasts;

            return rect;
        }

        private void BuildResolvePanel()
        {
            _resolveTitleText = CreateText("ResolveTitle", _resolvePanel, "Nodo", 26, TextAnchor.UpperCenter);
            RectTransform titleRect = _resolveTitleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.2f, 0.7f);
            titleRect.anchorMax = new Vector2(0.8f, 0.9f);

            _resolveBodyText = CreateText("ResolveBody", _resolvePanel, "Tipo:\n\nContenido placeholder.", 20, TextAnchor.UpperCenter);
            RectTransform bodyRect = _resolveBodyText.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.2f, 0.45f);
            bodyRect.anchorMax = new Vector2(0.8f, 0.7f);

            _resolveCompleteButton = CreateButton("ResolveComplete", _resolvePanel, "Completar");
            RectTransform completeRect = _resolveCompleteButton.GetComponent<RectTransform>();
            completeRect.anchorMin = new Vector2(0.3f, 0.25f);
            completeRect.anchorMax = new Vector2(0.7f, 0.33f);
            _resolveCompleteButton.onClick.AddListener(OnResolveComplete);

            _resolveBackButton = CreateButton("ResolveBack", _resolvePanel, "Volver");
            RectTransform backRect = _resolveBackButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.3f, 0.15f);
            backRect.anchorMax = new Vector2(0.7f, 0.23f);
            _resolveBackButton.onClick.AddListener(OnResolveBack);
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null)
            {
                return _whiteSprite;
            }

            _whiteSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }
    }
}

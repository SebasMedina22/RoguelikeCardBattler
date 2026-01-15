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
        private Canvas _canvas;
        private RectTransform _root;
        private Text _statusText;
        private Text _titleText;
        private readonly List<Button> _nodeButtons = new List<Button>();
        private Button _continueButton;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInRunScene()
        {
            if (SceneManager.GetActiveScene().name != "RunScene")
            {
                return;
            }

            if (FindObjectOfType<RunFlowController>() != null)
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
            _state.EnsureInitialized();

            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            EnsureEventSystem();
            BuildUI();
            ShowMap();
        }

        private void BuildUI()
        {
            _canvas = CreateCanvas("RunCanvas");
            _root = _canvas.GetComponent<RectTransform>();

            _titleText = CreateText("Title", _root, "Run - Acto 1 (placeholder)", 28, TextAnchor.UpperCenter);
            RectTransform titleRect = _titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.88f);
            titleRect.anchorMax = new Vector2(0.9f, 0.98f);

            _statusText = CreateText("Status", _root, "Gold: 0 | Completados: 0/3", 20, TextAnchor.UpperCenter);
            RectTransform statusRect = _statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.1f, 0.8f);
            statusRect.anchorMax = new Vector2(0.9f, 0.88f);

            CreateNodeButtons();
            _continueButton = CreateButton("ContinueButton", _root, "Continuar");
            _continueButton.onClick.AddListener(OnContinue);
            _continueButton.gameObject.SetActive(false);
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemGO = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystemGO);
        }

        private void CreateNodeButtons()
        {
            float startY = 0.65f;
            float step = 0.12f;
            for (int i = 0; i < RunState.NodeCount; i++)
            {
                int index = i;
                Button button = CreateButton($"Node_{i + 1}", _root, $"Nodo {i + 1}");
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.35f, startY - step * i);
                rect.anchorMax = new Vector2(0.65f, startY - step * i + 0.08f);
                button.onClick.AddListener(() => EnterNode(index));
                _nodeButtons.Add(button);
            }
        }

        private void ShowMap()
        {
            _continueButton.gameObject.SetActive(false);
            _titleText.text = "Mapa Acto 1 (placeholder)";

            int completed = 0;
            for (int i = 0; i < _state.CompletedNodes.Count; i++)
            {
                if (_state.CompletedNodes[i])
                {
                    completed++;
                }
            }

            _statusText.text = $"Gold: {_state.Gold} | Completados: {completed}/{RunState.NodeCount}";

            int nextAvailable = GetNextAvailableNodeIndex();
            for (int i = 0; i < _nodeButtons.Count; i++)
            {
                Button button = _nodeButtons[i];
                bool completedNode = _state.CompletedNodes[i];
                bool available = i == nextAvailable;

                button.gameObject.SetActive(true);
                button.interactable = available;

                Text label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = completedNode ? $"Nodo {i + 1} (Completado)" : $"Nodo {i + 1}";
                }
            }
        }

        private void EnterNode(int index)
        {
            if (index < 0 || index >= _state.CompletedNodes.Count)
            {
                return;
            }

            if (_state.CompletedNodes[index])
            {
                return;
            }

            if (index != GetNextAvailableNodeIndex())
            {
                return;
            }

            _state.CurrentNodeIndex = index;
            _titleText.text = $"Nodo {index + 1}";
            _statusText.text = "Contenido placeholder. Resolver y continuar.";

            foreach (Button button in _nodeButtons)
            {
                button.gameObject.SetActive(false);
            }

            _continueButton.gameObject.SetActive(true);
        }

        private void OnContinue()
        {
            if (_state.CurrentNodeIndex < 0 || _state.CurrentNodeIndex >= _state.CompletedNodes.Count)
            {
                ShowMap();
                return;
            }

            _state.CompletedNodes[_state.CurrentNodeIndex] = true;
            _state.CurrentNodeIndex = -1;
            _state.Gold += 10;
            ShowMap();
        }

        private int GetNextAvailableNodeIndex()
        {
            for (int i = 0; i < _state.CompletedNodes.Count; i++)
            {
                if (!_state.CompletedNodes[i])
                {
                    return i;
                }
            }

            return -1;
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
    }
}

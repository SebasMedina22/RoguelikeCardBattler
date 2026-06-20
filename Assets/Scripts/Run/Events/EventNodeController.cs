using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Relics;

namespace RoguelikeCardBattler.Run.Events
{
    /// <summary>
    /// Controlador del nodo Event (4b-1, eventos NO multidimensionales).
    /// RunFlowController lo crea como hijo en runtime (no requiere setup manual).
    /// Construye un panel sobre el canvas del run: título + texto narrativo +
    /// botones de decisión; al elegir aplica las consecuencias y muestra un panel
    /// de resultado con "Continuar". Espejo estructural de
    /// <c>CampfireNodeController</c> / <c>ShopNodeController</c> (panel runtime,
    /// fallback de color sin arte, ClearSpawnedButtons).
    ///
    /// La lógica de consecuencias vive en <see cref="EventConsequence.Apply"/>
    /// (static puro, testeable sin UI); este controller sólo orquesta la UI.
    /// </summary>
    public class EventNodeController : MonoBehaviour
    {
        // Color de fallback cuando no hay sprite de fondo: tono morado oscuro que
        // diferencia el evento de la Hoguera (azul) y la Tienda (marrón).
        private static readonly Color FallbackBackground = new Color(0.14f, 0.10f, 0.18f, 1f); // #241A2E

        private Canvas _canvas;
        private RunState _state;
        private RelicHookDispatcher _dispatcher;
        private EventPoolConfig _pool;
        private Action<int> _onComplete;
        private Font _uiFont;

        private RectTransform _root;
        private Image _background;
        private Text _titleText;
        private Text _bodyText;
        private RectTransform _choicesPanel;
        private RectTransform _resultPanel;
        private Text _resultText;
        private readonly List<GameObject> _spawnedButtons = new List<GameObject>();

        private int _activeNodeId = -1;
        private EventDefinition _activeDef;
        private static Sprite _whiteSprite;

        public void Initialize(
            Canvas canvas,
            RunState state,
            RelicHookDispatcher dispatcher,
            EventPoolConfig pool,
            Action<int> onComplete)
        {
            _canvas = canvas;
            _state = state;
            _dispatcher = dispatcher;
            _pool = pool;
            _onComplete = onComplete;
            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildPanel();
            _root.gameObject.SetActive(false);
        }

        /// <summary>
        /// Abre el panel para el evento <paramref name="definition"/> del nodo
        /// <paramref name="nodeId"/>. La definición viaja desde
        /// <c>MapNode.AssignedEvent</c> (fijada por seed en la generación del mapa);
        /// el controller no tiene referencia al mapa, así que RunFlowController la
        /// pasa. Si la definición es nula, el caller hace fallback al panel genérico.
        /// </summary>
        public void Show(int nodeId, EventDefinition definition)
        {
            if (_root == null || definition == null) return;

            _activeNodeId = nodeId;
            _activeDef = definition;

            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _resultPanel.gameObject.SetActive(false);
            _choicesPanel.gameObject.SetActive(true);

            ApplyBackground(definition);
            _titleText.text = string.IsNullOrEmpty(definition.Title) ? "Evento" : definition.Title;
            _bodyText.text = definition.Body ?? string.Empty;

            BuildChoices();
        }

        /// <summary>
        /// Resuelve el fondo del panel para el evento: sprite del propio
        /// EventDefinition → sprite del pool (fallback compartido) → color de fallback.
        /// </summary>
        private void ApplyBackground(EventDefinition definition)
        {
            if (_background == null) return;

            Sprite sprite = definition != null ? definition.BackgroundSprite : null;
            if (sprite == null && _pool != null) sprite = _pool.BackgroundSprite;

            if (sprite != null)
            {
                _background.sprite = sprite;
                _background.color = Color.white;
            }
            else
            {
                _background.sprite = GetWhiteSprite();
                _background.color = FallbackBackground;
            }
        }

        /// <summary>
        /// Una decisión está disponible si el jugador alcanza su requisito de oro.
        /// Static y sin UI para que los tests lo validen sin instanciar el panel
        /// (la UI sólo lo consume para deshabilitar el botón).
        /// </summary>
        public static bool IsChoiceAvailable(RunState state, EventChoice choice)
        {
            if (state == null || choice == null) return false;
            return state.Gold >= choice.MinGoldRequired;
        }

        // ──────────────────────────────────────────────
        // Choices flow
        // ──────────────────────────────────────────────

        private void BuildChoices()
        {
            ClearSpawnedButtons();

            IReadOnlyList<EventChoice> choices = _activeDef.Choices;
            int count = choices != null ? choices.Count : 0;
            if (count == 0)
            {
                // Evento sin decisiones autoradas: ofrecer salida para no bloquear.
                Button leave = CreateButtonRaw(_choicesPanel, "Continue", "Continuar", 0.3f, 0.04f, 0.7f, 0.14f, true);
                leave.onClick.AddListener(CompleteEvent);
                _spawnedButtons.Add(leave.gameObject);
                return;
            }

            // Reparto vertical adaptativo en la franja inferior del panel (el texto
            // narrativo vive arriba). Mismo enfoque que ShopNodeController.DrawStock.
            const float top = 0.62f;
            const float bottom = 0.04f;
            const float gap = 0.02f;
            float region = top - bottom;
            float slot = region / count;
            float buttonHeight = Mathf.Max(0.06f, slot - gap);

            for (int i = 0; i < count; i++)
            {
                EventChoice choice = choices[i];
                bool affordable = IsChoiceAvailable(_state, choice);
                float yMax = top - i * slot;
                float yMin = yMax - buttonHeight;

                string label = BuildChoiceLabel(choice, affordable);
                Button btn = CreateButtonRaw(_choicesPanel, $"Choice_{i}", label, 0.08f, yMin, 0.92f, yMax, affordable);
                if (affordable)
                {
                    EventChoice captured = choice;
                    btn.onClick.AddListener(() => OnChoiceSelected(captured));
                }
                _spawnedButtons.Add(btn.gameObject);
            }
        }

        private void OnChoiceSelected(EventChoice choice)
        {
            if (choice == null) return;

            // Aplicar todas las consecuencias en orden (los GiveRelic respetan el
            // AcquisitionOrder por orden de la lista).
            if (choice.Consequences != null)
            {
                foreach (EventConsequence consequence in choice.Consequences)
                {
                    EventConsequence.Apply(_state, _dispatcher, consequence);
                }
            }

            ShowResult(choice.ResultText);
        }

        private void ShowResult(string resultText)
        {
            _choicesPanel.gameObject.SetActive(false);
            _resultPanel.gameObject.SetActive(true);
            ClearSpawnedButtons();

            _resultText.text = string.IsNullOrEmpty(resultText) ? "..." : resultText;

            Button continueBtn = CreateButtonRaw(_resultPanel, "ContinueButton", "Continuar", 0.3f, 0.05f, 0.7f, 0.16f, true);
            continueBtn.onClick.AddListener(CompleteEvent);
            _spawnedButtons.Add(continueBtn.gameObject);
        }

        private void CompleteEvent()
        {
            _root.gameObject.SetActive(false);
            int nodeId = _activeNodeId;
            _activeNodeId = -1;
            _activeDef = null;
            _onComplete?.Invoke(nodeId);
        }

        // ──────────────────────────────────────────────
        // Label building (reusa CardDisplay para tokens de carta)
        // ──────────────────────────────────────────────

        private string BuildChoiceLabel(EventChoice choice, bool affordable)
        {
            string title = string.IsNullOrEmpty(choice.Label) ? "Decisión" : choice.Label;
            string summary = BuildConsequenceSummary(choice.Consequences);

            if (!affordable && choice.MinGoldRequired > 0)
            {
                return $"{title} (necesitas {choice.MinGoldRequired} oro)";
            }
            return string.IsNullOrEmpty(summary) ? title : $"{title}\n<{summary}>";
        }

        /// <summary>
        /// Resumen legible de las consecuencias para mostrar bajo la etiqueta de la
        /// decisión. Reusa <c>CardDisplay.CardToken</c> para los tokens de carta
        /// (tipo coloreado) en vez de duplicar el helper.
        /// </summary>
        private static string BuildConsequenceSummary(IReadOnlyList<EventConsequence> consequences)
        {
            if (consequences == null || consequences.Count == 0) return string.Empty;

            List<string> parts = new List<string>();
            foreach (EventConsequence c in consequences)
            {
                if (c == null) continue;
                switch (c.Type)
                {
                    case ConsequenceType.GiveCard:
                        parts.Add($"+{CardDisplay.CardToken(c.Card)}");
                        break;
                    case ConsequenceType.RemoveCard:
                        parts.Add($"-{CardDisplay.CardToken(c.Card)}");
                        break;
                    case ConsequenceType.GiveGold:
                        parts.Add($"+{Mathf.Abs(c.Amount)} oro");
                        break;
                    case ConsequenceType.LoseGold:
                        parts.Add($"-{Mathf.Abs(c.Amount)} oro");
                        break;
                    case ConsequenceType.ModifyHP:
                        parts.Add(c.Amount >= 0 ? $"+{c.Amount} HP" : $"{c.Amount} HP");
                        break;
                    case ConsequenceType.GiveRelic:
                        string relicName = c.Relic == null
                            ? "Retazo"
                            : (string.IsNullOrEmpty(c.Relic.DisplayName) ? c.Relic.name : c.Relic.DisplayName);
                        parts.Add($"+Retazo: {relicName}");
                        break;
                }
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < parts.Count; i++)
            {
                if (i > 0) sb.Append("  ");
                sb.Append(parts[i]);
            }
            return sb.ToString();
        }

        // ──────────────────────────────────────────────
        // Build (UI)
        // ──────────────────────────────────────────────

        private void BuildPanel()
        {
            GameObject rootGo = new GameObject("EventPanel", typeof(RectTransform), typeof(Image));
            rootGo.transform.SetParent(_canvas.transform, false);
            _root = rootGo.GetComponent<RectTransform>();
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;

            Image rootImage = rootGo.GetComponent<Image>();
            rootImage.sprite = GetWhiteSprite();
            rootImage.color = new Color(0f, 0f, 0f, 1f); // tapa el canvas detrás
            rootImage.raycastTarget = true;

            // Capa de fondo: sprite del pool si existe, si no color de fallback.
            GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(_root, false);
            RectTransform bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            _background = bgGo.GetComponent<Image>();
            // Fondo inicial = color de fallback. El sprite real se resuelve por-evento
            // en Show (cada EventDefinition puede traer el suyo).
            _background.sprite = GetWhiteSprite();
            _background.color = FallbackBackground;
            _background.raycastTarget = false;

            // Título
            _titleText = CreateText("Title", _root, "Evento", 38, TextAnchor.UpperCenter);
            RectTransform titleRect = _titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.85f);
            titleRect.anchorMax = new Vector2(0.9f, 0.96f);

            // Texto narrativo
            _bodyText = CreateText("Body", _root, "", 22, TextAnchor.UpperCenter);
            _bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            RectTransform bodyRect = _bodyText.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.12f, 0.66f);
            bodyRect.anchorMax = new Vector2(0.88f, 0.84f);

            // Panel de decisiones
            _choicesPanel = CreateSubPanel("ChoicesPanel", _root);
            _choicesPanel.anchorMin = new Vector2(0.2f, 0.05f);
            _choicesPanel.anchorMax = new Vector2(0.8f, 0.64f);

            // Panel de resultado
            _resultPanel = CreateSubPanel("ResultPanel", _root);
            _resultPanel.anchorMin = new Vector2(0.15f, 0.05f);
            _resultPanel.anchorMax = new Vector2(0.85f, 0.64f);
            _resultPanel.gameObject.SetActive(false);

            _resultText = CreateText("ResultText", _resultPanel, "", 24, TextAnchor.UpperCenter);
            _resultText.horizontalOverflow = HorizontalWrapMode.Wrap;
            RectTransform resultRect = _resultText.GetComponent<RectTransform>();
            resultRect.anchorMin = new Vector2(0.05f, 0.3f);
            resultRect.anchorMax = new Vector2(0.95f, 0.95f);
        }

        private RectTransform CreateSubPanel(string name, RectTransform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private Button CreateButtonRaw(RectTransform parent, string name, string label, float xMin, float yMin, float xMax, float yMax, bool interactable)
        {
            GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(parent, false);
            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonGO.GetComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.color = interactable
                ? new Color(0.16f, 0.12f, 0.20f, 0.92f)
                : new Color(0.1f, 0.1f, 0.1f, 0.6f);

            GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(buttonGO.transform, false);
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8, 4);
            textRect.offsetMax = new Vector2(-8, -4);

            Text text = textGO.GetComponent<Text>();
            text.font = _uiFont;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = interactable ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
            text.text = label;

            Button button = buttonGO.GetComponent<Button>();
            button.interactable = interactable;
            return button;
        }

        private Text CreateText(string name, RectTransform parent, string content, int fontSize, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = go.GetComponent<Text>();
            text.font = _uiFont;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(1f, 0.95f, 0.85f, 1f);
            text.text = content;
            return text;
        }

        private void ClearSpawnedButtons()
        {
            foreach (GameObject go in _spawnedButtons)
            {
                if (go != null) Destroy(go);
            }
            _spawnedButtons.Clear();
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            _whiteSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }
    }
}

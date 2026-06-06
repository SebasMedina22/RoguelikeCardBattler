using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using RoguelikeCardBattler.Core;
using RoguelikeCardBattler.Core.Audio;
using RoguelikeCardBattler.Core.UI;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;

namespace RoguelikeCardBattler.Run.NewRun
{
    /// <summary>
    /// Controlador scene-owned de NewRunScene (Sub-PR 3E): la pantalla de arranque
    /// de run entre MainMenu y RunScene. Máquina de 3 pasos:
    ///   1. Tipos    — el jugador elige un tipo elemental por mundo (deben ser distintos).
    ///   2. Draft    — elige 1 cara filtrada por el tipo del Mundo A + 1 por el del B;
    ///                 se compone una carta dual en runtime ("Componer", decisión #1).
    ///   3. Confirmar— resumen + "Comenzar run" → escribe RunState y carga RunScene.
    ///
    /// Construye toda la UI en runtime (sin setup manual de inspector), espejo
    /// estructural de <c>MainMenuController</c>. La lógica del draft vive en helpers
    /// static puros (<see cref="StarterDraft"/>), testeables sin UI.
    ///
    /// Regla de no-estado-sucio: RunState SÓLO se muta en
    /// <see cref="ApplySelection"/>, llamada exclusivamente al confirmar. Volver al
    /// menú en cualquier paso deja RunState intacto.
    /// </summary>
    public class NewRunController : MonoBehaviour
    {
        private const string SceneRun = "RunScene";
        private const string SceneMenu = "MainMenuScene";

        private enum Step { Types, Draft, Confirm }

        // El config se asigna en la escena (mismo patrón que ShopConfig →
        // RunFlowController). Lo crea el menú editor "Roguelike > Setup New Run Config".
        [SerializeField] private NewRunConfig newRunConfig;
        [SerializeField] private Font uiFont;

        private Canvas _canvas;
        private RectTransform _root;
        private Text _messageText;

        private RectTransform _typesPanel;
        private RectTransform _draftPanel;
        private RectTransform _confirmPanel;

        // Selección en curso (NO toca RunState hasta confirmar).
        private ElementType _selectedTypeA = ElementType.None;
        private ElementType _selectedTypeB = ElementType.None;
        private CardDefinition _chosenFaceA;
        private CardDefinition _chosenFaceB;
        private DualCardDefinition _composedDual;

        private StarterDraft.DraftOptions _draftOptions;
        private bool _draftBuilt;
        private int _draftSeed;

        private Button _continueButton; // paso 1 → 2
        private Button _toConfirmButton; // paso 2 → 3
        private Button _confirmButton;   // paso 3 → run

        // Colores estética cartón/crayón.
        private static readonly Color SelectedTint = new Color(0.95f, 0.78f, 0.35f, 1f);
        private static readonly Color IdleTint = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        private static readonly Color DisabledTint = new Color(0.1f, 0.1f, 0.1f, 0.6f);

        private static Sprite _whiteSprite;

        private void Awake()
        {
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            // Seed estable por sesión de pantalla: el draft no se rebaraja al ir y
            // volver entre pasos, pero cada entrada a NewRunScene da un draft fresco.
            _draftSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

            EnsureEventSystem();
            BuildUI();
            ShowStep(Step.Types);
        }

        // ──────────────────────────────────────────────
        // Navegación de pasos
        // ──────────────────────────────────────────────

        private void ShowStep(Step step)
        {
            _typesPanel.gameObject.SetActive(step == Step.Types);
            _draftPanel.gameObject.SetActive(step == Step.Draft);
            _confirmPanel.gameObject.SetActive(step == Step.Confirm);

            switch (step)
            {
                case Step.Types:
                    PopulateTypesPanel();
                    break;
                case Step.Draft:
                    PopulateDraftPanel();
                    break;
                case Step.Confirm:
                    PopulateConfirmPanel();
                    break;
            }
        }

        private void OnContinueToDraft()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.ClickSFX);
            ShowStep(Step.Draft);
        }

        private void OnBackToTypes()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.ClickSFX);
            ShowStep(Step.Types);
        }

        private void OnToConfirm()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.ClickSFX);
            ShowStep(Step.Confirm);
        }

        private void OnBackToDraft()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.ClickSFX);
            ShowStep(Step.Draft);
        }

        private void OnBackToMenu()
        {
            // Cancelar: NO se tocó RunState, así que volver al menú deja todo limpio.
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.ClickSFX);
            SceneTransitionManager.LoadScene(SceneMenu);
        }

        private void OnConfirmRun()
        {
            if (_composedDual == null) return;

            // Sonido de peso al confirmar (clip dedicado o click como fallback).
            AudioClip confirm = newRunConfig != null ? newRunConfig.ConfirmClip : null;
            AudioManager.Instance?.PlaySFX(confirm != null ? confirm : AudioManager.Instance.ClickSFX);

            ApplySelection();
            SceneTransitionManager.LoadScene(SceneRun);
        }

        // ──────────────────────────────────────────────
        // Mutación de estado (única vía que toca RunState)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Escribe la selección en RunState. ÚNICA vía que muta el estado, llamada
        /// SÓLO al confirmar: setea los 2 tipos por mundo y transporta la carta
        /// drafteada vía <c>PendingStarterCard</c> (la consume InitializeDeck como
        /// la "10ª carta" del mazo inicial). Garantiza "cancelar no deja estado sucio".
        /// </summary>
        public void ApplySelection()
        {
            RunState state = RunSession.GetOrCreate().State;
            StarterDraft.ApplySelectionToState(state, _selectedTypeA, _selectedTypeB, _composedDual);
        }

        // ──────────────────────────────────────────────
        // Paso 1 — Tipos
        // ──────────────────────────────────────────────

        private void PopulateTypesPanel()
        {
            ClearChildren(_typesPanel);

            CreateText("TypesTitle", _typesPanel, "Elige tu identidad: un tipo por mundo", 28, TextAnchor.UpperCenter,
                new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f));

            // Columna Mundo A (Medieval, izquierda) y Mundo B (Cyberpunk, derecha).
            BuildTypeColumn("ColumnA", "Mundo A — Medieval", 0.06f, 0.50f, isWorldA: true);
            BuildTypeColumn("ColumnB", "Mundo B — Cyberpunk", 0.50f, 0.94f, isWorldA: false);

            // Continuar (habilitado sólo con dos tipos distintos elegidos).
            _continueButton = CreateButtonAnchored(_typesPanel, "ContinueButton", "Continuar",
                0.6f, 0.02f, 0.94f, 0.10f, ContinueEnabled());
            if (ContinueEnabled())
            {
                _continueButton.onClick.AddListener(OnContinueToDraft);
            }

            // Volver al menú (siempre disponible).
            Button toMenu = CreateButtonAnchored(_typesPanel, "BackToMenu", "Volver al menú",
                0.06f, 0.02f, 0.40f, 0.10f, true);
            toMenu.onClick.AddListener(OnBackToMenu);
        }

        private void BuildTypeColumn(string name, string header, float xMin, float xMax, bool isWorldA)
        {
            CreateText(name + "Header", _typesPanel, header, 22, TextAnchor.UpperCenter,
                new Vector2(xMin, 0.78f), new Vector2(xMax, 0.86f));

            var types = newRunConfig != null ? newRunConfig.SelectableTypes : null;
            int count = types != null ? types.Count : 0;
            float top = 0.76f;
            float bottom = 0.14f;
            float region = top - bottom;
            float slot = count > 0 ? region / count : region;

            for (int i = 0; i < count; i++)
            {
                ElementType type = types[i];

                // En la columna B no se puede elegir el tipo ya tomado en A (y viceversa).
                ElementType otherSelected = isWorldA ? _selectedTypeB : _selectedTypeA;
                bool disabledByOther = type == otherSelected && otherSelected != ElementType.None;

                ElementType thisSelected = isWorldA ? _selectedTypeA : _selectedTypeB;
                bool isSelected = type == thisSelected;

                float yMax = top - i * slot;
                float yMin = yMax - (slot - 0.015f);

                Button btn = CreateButtonAnchored(_typesPanel, $"{name}_{type}", type.ToString(),
                    xMin, yMin, xMax, yMax, !disabledByOther);

                // Tinte por tipo (C8): cada tipo elemental ES un color → el botón lo
                // refleja. El dorado se reserva para "elegido" (afordancia ya usada en
                // el draft); deshabilitado = color del tipo atenuado pero aún legible.
                if (isSelected)
                {
                    btn.image.color = SelectedTint;
                }
                else if (disabledByOther)
                {
                    btn.image.color = ElementTypeColors.Dim(ElementTypeColors.For(type), 0.4f);
                }
                else
                {
                    btn.image.color = ElementTypeColors.For(type);
                }

                // El texto se adapta para contrastar sobre el fondo tintado
                // (negro sobre amarillo/blanco, blanco sobre azul/rojo/etc.).
                Text btnLabel = btn.GetComponentInChildren<Text>();
                if (btnLabel != null)
                {
                    btnLabel.color = ElementTypeColors.ReadableTextOn(btn.image.color);
                }

                if (!disabledByOther)
                {
                    ElementType captured = type;
                    bool worldA = isWorldA;
                    btn.onClick.AddListener(() => OnTypeChosen(worldA, captured));
                }
            }
        }

        private void OnTypeChosen(bool isWorldA, ElementType type)
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.ClickSFX);
            if (isWorldA) _selectedTypeA = type;
            else _selectedTypeB = type;

            // Cambiar un tipo invalida el draft compuesto previo: hay que rebarajar
            // las columnas con los tipos nuevos.
            _draftBuilt = false;
            _chosenFaceA = null;
            _chosenFaceB = null;
            _composedDual = null;

            PopulateTypesPanel(); // refresca habilitaciones / highlight / Continuar
        }

        private bool ContinueEnabled() => StarterDraft.TypesValid(_selectedTypeA, _selectedTypeB);

        // ──────────────────────────────────────────────
        // Paso 2 — Draft (Componer)
        // ──────────────────────────────────────────────

        private void PopulateDraftPanel()
        {
            ClearChildren(_draftPanel);

            if (!_draftBuilt)
            {
                _draftOptions = StarterDraft.BuildDraftOptions(newRunConfig, _selectedTypeA, _selectedTypeB, _draftSeed);
                _draftBuilt = true;
            }

            CreateText("DraftTitle", _draftPanel, "Draftea tu carta especial", 28, TextAnchor.UpperCenter,
                new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.99f));
            CreateText("DraftWeight", _draftPanel, "Esta carta te acompañará todo el run", 18, TextAnchor.UpperCenter,
                new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.90f));

            BuildDraftColumn("DraftA", $"Mundo A — {_selectedTypeA}", _draftOptions.WorldAOptions, 0.06f, 0.49f, isWorldA: true);
            BuildDraftColumn("DraftB", $"Mundo B — {_selectedTypeB}", _draftOptions.WorldBOptions, 0.51f, 0.94f, isWorldA: false);

            RefreshComposeState();

            _toConfirmButton = CreateButtonAnchored(_draftPanel, "ToConfirm", "Confirmar carta",
                0.6f, 0.02f, 0.94f, 0.10f, _composedDual != null);
            if (_composedDual != null)
            {
                _toConfirmButton.onClick.AddListener(OnToConfirm);
            }

            Button back = CreateButtonAnchored(_draftPanel, "BackToTypes", "Volver", 0.06f, 0.02f, 0.27f, 0.10f, true);
            back.onClick.AddListener(OnBackToTypes);
            Button toMenu = CreateButtonAnchored(_draftPanel, "BackToMenu", "Volver al menú", 0.28f, 0.02f, 0.55f, 0.10f, true);
            toMenu.onClick.AddListener(OnBackToMenu);
        }

        private void BuildDraftColumn(string name, string header, System.Collections.Generic.List<CardDefinition> options, float xMin, float xMax, bool isWorldA)
        {
            CreateText(name + "Header", _draftPanel, header, 20, TextAnchor.UpperCenter,
                new Vector2(xMin, 0.76f), new Vector2(xMax, 0.83f));

            int count = options != null ? options.Count : 0;
            float top = 0.74f;
            float bottom = 0.14f;
            float region = top - bottom;
            float slot = count > 0 ? region / Mathf.Max(1, count) : region;

            // Las cartas entran con fade + slide desde su lado de mundo
            // (A desde la izquierda, B desde la derecha) — feel pedido en el spec.
            float slideFrom = isWorldA ? -120f : 120f;

            for (int i = 0; i < count; i++)
            {
                CardDefinition face = options[i];
                bool isChosen = isWorldA ? (face == _chosenFaceA) : (face == _chosenFaceB);

                float yMax = top - i * slot;
                float yMin = yMax - (slot - 0.02f);

                string label = BuildFaceLabel(face);
                Button btn = CreateButtonAnchored(_draftPanel, $"{name}_{i}", label, xMin, yMin, xMax, yMax, true);
                if (isChosen) btn.image.color = SelectedTint;

                // Arte de la cara (C7 / cierra N2): si la cara tiene ilustración, se
                // renderiza en la región superior del botón y el texto baja a la franja
                // inferior. Sin arte (caso actual hasta que llegue el PNG), queda el
                // texto como hoy — el gancho está cableado, el efecto visual es nulo.
                if (face != null && face.Art != null)
                {
                    AddFaceArt(btn, face.Art);
                }

                CardDefinition captured = face;
                bool worldA = isWorldA;
                btn.onClick.AddListener(() => OnFaceChosen(worldA, captured));

                AnimateCardEntry(btn.GetComponent<RectTransform>(), slideFrom, i * 0.06f);
            }
        }

        private void OnFaceChosen(bool isWorldA, CardDefinition face)
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.ClickSFX);
            if (isWorldA) _chosenFaceA = face;
            else _chosenFaceB = face;

            PopulateDraftPanel(); // refresca highlight + estado del botón Confirmar
        }

        private void RefreshComposeState()
        {
            // Compone el dual sólo cuando hay una cara elegida por cada mundo.
            _composedDual = (_chosenFaceA != null && _chosenFaceB != null)
                ? StarterDraft.ComposeDualCard(_chosenFaceA, _chosenFaceB)
                : null;
        }

        // ──────────────────────────────────────────────
        // Paso 3 — Confirmar
        // ──────────────────────────────────────────────

        private void PopulateConfirmPanel()
        {
            ClearChildren(_confirmPanel);

            CreateText("ConfirmTitle", _confirmPanel, "Tu build", 30, TextAnchor.UpperCenter,
                new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f));

            string summary =
                $"Mundo A: {_selectedTypeA}\n" +
                $"Mundo B: {_selectedTypeB}\n\n" +
                $"Carta especial:\n{BuildComposedSummary()}";
            CreateText("ConfirmSummary", _confirmPanel, summary, 22, TextAnchor.UpperCenter,
                new Vector2(0.1f, 0.30f), new Vector2(0.9f, 0.82f));

            _confirmButton = CreateButtonAnchored(_confirmPanel, "ConfirmRun", "Comenzar run",
                0.6f, 0.04f, 0.92f, 0.14f, _composedDual != null);
            if (_composedDual != null)
            {
                _confirmButton.onClick.AddListener(OnConfirmRun);
            }

            Button back = CreateButtonAnchored(_confirmPanel, "BackToDraft", "Volver", 0.08f, 0.04f, 0.40f, 0.14f, true);
            back.onClick.AddListener(OnBackToDraft);
        }

        private string BuildComposedSummary()
        {
            if (_composedDual == null) return "—";
            string a = _chosenFaceA != null ? $"{_chosenFaceA.CardName} ({_selectedTypeA})" : "?";
            string b = _chosenFaceB != null ? $"{_chosenFaceB.CardName} ({_selectedTypeB})" : "?";
            return $"A: {a}\nB: {b}";
        }

        private static string BuildFaceLabel(CardDefinition face)
        {
            if (face == null) return "Carta";
            string desc = string.IsNullOrEmpty(face.Description) ? string.Empty : $"\n<{face.Description}>";
            return $"{face.CardName}{desc}";
        }

        // ──────────────────────────────────────────────
        // Construcción de UI
        // ──────────────────────────────────────────────

        private void BuildUI()
        {
            _canvas = CreateCanvas("NewRunCanvas");
            RectTransform root = _canvas.GetComponent<RectTransform>();

            // Fondo atmosférico (placeholder de color hasta tener arte de la escena).
            RectTransform background = CreatePanel("Background", root, new Color(0.10f, 0.09f, 0.13f, 1f));
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;
            _root = background;

            Text title = CreateText("Title", _root, "Nueva Run", 38, TextAnchor.UpperCenter,
                new Vector2(0.1f, 0.93f), new Vector2(0.9f, 0.99f));
            title.color = new Color(1f, 0.95f, 0.85f, 1f);

            _messageText = CreateText("Message", _root, string.Empty, 18, TextAnchor.LowerCenter,
                new Vector2(0.1f, 0.0f), new Vector2(0.9f, 0.04f));

            _typesPanel = CreateStepPanel("TypesPanel");
            _draftPanel = CreateStepPanel("DraftPanel");
            _confirmPanel = CreateStepPanel("ConfirmPanel");

            if (newRunConfig == null)
            {
                // Sin config no hay pool ni tipos: lo decimos en vez de fallar silenciosamente.
                Debug.LogError("[NewRunController] newRunConfig sin asignar. Corre " +
                    "'Roguelike > Setup New Run Config' y asigna el asset en la escena.");
                SetMessage("Falta NewRunConfig — ver consola.");
            }
        }

        private RectTransform CreateStepPanel(string name)
        {
            RectTransform panel = CreatePanel(name, _root, new Color(0f, 0f, 0f, 0f));
            panel.anchorMin = new Vector2(0.05f, 0.05f);
            panel.anchorMax = new Vector2(0.95f, 0.92f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            return panel;
        }

        /// <summary>
        /// Fade + slide de entrada de una carta del draft desde su lado de mundo.
        /// Fire-and-forget: cosmético, no bloquea la lógica de selección.
        /// </summary>
        private void AnimateCardEntry(RectTransform target, float slideFromX, float delay)
        {
            CanvasGroup group = target.gameObject.GetComponent<CanvasGroup>();
            if (group == null) group = target.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            Sequence seq = DOTween.Sequence().SetTarget(target).SetUpdate(true);
            if (delay > 0f) seq.AppendInterval(delay);
            seq.Append(UIAnimationHelper.FadeIn(group, 0.25f));
            seq.Join(UIAnimationHelper.SlideIn(target, new Vector2(slideFromX, 0f), 0.25f));
        }

        private void SetMessage(string message)
        {
            if (_messageText != null) _messageText.text = message;
        }

        private void EnsureEventSystem()
        {
            EventSystem existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing != null) return;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            Type inputSystemType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemType != null)
            {
                eventSystemObject.AddComponent(inputSystemType);
            }
            else
            {
                eventSystemObject.AddComponent<StandaloneInputModule>();
            }
        }

        private Canvas CreateCanvas(string name)
        {
            GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private RectTransform CreatePanel(string name, RectTransform parent, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            Image image = panel.GetComponent<Image>();
            image.color = color;
            return panel.GetComponent<RectTransform>();
        }

        private Text CreateText(string name, RectTransform parent, string content, int fontSize, TextAnchor anchor,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.text = content;
            text.color = Color.white;
            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return text;
        }

        private Button CreateButtonAnchored(RectTransform parent, string name, string label,
            float xMin, float yMin, float xMax, float yMax, bool interactable)
        {
            GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(parent, false);
            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonGO.GetComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.color = interactable ? IdleTint : DisabledTint;

            Text text = CreateText("Label", rect, label, 20, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one);
            text.color = interactable ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.offsetMin = new Vector2(8, 4);
            textRect.offsetMax = new Vector2(-8, -4);

            Button button = buttonGO.GetComponent<Button>();
            button.interactable = interactable;
            return button;
        }

        /// <summary>
        /// Renderiza el arte de una cara de draft (C7) sobre su botón: Image hijo en
        /// la región superior (preserveAspect, sin captar raycasts) y baja el texto a
        /// la franja inferior. Espeja el criterio de <c>CardHandView</c> en combate
        /// (se duplican ~6 líneas a propósito: viven en escenas/asmdef distintos, ver
        /// alternativa A2 del spec).
        /// </summary>
        private void AddFaceArt(Button btn, Sprite art)
        {
            RectTransform btnRect = btn.GetComponent<RectTransform>();

            GameObject artGO = new GameObject("Art", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            artGO.transform.SetParent(btnRect, false);
            RectTransform artRect = artGO.GetComponent<RectTransform>();
            artRect.anchorMin = new Vector2(0.06f, 0.42f);
            artRect.anchorMax = new Vector2(0.94f, 0.96f);
            artRect.offsetMin = Vector2.zero;
            artRect.offsetMax = Vector2.zero;

            Image artImage = artGO.GetComponent<Image>();
            artImage.sprite = art;
            artImage.preserveAspect = true;
            artImage.raycastTarget = false;
            artImage.color = Color.white;

            // El texto baja a la franja inferior para no pisar la ilustración.
            Text label = btn.GetComponentInChildren<Text>();
            if (label != null)
            {
                RectTransform labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(1f, 0.40f);
                labelRect.offsetMin = new Vector2(8, 4);
                labelRect.offsetMax = new Vector2(-8, -4);
                label.alignment = TextAnchor.UpperCenter;
            }
        }

        private void ClearChildren(RectTransform panel)
        {
            for (int i = panel.childCount - 1; i >= 0; i--)
            {
                Destroy(panel.GetChild(i).gameObject);
            }
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            _whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }
    }
}

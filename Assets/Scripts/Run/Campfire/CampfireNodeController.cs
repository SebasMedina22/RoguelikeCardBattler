using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using RoguelikeCardBattler.Core.Audio;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Run.Campfire
{
    /// <summary>
    /// Controlador del nodo Hoguera. RunFlowController lo crea como hijo en
    /// runtime (no requiere setup manual). Construye un panel sobre el canvas
    /// del run con parallax 2D de 3 capas + opciones (Descansar / Mejorar carta
    /// / extras añadidas por Retazos vía OnCampfireOptionsBuilt).
    /// </summary>
    public class CampfireNodeController : MonoBehaviour
    {
        // Colores de fallback cuando los sprites del config son null. Permiten
        // probar la feature antes de tener arte: un fondo plano en cada capa.
        private static readonly Color FallbackSky = new Color(0.10f, 0.10f, 0.18f, 1f);   // #1A1A2E
        private static readonly Color FallbackMid = new Color(0.18f, 0.18f, 0.18f, 1f);   // #2D2D2D
        private static readonly Color FallbackFire = new Color(1f, 0.42f, 0f, 1f);        // #FF6B00

        private Canvas _canvas;
        private RunState _state;
        private RelicHookDispatcher _dispatcher;
        private CampfireConfig _config;
        private Action<int> _onComplete;
        private Font _uiFont;

        private RectTransform _root;
        private RectTransform _optionsPanel;
        private RectTransform _cardSelectPanel;
        private Image _skyLayer;
        private Image _midLayer;
        private Image _fireLayer;
        private Text _hpText;
        private readonly List<GameObject> _spawnedButtons = new List<GameObject>();

        private int _activeNodeId = -1;
        private static Sprite _whiteSprite;

        public void Initialize(
            Canvas canvas,
            RunState state,
            RelicHookDispatcher dispatcher,
            CampfireConfig config,
            Action<int> onComplete)
        {
            _canvas = canvas;
            _state = state;
            _dispatcher = dispatcher;
            _config = config;
            _onComplete = onComplete;
            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildPanel();
            _root.gameObject.SetActive(false);
        }

        public void Show(int nodeId)
        {
            if (_root == null) return;

            _activeNodeId = nodeId;

            if (AudioManager.Instance != null && AudioManager.Instance.CampfireAmbientClip != null)
            {
                AudioManager.Instance.PlayMusic(AudioManager.Instance.CampfireAmbientClip);
            }

            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _cardSelectPanel.gameObject.SetActive(false);
            _optionsPanel.gameObject.SetActive(true);

            RefreshHpText();
            BuildAndShowOptions();
            StartParallaxAnimations();
        }

        // ──────────────────────────────────────────────
        // Build
        // ──────────────────────────────────────────────

        private void BuildPanel()
        {
            GameObject rootGo = new GameObject("CampfirePanel", typeof(RectTransform), typeof(Image));
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

            // Sky: fondo full, stretch (puede deformarse: es atmósfera de fondo).
            // Mid: full screen, stretch — el PNG llena toda la pantalla aunque
            //      cambie el aspect ratio (el cielo + estrellas viven dentro del Mid).
            // Fire: bloque inferior-centrado, preserveAspect para no deformar la fogata.
            _skyLayer = CreateLayer("SkyLayer", _root, _config != null ? _config.SkySprite : null, FallbackSky,
                new Vector2(0f, 0f), new Vector2(1f, 1f), preserveAspect: false);
            _midLayer = CreateLayer("MidLayer", _root, _config != null ? _config.MidSprite : null, FallbackMid,
                new Vector2(-0.02f, -0.22f), new Vector2(1.03f, 0.78f), preserveAspect: false);
            _fireLayer = CreateLayer("FireLayer", _root, _config != null ? _config.FireSprite : null, FallbackFire,
                new Vector2(0.32f, -0.05f), new Vector2(0.68f, 0.28f), preserveAspect: true);

            // Título
            Text title = CreateText("Title", _root, "~ Hoguera ~", 40, TextAnchor.UpperCenter);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.85f);
            titleRect.anchorMax = new Vector2(0.9f, 0.95f);

            // HP text (debajo del título)
            _hpText = CreateText("HpText", _root, "HP: -- / --", 22, TextAnchor.UpperCenter);
            RectTransform hpRect = _hpText.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0.1f, 0.78f);
            hpRect.anchorMax = new Vector2(0.9f, 0.84f);

            // Panel de opciones
            _optionsPanel = CreateSubPanel("OptionsPanel", _root);
            _optionsPanel.anchorMin = new Vector2(0.25f, 0.15f);
            _optionsPanel.anchorMax = new Vector2(0.75f, 0.7f);

            // Panel selector de carta
            _cardSelectPanel = CreateSubPanel("CardSelectPanel", _root);
            _cardSelectPanel.anchorMin = new Vector2(0.15f, 0.1f);
            _cardSelectPanel.anchorMax = new Vector2(0.85f, 0.78f);
            _cardSelectPanel.gameObject.SetActive(false);
        }

        private Image CreateLayer(string name, RectTransform parent, Sprite sprite, Color fallback, Vector2 anchorMin, Vector2 anchorMax, bool preserveAspect)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = go.GetComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
                img.preserveAspect = preserveAspect;
            }
            else
            {
                img.sprite = GetWhiteSprite();
                img.color = fallback;
            }
            img.raycastTarget = false;
            return img;
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

        // ──────────────────────────────────────────────
        // Options flow
        // ──────────────────────────────────────────────

        private void BuildAndShowOptions()
        {
            ClearSpawnedButtons();

            int healPercent = _config != null ? _config.CampfireHealPercent : 30;
            bool canRest = _state.PlayerCurrentHP < _state.PlayerMaxHP;
            bool canUpgrade = HasUpgradableCard();

            List<CampfireOption> options = new List<CampfireOption>
            {
                new CampfireOption(
                    "Descansar",
                    canRest ? $"Recupera {healPercent}%" : "HP al máximo",
                    canRest,
                    OnSelectRest),
                new CampfireOption(
                    "Mejorar carta",
                    canUpgrade ? "Elige una carta del mazo" : "Sin cartas mejorables",
                    canUpgrade,
                    OnSelectUpgrade)
            };

            // Permite a los Retazos agregar opciones (ej: "Excavar para encontrar
            // Retazo común"). El dispatcher itera in-place; los handlers mutan
            // la lista. TurnManager null porque estamos fuera de combate.
            if (_dispatcher != null)
            {
                _dispatcher.Dispatch(
                    RelicHook.OnCampfireOptionsBuilt,
                    new CampfireOptionsBuiltHookData(_state, _dispatcher, options));
            }

            float yMax = 0.95f;
            float buttonHeight = 0.18f;
            float gap = 0.03f;
            for (int i = 0; i < options.Count; i++)
            {
                CampfireOption option = options[i];
                float top = yMax - i * (buttonHeight + gap);
                float bottom = top - buttonHeight;
                CreateOptionButton(_optionsPanel, option, bottom, top);
            }
        }

        private void OnSelectRest()
        {
            int percent = _config != null ? _config.CampfireHealPercent : 30;
            ApplyRest(_state, percent);
            CompleteCampfire();
        }

        private void OnSelectUpgrade()
        {
            ShowCardSelectPanel();
        }

        private void ShowCardSelectPanel()
        {
            _optionsPanel.gameObject.SetActive(false);
            _cardSelectPanel.gameObject.SetActive(true);
            ClearSpawnedButtons();

            Text header = CreateText("CardSelectHeader", _cardSelectPanel, "Elige una carta a mejorar", 26, TextAnchor.UpperCenter);
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.9f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            _spawnedButtons.Add(header.gameObject);

            List<CardDeckEntry> upgradable = new List<CardDeckEntry>();
            foreach (CardDeckEntry entry in _state.Deck)
            {
                if (entry != null && entry.CanUpgrade())
                {
                    upgradable.Add(entry);
                }
            }

            // Layout en grilla 2 columnas; suficiente para mazos de 10-25.
            int cols = 2;
            float topY = 0.88f;
            float bottomY = 0.1f;
            float rowHeight = 0.1f;
            float colWidth = 0.48f;

            for (int i = 0; i < upgradable.Count; i++)
            {
                CardDeckEntry entry = upgradable[i];
                int col = i % cols;
                int row = i / cols;
                float xMin = 0.01f + col * (colWidth + 0.02f);
                float xMax = xMin + colWidth;
                float yMaxButton = topY - row * (rowHeight + 0.01f);
                float yMinButton = yMaxButton - rowHeight;
                if (yMinButton < bottomY) break; // overflow: en mazos largos sería pageable, hoy clamp.

                string label = BuildCardSelectLabel(entry);
                Button btn = CreateButtonRaw(_cardSelectPanel, $"Card_{i}", label, xMin, yMinButton, xMax, yMaxButton, true);
                btn.onClick.AddListener(() =>
                {
                    entry.ApplyUpgrade();
                    CompleteCampfire();
                });
                _spawnedButtons.Add(btn.gameObject);
            }

            // Botón "Volver"
            Button back = CreateButtonRaw(_cardSelectPanel, "BackButton", "Volver", 0.35f, 0.0f, 0.65f, 0.07f, true);
            back.onClick.AddListener(() =>
            {
                _cardSelectPanel.gameObject.SetActive(false);
                _optionsPanel.gameObject.SetActive(true);
                BuildAndShowOptions();
            });
            _spawnedButtons.Add(back.gameObject);
        }

        private void CompleteCampfire()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopMusic();
            }
            StopParallaxAnimations();
            _root.gameObject.SetActive(false);
            int nodeId = _activeNodeId;
            _activeNodeId = -1;
            _onComplete?.Invoke(nodeId);
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────

        /// <summary>
        /// Etiqueta para el botón de selección de carta. Muestra el prefijo de tipo
        /// coloreado antes del nombre (reutiliza ElementTypeColors.TypePrefix). Para
        /// duales muestra AMBOS lados con su tipo y color propios; colapsa a un solo
        /// token solo si nombre Y tipo coinciden en los dos lados.
        /// </summary>
        private static string BuildCardSelectLabel(CardDeckEntry entry)
        {
            if (entry == null) return "Carta";
            if (entry.DualCard != null)
            {
                CardDefinition a = entry.DualCard.SideA;
                CardDefinition b = entry.DualCard.SideB;
                string tokenA = CardToken(a);
                string tokenB = CardToken(b);
                bool sameName = (a != null ? a.CardName : "?") == (b != null ? b.CardName : "?");
                bool sameType = SideType(a) == SideType(b);
                return (sameName && sameType) ? tokenA : $"{tokenA} / {tokenB}";
            }
            return entry.SingleCard != null ? CardToken(entry.SingleCard) : "Carta";
        }

        // Token rich-text "[Tipo] Nombre" para una CardDefinition (o "?" si es null).
        private static string CardToken(CardDefinition c)
        {
            if (c == null) return "?";
            string p = ElementTypeColors.TypePrefix(c.ElementType);
            return string.IsNullOrEmpty(p) ? c.CardName : $"{p} {c.CardName}";
        }

        // Null-safe CardDefinition → ElementType. Mismo nombre que el helper espejo
        // de ShopNodeController.SideType para que ambos BuildCardSelectLabel se lean
        // simétricos en paralelo.
        private static ElementType SideType(CardDefinition c) =>
            c != null ? c.ElementType : ElementType.None;

        private int ComputeHealAmount()
        {
            int percent = _config != null ? _config.CampfireHealPercent : 30;
            return ComputeHealAmount(_state.PlayerMaxHP, percent);
        }

        /// <summary>
        /// Helper estático para calcular el heal porcentual. Expuesto para que
        /// los tests EditMode puedan validarlo sin instanciar la UI.
        /// </summary>
        public static int ComputeHealAmount(int maxHp, int percent)
        {
            int clampedPct = Mathf.Clamp(percent, 0, 100);
            return Mathf.Max(0, Mathf.RoundToInt(maxHp * (clampedPct / 100f)));
        }

        /// <summary>
        /// Aplica el heal porcentual sobre RunState respetando el cap de
        /// PlayerMaxHP. Mismo comportamiento que el botón Descansar — usado por
        /// los tests EditMode.
        /// </summary>
        public static void ApplyRest(RunState state, int percent)
        {
            if (state == null) return;
            int heal = ComputeHealAmount(state.PlayerMaxHP, percent);
            state.PlayerCurrentHP = Mathf.Min(state.PlayerMaxHP, state.PlayerCurrentHP + heal);
        }

        private bool HasUpgradableCard()
        {
            foreach (CardDeckEntry entry in _state.Deck)
            {
                if (entry != null && entry.CanUpgrade()) return true;
            }
            return false;
        }

        private void RefreshHpText()
        {
            _hpText.text = $"HP: {_state.PlayerCurrentHP} / {_state.PlayerMaxHP}";
        }

        private void CreateOptionButton(RectTransform parent, CampfireOption option, float yMin, float yMax)
        {
            string label = option.IsAvailable
                ? $"{option.Title}\n<{option.Description}>"
                : $"{option.Title} ({option.Description})";
            Button btn = CreateButtonRaw(parent, option.Title, label, 0.05f, yMin, 0.95f, yMax, option.IsAvailable);
            if (option.IsAvailable && option.OnSelect != null)
            {
                btn.onClick.AddListener(() => option.OnSelect());
            }
            _spawnedButtons.Add(btn.gameObject);
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
                ? new Color(0.15f, 0.12f, 0.1f, 0.92f)
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

        // ──────────────────────────────────────────────
        // Parallax animation
        // ──────────────────────────────────────────────

        private void StartParallaxAnimations()
        {
            if (_midLayer != null)
            {
                RectTransform midRect = _midLayer.rectTransform;
                Vector2 baseAnchor = midRect.anchoredPosition;
                DOTween.Kill(midRect);
                DOTween.To(
                        () => midRect.anchoredPosition,
                        v => midRect.anchoredPosition = v,
                        baseAnchor + new Vector2(15f, 0f),
                        2f)
                    .SetTarget(midRect)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }

            if (_fireLayer != null)
            {
                Transform fireT = _fireLayer.transform;
                DOTween.Kill(fireT);
                fireT.localScale = Vector3.one;
                DOTween.To(
                        () => fireT.localScale,
                        v => fireT.localScale = v,
                        new Vector3(1.05f, 1.05f, 1f),
                        0.75f)
                    .SetTarget(fireT)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }
        }

        private void StopParallaxAnimations()
        {
            if (_midLayer != null) DOTween.Kill(_midLayer.rectTransform);
            if (_fireLayer != null)
            {
                DOTween.Kill(_fireLayer.transform);
                _fireLayer.transform.localScale = Vector3.one;
            }
        }

        private void OnDestroy()
        {
            StopParallaxAnimations();
        }
    }
}

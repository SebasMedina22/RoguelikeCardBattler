using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using RoguelikeCardBattler.Core.Audio;
using RoguelikeCardBattler.Core.UI;
using RoguelikeCardBattler.Gameplay.Cards;

namespace RoguelikeCardBattler.Gameplay.Combat
{
    /// <summary>
    /// Maneja la mano de cartas del jugador: creación de botones, sincronización
    /// con el estado de TurnManager, layout adaptivo, y click para jugar cartas.
    ///
    /// Extraído de CombatUIController como parte de la descomposición en componentes
    /// independientes (ver Docs/dev/COMBAT_ARCHITECTURE.md — Fase 2).
    ///
    /// Este componente es SOLO presentación — la lógica de jugar cartas se delega
    /// a TurnManager via TryPrepareCardPlay/ResolvePreparedCardPlay.
    /// </summary>
    public class CardHandView : MonoBehaviour
    {
        // ── Referencias inyectadas por CombatUIController.InitializeCardHandView() ──
        private TurnManager _turnManager;
        private RectTransform _handContainer;
        private SpriteFrameAnimatorUI _playerAnimator;
        private CombatFeedbackView _feedbackView;
        private Image _energyPanelImage;
        private Font _uiFont;

        // ── Estado interno ──
        private bool _initialized;
        private bool _isPlayingAttack;
        private float _lastHandWidth;
        private int _lastHandCount = -1;

        private readonly List<CardButtonBinding> _cardButtons = new List<CardButtonBinding>();
        private readonly List<CardDeckEntry> _handCache = new List<CardDeckEntry>();

        // Clase interna que vincula un CardDeckEntry con su botón UI.
        private class CardButtonBinding
        {
            public CardDeckEntry CardEntry;
            public Button Button;
            public Text Label;
            public CanvasGroup Group;
            public Image Art;        // ilustración de la carta (C7); Image hijo creado siempre, una vez.
            public bool ArtShown;    // última visibilidad de arte aplicada (guard anti-thrash de layout).
        }

        // ── Constantes de layout del arte (C7) ──
        // Región superior para la ilustración; el texto baja a la franja inferior
        // cuando hay arte. Sin arte, el texto vuelve a full-card (look actual).
        private static readonly Vector2 ArtAnchorMin = new Vector2(0.06f, 0.42f);
        private static readonly Vector2 ArtAnchorMax = new Vector2(0.94f, 0.96f);
        private static readonly Vector2 LabelWithArtAnchorMin = new Vector2(0f, 0f);
        private static readonly Vector2 LabelWithArtAnchorMax = new Vector2(1f, 0.40f);
        private static readonly Vector2 LabelFullAnchorMin = new Vector2(0f, 0f);
        private static readonly Vector2 LabelFullAnchorMax = new Vector2(1f, 1f);
        private const int LabelFontFull = 20;
        private const int LabelFontWithArt = 16;

        // ── Constantes de layout ──
        // Tamaños base y mínimos para escalar la mano cuando hay muchas cartas.
        private const float HandCardWidthBase = 230f;
        private const float HandCardHeightBase = 130f;
        private const float HandCardWidthMin = 150f;
        private const float HandCardHeightMin = 96f;
        private const float HandSpacingBase = 12f;
        private const float HandSpacingMin = 4f;

        // ── Colores de cartas ──
        private static readonly Color CardButtonNormalColor = new Color(0.18f, 0.18f, 0.25f, 0.95f);
        private static readonly Color CardButtonDisabledColor = new Color(0.1f, 0.1f, 0.14f, 0.6f);
        private static readonly Color DisabledLabelColor = new Color(0.75f, 0.75f, 0.75f, 0.7f);

        /// <summary>
        /// Inyecta todas las referencias necesarias. Llamado por CombatUIController
        /// después de BuildUI(). Sin esta llamada, el componente no hace nada.
        /// </summary>
        public void Initialize(
            TurnManager turnManager,
            RectTransform handContainer,
            SpriteFrameAnimatorUI playerAnimator,
            CombatFeedbackView feedbackView,
            Image energyPanelImage,
            Font uiFont)
        {
            _turnManager = turnManager;
            _handContainer = handContainer;
            _playerAnimator = playerAnimator;
            _feedbackView = feedbackView;
            _energyPanelImage = energyPanelImage;
            _uiFont = uiFont;
            _initialized = true;
        }

        // ────────────────────────────────────────────────────────
        // Sync: llamado cada frame por CombatUIController.Update()
        // ────────────────────────────────────────────────────────

        /// <summary>
        /// Sincroniza los botones de la mano con el estado actual del TurnManager.
        /// Reconstruye si cambió la composición de la mano; actualiza interactividad
        /// y labels si solo cambió el estado.
        /// </summary>
        public void SyncHandButtons(bool forceRebuild = false)
        {
            if (!_initialized || _turnManager == null) return;

            if (forceRebuild || !IsHandCacheValid())
            {
                RebuildHandButtons();
            }

            bool canInteract = _turnManager.IsPlayerTurn();
            foreach (CardButtonBinding binding in _cardButtons)
            {
                bool canPlay = _turnManager.CanPlayCard(binding.CardEntry);
                binding.Button.interactable = canInteract && canPlay;
                binding.Label.text = BuildCardLabel(binding.CardEntry);
                if (binding.Label != null)
                {
                    binding.Label.color = binding.Button.interactable ? Color.white : DisabledLabelColor;
                }
                Image image = binding.Button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = binding.Button.interactable ? CardButtonNormalColor : CardButtonDisabledColor;
                }

                // Arte del lado activo (C7): conmuta en vivo con el mundo por el mismo
                // camino que el label (GetActiveCardDefinition resuelve A/B). Sin arte,
                // ApplyCardArtLayout devuelve el texto a full-card (look actual).
                if (binding.Art != null)
                {
                    CardDefinition activeCard = _turnManager.GetActiveCardDefinition(binding.CardEntry);
                    Sprite art = activeCard != null ? activeCard.Art : null;
                    binding.Art.sprite = art;
                    ApplyCardArtLayout(binding, art != null);
                }
            }

            UpdateHandLayout();
        }

        /// <summary>
        /// Indica si hay una animación de ataque en curso. CombatUIController
        /// puede consultar esto para evitar acciones concurrentes.
        /// </summary>
        public bool IsPlayingAttack => _isPlayingAttack;

        // ────────────────────────────────────────────────────────
        // Card click handler
        // ────────────────────────────────────────────────────────

        private void OnCardButtonClicked(CardDeckEntry entry)
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.CardPlaySFX);
            if (_turnManager == null || entry == null || _isPlayingAttack) return;

            if (!_turnManager.TryPrepareCardPlay(entry, out PreparedCardPlay prepared))
            {
                _feedbackView?.FlashPanel(_energyPanelImage);
                return;
            }

            SyncHandButtons(forceRebuild: true);

            if (prepared.IsAttackCard && _playerAnimator != null)
            {
                _isPlayingAttack = true;
                SetCardButtonsInteractable(false);
                _playerAnimator.PlayAttackOnce(() =>
                {
                    _turnManager.ResolvePreparedCardPlay(prepared);
                    _isPlayingAttack = false;
                    SyncHandButtons(forceRebuild: true);
                });
            }
            else
            {
                _turnManager.ResolvePreparedCardPlay(prepared);
                SyncHandButtons(forceRebuild: true);
            }
        }

        // ────────────────────────────────────────────────────────
        // Hand cache validation + rebuild
        // ────────────────────────────────────────────────────────

        private bool IsHandCacheValid()
        {
            if (_turnManager == null) return false;

            IReadOnlyList<CardDeckEntry> hand = _turnManager.PlayerHand;
            if (hand.Count != _handCache.Count) return false;

            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i] != _handCache[i]) return false;
            }

            return true;
        }

        private void RebuildHandButtons()
        {
            foreach (CardButtonBinding binding in _cardButtons)
            {
                if (binding?.Button != null)
                {
                    DOTween.Kill(binding.Button.GetComponent<RectTransform>());
                    if (binding.Group != null) DOTween.Kill(binding.Group);
                    Destroy(binding.Button.gameObject);
                }
            }

            _cardButtons.Clear();
            _handCache.Clear();

            IReadOnlyList<CardDeckEntry> hand = _turnManager.PlayerHand;
            for (int i = 0; i < hand.Count; i++)
            {
                CardDeckEntry entry = hand[i];
                Button button = CreateCardButton(entry, _handContainer, out Image artImage, out Text label);

                // Juice: staggered fade-in (LayoutGroup controla posición y escala).
                CanvasGroup cardGroup = button.gameObject.AddComponent<CanvasGroup>();
                cardGroup.alpha = 0f;
                float delay = i * 0.05f;
                UIAnimationHelper.FadeIn(cardGroup, 0.2f).SetDelay(delay);

                _cardButtons.Add(new CardButtonBinding
                {
                    CardEntry = entry,
                    Button = button,
                    Label = label,
                    Group = cardGroup,
                    Art = artImage,
                    ArtShown = false   // el primer sync aplica el layout correcto.
                });
                _handCache.Add(entry);
            }
        }

        // ────────────────────────────────────────────────────────
        // Card button creation
        // ────────────────────────────────────────────────────────

        private Button CreateCardButton(CardDeckEntry entry, Transform parent, out Image artImage, out Text labelText)
        {
            CardDefinition activeCard = _turnManager.GetActiveCardDefinition(entry);
            string label = activeCard != null ? activeCard.name : "Card";

            GameObject buttonGO = new GameObject(label + "_Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGO.transform.SetParent(parent, false);

            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(HandCardWidthBase, HandCardHeightBase);

            Image image = buttonGO.GetComponent<Image>();
            image.color = CardButtonNormalColor;

            LayoutElement layout = buttonGO.GetComponent<LayoutElement>();
            layout.preferredWidth = HandCardWidthBase;
            layout.preferredHeight = HandCardHeightBase;

            Button button = buttonGO.GetComponent<Button>();

            // Arte de la carta (C7): Image hijo en la región superior, creado SIEMPRE
            // (una vez). El sprite y la visibilidad los setea el sync. Mismo patrón que
            // CombatUIController.CreateAvatar (preserveAspect, no captura raycasts).
            artImage = CreateArtImage("Art", rect);

            // Texto de la carta: nombre, tipo, costo, descripción.
            labelText = CreateText("Label", rect, BuildCardLabel(entry), LabelFontFull, TextAnchor.MiddleCenter);
            labelText.fontStyle = FontStyle.Bold;

            button.onClick.AddListener(() => OnCardButtonClicked(entry));

            return button;
        }

        /// <summary>
        /// Crea el Image de la ilustración de la carta, anclado a la región superior
        /// y oculto por defecto (el sync lo enciende sólo si la carta tiene arte).
        /// </summary>
        private Image CreateArtImage(string name, RectTransform parent)
        {
            GameObject artGO = new GameObject(name, typeof(RectTransform), typeof(Image));
            artGO.transform.SetParent(parent, false);

            RectTransform rect = artGO.GetComponent<RectTransform>();
            rect.anchorMin = ArtAnchorMin;
            rect.anchorMax = ArtAnchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image art = artGO.GetComponent<Image>();
            art.preserveAspect = true;
            art.raycastTarget = false;
            art.color = Color.white;
            art.enabled = false;   // arranca apagado; el sync decide según haya sprite.

            return art;
        }

        /// <summary>
        /// Aplica el layout de carta según haya arte o no. Idempotente: sólo re-ancla
        /// el texto cuando cambia la visibilidad del arte (guard anti-thrash, evita
        /// reescribir anchors cada frame). Con arte: imagen arriba + texto en franja
        /// inferior. Sin arte: texto full-card centrado (look actual, cero regresión).
        /// </summary>
        private void ApplyCardArtLayout(CardButtonBinding binding, bool hasArt)
        {
            if (binding == null || binding.Art == null) return;
            if (hasArt == binding.ArtShown && binding.Art.enabled == hasArt) return;

            binding.Art.enabled = hasArt;

            if (binding.Label != null)
            {
                RectTransform labelRect = binding.Label.GetComponent<RectTransform>();
                if (hasArt)
                {
                    labelRect.anchorMin = LabelWithArtAnchorMin;
                    labelRect.anchorMax = LabelWithArtAnchorMax;
                    binding.Label.alignment = TextAnchor.UpperCenter;
                    binding.Label.fontSize = LabelFontWithArt;
                }
                else
                {
                    labelRect.anchorMin = LabelFullAnchorMin;
                    labelRect.anchorMax = LabelFullAnchorMax;
                    binding.Label.alignment = TextAnchor.MiddleCenter;
                    binding.Label.fontSize = LabelFontFull;
                }
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            binding.ArtShown = hasArt;
        }

        /// <summary>
        /// Crea un elemento Text dentro de un parent. Helper local para no depender
        /// de CombatUIController.CreateText() (que es private).
        /// </summary>
        private Text CreateText(string name, RectTransform parent, string initialText, int fontSize, TextAnchor alignment)
        {
            GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(parent, false);

            RectTransform rect = textGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = textGO.GetComponent<Text>();
            text.font = _uiFont;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = initialText;

            return text;
        }

        // ────────────────────────────────────────────────────────
        // Card label + interactability helpers
        // ────────────────────────────────────────────────────────

        private string BuildCardLabel(CardDeckEntry entry)
        {
            if (entry == null) return "Unknown Card";

            CardDefinition activeCard = _turnManager != null ? _turnManager.GetActiveCardDefinition(entry) : null;
            if (activeCard == null) return "Unknown Card";

            string prefix = string.Empty;
            if (entry.DualCard != null)
            {
                prefix = _turnManager.CurrentWorld == TurnManager.WorldSide.A ? "[A] " : "[B] ";
            }

            // Tinte por tipo (C8): el prefijo [Tipo] toma el color del tipo vía rich
            // text. ReadableOnDark mantiene legible el Negro sobre el fondo oscuro de
            // la carta. Color desde la fuente única de verdad (ElementTypeColors).
            string typePrefix = string.Empty;
            if (activeCard.ElementType != ElementType.None)
            {
                string typeHex = ColorUtility.ToHtmlStringRGB(ElementTypeColors.ReadableOnDark(activeCard.ElementType));
                typePrefix = $"<color=#{typeHex}>[{activeCard.ElementType}]</color> ";
            }
            string title = string.IsNullOrEmpty(prefix)
                ? $"{typePrefix}{activeCard.CardName}"
                : $"{prefix}{typePrefix}{activeCard.CardName}";
            return $"{title} (Cost {activeCard.Cost})\n{activeCard.Description}";
        }

        private void SetCardButtonsInteractable(bool interactable)
        {
            foreach (CardButtonBinding binding in _cardButtons)
            {
                if (binding?.Button != null)
                {
                    binding.Button.interactable = interactable;
                }
            }
        }

        // ────────────────────────────────────────────────────────
        // Layout adaptivo: escala cartas si la mano es grande
        // ────────────────────────────────────────────────────────

        /// <summary>
        /// Ajusta tamaño y espaciado de la mano para mantenerla centrada y visible.
        /// Usa preferredWidth/Height (no sizeDelta) porque HorizontalLayoutGroup se guía por LayoutElement.
        /// Se fuerza un rebuild para evitar que el layout quede desplazado.
        /// </summary>
        private void UpdateHandLayout()
        {
            if (_handContainer == null) return;

            int count = _cardButtons.Count;
            float availableWidth = _handContainer.rect.width;
            if (count <= 0 || availableWidth <= 0f) return;

            if (Mathf.Abs(availableWidth - _lastHandWidth) < 1f && count == _lastHandCount) return;

            _lastHandWidth = availableWidth;
            _lastHandCount = count;

            float padding = 32f;
            float targetWidth = Mathf.Max(0f, availableWidth - padding);
            float spacing = HandSpacingBase;
            float cardWidth = HandCardWidthBase;

            float totalWidth = (count * cardWidth) + ((count - 1) * spacing);
            if (totalWidth > targetWidth)
            {
                float scale = targetWidth / totalWidth;
                cardWidth = Mathf.Max(HandCardWidthMin, cardWidth * scale);
                spacing = Mathf.Max(HandSpacingMin, spacing * scale);
            }

            float cardHeight = Mathf.Max(HandCardHeightMin, HandCardHeightBase * (cardWidth / HandCardWidthBase));

            HorizontalLayoutGroup layout = _handContainer.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = spacing;
            }

            bool layoutChanged = false;
            foreach (CardButtonBinding binding in _cardButtons)
            {
                if (binding?.Button == null) continue;

                LayoutElement element = binding.Button.GetComponent<LayoutElement>();
                if (element != null)
                {
                    if (!Mathf.Approximately(element.preferredWidth, cardWidth)
                        || !Mathf.Approximately(element.preferredHeight, cardHeight))
                    {
                        element.preferredWidth = cardWidth;
                        element.preferredHeight = cardHeight;
                        layoutChanged = true;
                    }
                }
            }

            if (layoutChanged)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_handContainer);
            }
        }

        // ────────────────────────────────────────────────────────
        // Cleanup
        // ────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            foreach (CardButtonBinding binding in _cardButtons)
            {
                if (binding?.Button != null)
                {
                    DOTween.Kill(binding.Button.GetComponent<RectTransform>());
                }
                if (binding?.Group != null)
                {
                    DOTween.Kill(binding.Group);
                }
            }
        }
    }
}

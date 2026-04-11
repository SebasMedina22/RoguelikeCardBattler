using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RoguelikeCardBattler.Core.Audio;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Gameplay.Combat
{
    /// <summary>
    /// Construye la UI de combate en runtime: HUD (energía, momentum, mundo, switches),
    /// mano de cartas y popups de feedback. Reconstruye el canvas y captura sprites
    /// existentes si fueron asignados en escena/inspector para que no se pierdan.
    /// </summary>
    public class CombatUIController : MonoBehaviour
    {
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private Font uiFont;
        [SerializeField] private Sprite playerSprite;
        [SerializeField] private Sprite enemySprite;
        [SerializeField] private bool showPlayerBackground = true;
        [SerializeField] private bool showEnemyBackground = true;
        [Header("Background (Canvas Placeholder)")]
        [SerializeField] private bool useCanvasBackground = false;
        [SerializeField] private float playerSpriteScale = 1f;
        [SerializeField] private float enemySpriteScale = 0.8f;
        [SerializeField] private float playerSpriteYOffset = -40f;
        [SerializeField] private float enemySpriteYOffset = -20f;
        [SerializeField] private Color blockOverlayColor = new Color(0.2f, 0.6f, 1f, 0.35f);

        private Canvas _canvas;
        private Text _playerEnergyText;
        private Text _freePlaysText;
        private Text _playerHpLabel;
        private Text _enemyHpLabel;
        private Text _enemyTypeLabel;
        private Text _playerBlockText;
        private Text _enemyBlockText;
        private Text _drawPileText;
        private Text _discardPileText;
        private Text _enemyIntentText;
        private Text _worldLabel;
        private Text _worldSwitchesText;
        private Button _endTurnButton;
        private Button _changeWorldButton;
        private Text _endTurnLabel;
        private Text _changeWorldLabel;
        private Text _handLimitText;
        private RectTransform _handContainer;
        private Image _playerAvatarImage;
        private Image _enemyAvatarImage;
        private Color _playerAvatarBaseColor;
        private Color _enemyAvatarBaseColor;
        private Image _playerBlockOverlay;
        private Image _enemyBlockOverlay;
        private Image _skyBackgroundImage;
        private Image _groundBackgroundImage;
        [SerializeField] private Color worldASkyColor = new Color(0.07f, 0.12f, 0.3f, 1f);
        [SerializeField] private Color worldAGroundColor = new Color(0.02f, 0.07f, 0.09f, 1f);
        [SerializeField] private Color worldBSkyColor = new Color(0.2f, 0.05f, 0.05f, 1f);
        [SerializeField] private Color worldBGroundColor = new Color(0.09f, 0.02f, 0.04f, 1f);
        [Header("Background Sprites")]
        [SerializeField] private Sprite worldASkySprite;
        [SerializeField] private Sprite worldAGroundSprite;
        [SerializeField] private Sprite worldBSkySprite;
        [SerializeField] private Sprite worldBGroundSprite;
        [Header("Hero Animation")]
        [SerializeField] private List<Sprite> heroIdleFrames = new List<Sprite>();
        [SerializeField] private List<Sprite> heroAttackFrames = new List<Sprite>();
        [SerializeField] private float heroAttackFps = 16f;
        private Text _hitFeedbackText;
        // Tiempos de feedback visual ajustables por diseño sin tocar gameplay.
        [Header("Feedback Timing")]
        [SerializeField, Min(0.1f)] private float hitFeedbackDuration = 0.85f;
        private SpriteFrameAnimatorUI _playerAnimator;
        private Image _playerAvatarSpriteImage;
        [Header("Enemy Hit Feedback")]
        [SerializeField] private List<Sprite> enemyHitFrames = new List<Sprite>();
        [SerializeField] private float enemyHitFps = 16f;
        private SpriteFrameAnimatorUI _enemyAnimator;
        private Image _enemyAvatarSpriteImage;
        private Image _energyPanelImage;
        private Image _worldPanelImage;
        // Componentes extraídos que manejan áreas específicas de la UI de combate.
        private CombatFeedbackView _feedbackView;
        private CardHandView _cardHandView;

        // Layout/estilo del HUD: constantes para mantener jerarquía visual y escalado.
        private const float TopBarMinY = 0.92f;
        private const float BattlefieldMinY = 0.28f;
        private const float BattlefieldMaxY = 0.9f;
        private const float BottomBarMaxY = 0.28f;

        private static readonly Color HudPanelColor = new Color(0.03f, 0.03f, 0.05f, 0.65f);
        private static readonly Color HudPanelSoftColor = new Color(0.03f, 0.03f, 0.05f, 0.45f);
        private static readonly Color ButtonNormalColor = new Color(0.22f, 0.22f, 0.32f, 0.95f);
        private static readonly Color ButtonDisabledColor = new Color(0.12f, 0.12f, 0.16f, 0.7f);
        private static readonly Color ButtonPressedColor = new Color(0.35f, 0.35f, 0.45f, 1f);
        private static readonly Color ButtonHighlightColor = new Color(0.3f, 0.3f, 0.4f, 1f);
        private static readonly Color DisabledLabelColor = new Color(0.75f, 0.75f, 0.75f, 0.7f);
        private static readonly Color WarningLabelColor = new Color(1f, 0.6f, 0.6f, 1f);
        [SerializeField, Min(0.1f)] private float handLimitToastDuration = 0.7f;
        [SerializeField, Min(0f)] private float handLimitToastCooldown = 0.6f;

        private void Awake()
        {
            if (turnManager == null)
            {
                turnManager = GetComponent<TurnManager>();
            }

            CaptureExistingAvatarSprites();
            if (uiFont == null)
            {
                // Unity 6 reemplazó Arial por LegacyRuntime como built-in
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            EnsureEventSystem();
            // Nota: BuildUI() se llamará desde Start(), no desde Awake(),
            // para permitir que BattleFlowController configure el TurnManager correctamente primero
        }

        private void Start()
        {
            if (_canvas == null)
            {
                BuildUI();
            }
        }

        private void OnEnable()
        {
            if (turnManager == null)
            {
                turnManager = GetComponent<TurnManager>();
            }

            // Eventos de feedback (PlayerHitEffectiveness, EnemyTookDamage,
            // PlayerHandLimitReached) delegados a CombatFeedbackView.
        }

        private void OnDisable()
        {
            // Desuscripción de eventos de feedback delegada a CombatFeedbackView.
        }

        private void Update()
        {
            if (turnManager == null)
            {
                return;
            }

            UpdateInfoTexts();
            _cardHandView?.SyncHandButtons();
            // Hit feedback y hand limit timers delegados a CombatFeedbackView.
        }

        private void EnsureEventSystem()
        {
            EventSystem existing = Object.FindFirstObjectByType<EventSystem>();
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
        }

        /// <summary>
        /// Si ya existen instancias de UI en escena (por ejemplo, con sprites asignados en editor),
        /// preserva esos sprites y limpia la UI previa para que BuildUI regenere con esos sprites.
        /// IMPORTANTE: sprites asignados dentro del canvas original se pierden si no se
        /// configuran aquí; asignarlos al controller o asegurarse de que estén capturados.
        /// </summary>
        private void CaptureExistingAvatarSprites()
        {
            Transform existingCanvas = transform.Find("CombatCanvas");
            if (existingCanvas == null)
            {
                return;
            }

            Transform battlefield = existingCanvas.Find("BattlefieldPanel");
            if (battlefield != null)
            {
                Transform playerAvatar = battlefield.Find("PlayerAvatar");
                if (playerAvatar != null)
                {
                    Image img = playerAvatar.GetComponent<Image>();
                    if (img != null && img.sprite != null && playerSprite == null)
                    {
                        playerSprite = img.sprite;
                    }
                }

                Transform enemyAvatar = battlefield.Find("EnemyAvatar");
                if (enemyAvatar != null)
                {
                    Image img = enemyAvatar.GetComponent<Image>();
                    if (img != null && img.sprite != null && enemySprite == null)
                    {
                        enemySprite = img.sprite;
                    }
                }
            }

            Destroy(existingCanvas.gameObject);
        }

        private void BuildUI()
        {
            _canvas = CreateCanvas("CombatCanvas");
            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();

            if (useCanvasBackground)
            {
                CreateBackgroundLayers(canvasRect);
            }

            // Layout principal del HUD:
            // - TopBar: estado de mundo y switches (ligero, lectura rápida).
            // - Battlefield: zona central sin overlap para avatares/intentos.
            // - BottomBar: mano centrada + energía y botones principales.
            RectTransform topBar = CreatePanel(
                "TopBar",
                canvasRect,
                new Vector2(0f, TopBarMinY),
                new Vector2(1f, 1f),
                HudPanelSoftColor);

            RectTransform battlefield = CreatePanel(
                "BattlefieldPanel",
                canvasRect,
                new Vector2(0.04f, BattlefieldMinY),
                new Vector2(0.96f, BattlefieldMaxY),
                new Color(0.04f, 0.07f, 0.11f, 0.55f));

            RectTransform bottomBar = CreatePanel(
                "BottomBar",
                canvasRect,
                new Vector2(0f, 0f),
                new Vector2(1f, BottomBarMaxY),
                HudPanelSoftColor);

            _playerAvatarImage = CreateAvatar(
                "PlayerAvatar",
                battlefield,
                new Vector2(0.05f, 0.1f),
                new Vector2(0.3f, 0.9f),
                new Color(0.2f, 0.55f, 0.9f),
                "Pilot",
                playerSprite,
                showPlayerBackground,
                playerSpriteScale,
                new Vector2(0f, playerSpriteYOffset));
            _playerAvatarBaseColor = _playerAvatarImage.color;

            Vector2 enemyOffset = new Vector2(0f, enemySpriteYOffset);
            float enemyScale = enemySpriteScale;
            Sprite finalEnemySprite = enemySprite;
#if UNITY_EDITOR
            Debug.Log($"[CombatUI] enemySprite asignado en inspector: {enemySprite}");
#endif
            if (turnManager != null)
            {
                enemyOffset += turnManager.CurrentEnemyAvatarOffset;
                enemyScale = turnManager.CurrentEnemyAvatarScale;
                
#if UNITY_EDITOR
                Debug.Log($"[CombatUI] CurrentEnemyDefinition: {turnManager.CurrentEnemyDefinition}");
                if (turnManager.CurrentEnemyDefinition != null)
                {
                    Debug.Log($"[CombatUI] Enemy Avatar from Definition: {turnManager.CurrentEnemyDefinition.Avatar}");
                }
#endif
                
                // Si no hay sprite asignado manualmente, intenta cargar desde la EnemyDefinition
                if (finalEnemySprite == null && turnManager.CurrentEnemyDefinition != null)
                {
                    finalEnemySprite = turnManager.CurrentEnemyDefinition.Avatar;
#if UNITY_EDITOR
                    Debug.Log($"[CombatUI] Usando sprite del enemigo: {finalEnemySprite}");
#endif
                }
            }

            _enemyAvatarImage = CreateAvatar(
                "EnemyAvatar",
                battlefield,
                new Vector2(0.7f, 0.1f),
                new Vector2(0.95f, 0.9f),
                new Color(0.95f, 0.4f, 0.4f),
                turnManager.CurrentEnemyDefinition?.EnemyName ?? "Enemy",
                finalEnemySprite,
                showEnemyBackground,
                enemyScale,
                enemyOffset);
            _enemyAvatarBaseColor = _enemyAvatarImage.color;
            InitializeEnemyAnimator();

            RectTransform energyPanel = CreatePanel(
                "EnergyPanel",
                bottomBar,
                new Vector2(0.02f, 0.52f),
                new Vector2(0.17f, 0.95f),
                HudPanelColor);
            _energyPanelImage = energyPanel.GetComponent<Image>();
            _playerEnergyText = CreateText("PlayerEnergy", energyPanel, "Energy 0/0", 26, TextAnchor.UpperCenter);
            _freePlaysText = CreateText("FreePlays", energyPanel, "Momentum: 0", 18, TextAnchor.LowerCenter);

            RectTransform worldPanel = CreatePanel(
                "WorldPanel",
                topBar,
                new Vector2(0.38f, 0.1f),
                new Vector2(0.62f, 0.9f),
                HudPanelColor);
            _worldPanelImage = worldPanel.GetComponent<Image>();
            _worldLabel = CreateText("WorldLabel", worldPanel, "World: A", 24, TextAnchor.UpperCenter);
            _worldSwitchesText = CreateText("WorldSwitches", worldPanel, "Switches: 0/0", 18, TextAnchor.LowerCenter);

            RectTransform handPanel = CreatePanel(
                "HandPanel",
                bottomBar,
                new Vector2(0.18f, 0.12f),
                new Vector2(0.82f, 0.95f),
                new Color(0.08f, 0.08f, 0.12f, 0.75f));
            HorizontalLayoutGroup layout = handPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12f;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.padding = new RectOffset(16, 16, 12, 12);
            _handContainer = handPanel;


            _playerHpLabel = CreateText("PlayerHpLabel", _playerAvatarImage.rectTransform, "0/0", 24, TextAnchor.LowerCenter);
            RectTransform playerHpRect = _playerHpLabel.GetComponent<RectTransform>();
            playerHpRect.anchorMin = new Vector2(0.1f, -0.15f);
            playerHpRect.anchorMax = new Vector2(0.9f, -0.02f);
            playerHpRect.offsetMin = Vector2.zero;
            playerHpRect.offsetMax = Vector2.zero;
            CreateBlockUI(_playerAvatarImage.rectTransform, "Player", out _playerBlockOverlay, out _playerBlockText);

            _enemyHpLabel = CreateText("EnemyHpLabel", _enemyAvatarImage.rectTransform, "0/0", 24, TextAnchor.LowerCenter);
            RectTransform enemyHpRect = _enemyHpLabel.GetComponent<RectTransform>();
            enemyHpRect.anchorMin = new Vector2(0.1f, -0.15f);
            enemyHpRect.anchorMax = new Vector2(0.9f, -0.02f);
            enemyHpRect.offsetMin = Vector2.zero;
            enemyHpRect.offsetMax = Vector2.zero;
            _enemyTypeLabel = CreateText("EnemyTypeLabel", _enemyAvatarImage.rectTransform, "Type: —", 18, TextAnchor.UpperCenter);
            RectTransform enemyTypeRect = _enemyTypeLabel.GetComponent<RectTransform>();
            enemyTypeRect.anchorMin = new Vector2(0.05f, 0.76f);
            enemyTypeRect.anchorMax = new Vector2(0.95f, 0.88f);
            enemyTypeRect.offsetMin = Vector2.zero;
            enemyTypeRect.offsetMax = Vector2.zero;
            CreateBlockUI(_enemyAvatarImage.rectTransform, "Enemy", out _enemyBlockOverlay, out _enemyBlockText);

            RectTransform intentPanel = CreatePanel(
                "EnemyIntentPanel",
                _enemyAvatarImage.rectTransform,
                new Vector2(0.1f, 1.02f),
                new Vector2(0.9f, 1.2f),
                HudPanelColor);
            _enemyIntentText = CreateText("EnemyIntent", intentPanel, "?", 24, TextAnchor.MiddleCenter);

            _hitFeedbackText = CreateText("HitFeedback", battlefield, "", 30, TextAnchor.UpperCenter);
            RectTransform hitRect = _hitFeedbackText.GetComponent<RectTransform>();
            hitRect.anchorMin = new Vector2(0.35f, 0.92f);
            hitRect.anchorMax = new Vector2(0.65f, 0.99f);
            hitRect.offsetMin = Vector2.zero;
            hitRect.offsetMax = Vector2.zero;
            _hitFeedbackText.gameObject.SetActive(false);

            // Toast para límite de mano: visible y centrado en el battlefield.
            _handLimitText = CreateText("HandLimitText", battlefield, "", 28, TextAnchor.UpperCenter);
            _handLimitText.fontStyle = FontStyle.Bold;
            RectTransform handLimitRect = _handLimitText.GetComponent<RectTransform>();
            handLimitRect.anchorMin = new Vector2(0.35f, 0.86f);
            handLimitRect.anchorMax = new Vector2(0.65f, 0.93f);
            handLimitRect.offsetMin = Vector2.zero;
            handLimitRect.offsetMax = Vector2.zero;
            _handLimitText.color = Color.white;
            _handLimitText.gameObject.SetActive(false);

            _drawPileText = CreateCornerCounter(bottomBar, new Vector2(0.02f, 0.05f), new Vector2(0.14f, 0.32f), "Draw: 0");
            _discardPileText = CreateCornerCounter(bottomBar, new Vector2(0.84f, 0.05f), new Vector2(0.14f, 0.32f), "Discard: 0");

            _endTurnButton = CreateButton(
                "EndTurnButton",
                bottomBar,
                new Vector2(0.82f, 0.52f),
                new Vector2(0.98f, 0.95f),
                "End Turn");
            _endTurnLabel = _endTurnButton.GetComponentInChildren<Text>();
            _endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
            ApplyButtonStyle(_endTurnButton);

            _changeWorldButton = CreateButton(
                "ChangeWorldButton",
                topBar,
                new Vector2(0.74f, 0.1f),
                new Vector2(0.98f, 0.9f),
                "Change World");
            _changeWorldLabel = _changeWorldButton.GetComponentInChildren<Text>();
            _changeWorldButton.onClick.AddListener(OnChangeWorldButtonClicked);
            ApplyButtonStyle(_changeWorldButton);

            InitializePlayerAnimator();
            UpdateWorldVisuals();
            InitializeExtractedViews();
        }

        /// <summary>
        /// Crea e inicializa los componentes extraídos (CombatFeedbackView, CardHandView).
        /// Les pasa las referencias de UI que necesitan después de BuildUI().
        /// </summary>
        private void InitializeExtractedViews()
        {
            _feedbackView = gameObject.AddComponent<CombatFeedbackView>();
            _feedbackView.Initialize(
                turnManager,
                _hitFeedbackText,
                _handLimitText,
                _playerHpLabel,
                _playerAvatarImage,
                _enemyAvatarImage,
                _enemyAvatarSpriteImage,
                _energyPanelImage,
                _worldPanelImage,
                _enemyAnimator,
                _canvas,
                uiFont,
                hitFeedbackDuration,
                handLimitToastDuration,
                handLimitToastCooldown);

            _cardHandView = gameObject.AddComponent<CardHandView>();
            _cardHandView.Initialize(
                turnManager,
                _handContainer,
                _playerAnimator,
                _feedbackView,
                _energyPanelImage,
                uiFont);
        }
        private Text CreateCornerCounter(RectTransform parent, Vector2 anchorMin, Vector2 size, string label)
        {
            RectTransform panel = CreatePanel(
                label + "_Panel",
                parent,
                anchorMin,
                anchorMin + size,
                HudPanelColor);
            return CreateText(label + "_Text", panel, label, 20, TextAnchor.MiddleCenter);
        }


        private Canvas CreateCanvas(string name)
        {
            GameObject canvasGO = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private void CreateBackgroundLayers(RectTransform canvasRect)
        {
            RectTransform sky = CreatePanel(
                "BackgroundSky",
                canvasRect,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                worldASkyColor);
            sky.SetAsFirstSibling();
            _skyBackgroundImage = sky.GetComponent<Image>();

            RectTransform ground = CreatePanel(
                "BackgroundGround",
                canvasRect,
                new Vector2(0, 0),
                new Vector2(1, 0.45f),
                worldAGroundColor);
            ground.SetSiblingIndex(1);
            _groundBackgroundImage = ground.GetComponent<Image>();
        }

        private RectTransform CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Color? background = null)
        {
            GameObject panelGO = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(parent, false);

            RectTransform rect = panelGO.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            Image image = panelGO.GetComponent<Image>();
            image.color = background ?? new Color(0f, 0f, 0f, 0.4f);
            image.raycastTarget = false;

            return rect;
        }

        private Image CreateAvatar(
            string name,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color tint,
            string label,
            Sprite sprite,
            bool showBackground,
            float spriteScale,
            Vector2 spriteOffset)
        {
            RectTransform rect = CreatePanel(name, parent, anchorMin, anchorMax, tint);
            Image image = rect.GetComponent<Image>();
            image.color = showBackground ? tint : new Color(tint.r, tint.g, tint.b, 0f);

            if (sprite != null)
            {
                GameObject spriteGO = new GameObject(name + "_Sprite", typeof(RectTransform), typeof(Image));
                spriteGO.transform.SetParent(rect, false);
                RectTransform spriteRect = spriteGO.GetComponent<RectTransform>();
                spriteRect.anchorMin = new Vector2(0.1f, 0.1f);
                spriteRect.anchorMax = new Vector2(0.9f, 0.9f);
                spriteRect.offsetMin = Vector2.zero;
                spriteRect.offsetMax = Vector2.zero;

                Image spriteImage = spriteGO.GetComponent<Image>();
                spriteImage.sprite = sprite;
                spriteImage.preserveAspect = true;
                spriteImage.raycastTarget = false;
                spriteImage.color = Color.white;
                spriteRect.localScale = Vector3.one * Mathf.Max(0.1f, spriteScale);
                spriteRect.anchoredPosition += spriteOffset;
            }

            Text caption = CreateText(name + "_Label", rect, label, 30, TextAnchor.UpperCenter);
            caption.fontStyle = FontStyle.Bold;
            RectTransform captionRect = caption.GetComponent<RectTransform>();
            captionRect.anchorMin = new Vector2(0, 0.7f);
            captionRect.anchorMax = new Vector2(1, 1);
            captionRect.offsetMin = Vector2.zero;
            captionRect.offsetMax = Vector2.zero;

            return image;
        }

        private Text CreateText(string name, RectTransform parent, string initialText, int fontSize = 24, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(parent, false);

            RectTransform rect = textGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = textGO.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = initialText;

            return text;
        }

        private Button CreateButton(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string label)
        {
            GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(parent, false);

            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonGO.GetComponent<Image>();
            image.color = ButtonNormalColor;

            Button button = buttonGO.GetComponent<Button>();
            button.targetGraphic = image;

            Text labelText = CreateText("Label", rect, label, 26, TextAnchor.MiddleCenter);
            labelText.fontStyle = FontStyle.Bold;

            return button;
        }

        private void CreateBlockUI(RectTransform parent, string prefix, out Image overlay, out Text blockText)
        {
            GameObject overlayGO = new GameObject(prefix + "_BlockOverlay", typeof(RectTransform), typeof(Image));
            overlayGO.transform.SetParent(parent, false);
            RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
            overlayRect.anchorMin = new Vector2(0.05f, 0.05f);
            overlayRect.anchorMax = new Vector2(0.95f, 0.95f);
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            overlay = overlayGO.GetComponent<Image>();
            overlay.color = new Color(blockOverlayColor.r, blockOverlayColor.g, blockOverlayColor.b, 0f);
            overlay.raycastTarget = false;

            blockText = CreateText(prefix + "_BlockText", parent, "Block 0", 20, TextAnchor.MiddleCenter);
            RectTransform blockRect = blockText.GetComponent<RectTransform>();
            blockRect.anchorMin = new Vector2(0.12f, 0.02f);
            blockRect.anchorMax = new Vector2(0.88f, 0.16f);
            blockRect.offsetMin = Vector2.zero;
            blockRect.offsetMax = Vector2.zero;
        }

        // CreateCardButton extraído a CardHandView.

        private void OnEndTurnButtonClicked()
        {
            if (turnManager == null || !turnManager.IsPlayerTurn())
            {
                _feedbackView?.FlashPanel(_energyPanelImage);
                return;
            }

            turnManager.EndPlayerTurn();
        }

        // OnCardButtonClicked extraído a CardHandView.

        private void UpdateInfoTexts()
        {
            _playerEnergyText.text = $"Energy {turnManager.PlayerEnergy}/{turnManager.PlayerMaxEnergy}";
            if (_freePlaysText != null)
            {
                _freePlaysText.text = $"Momentum: {turnManager.FreePlays}";
            }
            if (_worldSwitchesText != null)
            {
                string switchesLabel = turnManager.DebugUnlimitedWorldSwitches
                    ? "Switches: ∞"
                    : $"Switches: {turnManager.WorldSwitchesUsed}/{turnManager.MaxWorldSwitchesPerCombat}";
                _worldSwitchesText.text = switchesLabel;
            }
            _playerHpLabel.text = $"{turnManager.PlayerHP}/{turnManager.PlayerMaxHP}";
            _enemyHpLabel.text = $"{turnManager.EnemyHP}/{turnManager.EnemyMaxHP}";
            _enemyTypeLabel.text = BuildEnemyTypeLabel();
            _drawPileText.text = $"Draw: {turnManager.PlayerDrawPileCount}";
            _discardPileText.text = $"Discard: {turnManager.PlayerDiscardPileCount}";
            _enemyIntentText.text = BuildEnemyIntentLabel();
            UpdateBlockVisuals(_playerBlockText, _playerBlockOverlay, turnManager.PlayerBlock);
            UpdateBlockVisuals(_enemyBlockText, _enemyBlockOverlay, turnManager.EnemyBlock);
            UpdateWorldVisuals();

            bool playerTurn = turnManager.IsPlayerTurn();
            _endTurnButton.interactable = playerTurn && !turnManager.IsCombatFinished;
            if (_endTurnLabel != null)
            {
                _endTurnLabel.color = _endTurnButton.interactable ? Color.white : DisabledLabelColor;
            }
            bool worldSwitchAvailable = turnManager.DebugUnlimitedWorldSwitches
                || turnManager.WorldSwitchesUsed < turnManager.MaxWorldSwitchesPerCombat;
            _changeWorldButton.interactable = playerTurn
                && !turnManager.IsCombatFinished
                && worldSwitchAvailable;
            if (_changeWorldLabel != null)
            {
                _changeWorldLabel.color = _changeWorldButton.interactable ? Color.white : DisabledLabelColor;
            }
            if (_worldSwitchesText != null)
            {
                _worldSwitchesText.color = worldSwitchAvailable ? Color.white : WarningLabelColor;
            }
            UpdateAvatarHighlight(playerTurn);
        }

        private void UpdateAvatarHighlight(bool playerTurn)
        {
            if (_playerAvatarImage == null || _enemyAvatarImage == null)
            {
                return;
            }

            _playerAvatarImage.color = playerTurn
                ? _playerAvatarBaseColor
                : DimColor(_playerAvatarBaseColor, 0.6f);

            _enemyAvatarImage.color = playerTurn
                ? DimColor(_enemyAvatarBaseColor, 0.6f)
                : _enemyAvatarBaseColor;
        }

        private Color DimColor(Color color, float factor)
        {
            factor = Mathf.Clamp01(factor);
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
        }

        // SetCardButtonsInteractable, SyncHandButtons, IsHandCacheValid,
        // RebuildHandButtons extraídos a CardHandView.

        private void InitializePlayerAnimator()
        {
            if (_playerAvatarImage == null)
            {
                return;
            }

            Transform spriteChild = _playerAvatarImage.transform.Find("PlayerAvatar_Sprite");
            if (spriteChild == null)
            {
                return;
            }

            _playerAvatarSpriteImage = spriteChild.GetComponent<Image>();
            if (_playerAvatarSpriteImage == null)
            {
                return;
            }

            _playerAnimator = spriteChild.GetComponent<SpriteFrameAnimatorUI>();
            if (_playerAnimator == null)
            {
                _playerAnimator = spriteChild.gameObject.AddComponent<SpriteFrameAnimatorUI>();
            }

            _playerAnimator.Configure(_playerAvatarSpriteImage, heroIdleFrames, heroAttackFrames, heroAttackFps);
            _playerAnimator.PlayIdleLoop();
        }

        private void InitializeEnemyAnimator()
        {
            if (_enemyAvatarImage == null)
            {
                return;
            }

            Transform spriteChild = _enemyAvatarImage.transform.Find("EnemyAvatar_Sprite");
            if (spriteChild == null)
            {
                return;
            }

            _enemyAvatarSpriteImage = spriteChild.GetComponent<Image>();
            if (_enemyAvatarSpriteImage == null)
            {
                return;
            }

            _enemyAnimator = spriteChild.GetComponent<SpriteFrameAnimatorUI>();
            if (_enemyAnimator == null)
            {
                _enemyAnimator = spriteChild.gameObject.AddComponent<SpriteFrameAnimatorUI>();
            }

            // Idle opcional (no configurado por ahora) se deja vacío; se usará para hit si hay frames.
            _enemyAnimator.Configure(_enemyAvatarSpriteImage, new List<Sprite>(), enemyHitFrames, enemyHitFps);
        }

        // BuildCardLabel extraído a CardHandView.

        private string BuildEnemyIntentLabel()
        {
            EnemyIntentType intentType = turnManager?.PlannedEnemyIntentType ?? EnemyIntentType.Unknown;
            int value = turnManager?.PlannedEnemyIntentValue ?? 0;

            return intentType switch
            {
                EnemyIntentType.Attack when value > 0 => $"ATTACK {value}",
                EnemyIntentType.Defend when value > 0 => $"DEFEND {value}",
                EnemyIntentType.Attack => "ATTACK",
                EnemyIntentType.Defend => "DEFEND",
                _ => "?"
            };
        }

        private string BuildEnemyTypeLabel()
        {
            if (turnManager == null)
            {
                return "Type: —";
            }

            ElementType type = turnManager.EnemyElementType;
            return type == ElementType.None ? "Type: —" : $"Type: {type}";
        }

        private void UpdateBlockVisuals(Text label, Image overlay, int blockValue)
        {
            if (label != null)
            {
                label.text = $"Block {blockValue}";
            }

            if (overlay != null)
            {
                overlay.color = new Color(
                    blockOverlayColor.r,
                    blockOverlayColor.g,
                    blockOverlayColor.b,
                    blockValue > 0 ? blockOverlayColor.a : 0f);
            }
        }

        private void OnChangeWorldButtonClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.WorldChangeSFX);
            if (turnManager == null)
            {
                return;
            }

            bool changed = turnManager.TryChangeWorld();
            UpdateWorldVisuals();
            _cardHandView?.SyncHandButtons(forceRebuild: true);
            if (!changed)
            {
                _feedbackView?.FlashPanel(_worldPanelImage);
            }
        }

        // OnPlayerHandLimitReached, HandLimitCooldownRoutine extraídos a CombatFeedbackView.
        // UpdateHandLayout extraído a CardHandView.

        private void ApplyButtonStyle(Button button)
        {
            if (button == null)
            {
                return;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = ButtonNormalColor;
            colors.highlightedColor = ButtonHighlightColor;
            colors.pressedColor = ButtonPressedColor;
            colors.disabledColor = ButtonDisabledColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        // FlashPanel y FlashPanelRoutine extraídos a CombatFeedbackView.

        private void UpdateWorldVisuals()
        {
            if (turnManager == null || _worldLabel == null)
            {
                return;
            }

            string worldLabel = turnManager.CurrentWorld == TurnManager.WorldSide.A ? "A" : "B";
            _worldLabel.text = $"World: {worldLabel}";

            bool isWorldA = turnManager.CurrentWorld == TurnManager.WorldSide.A;
            Color targetSky = isWorldA ? worldASkyColor : worldBSkyColor;
            Color targetGround = isWorldA ? worldAGroundColor : worldBGroundColor;
            Sprite targetSkySprite = isWorldA ? worldASkySprite : worldBSkySprite;
            Sprite targetGroundSprite = isWorldA ? worldAGroundSprite : worldBGroundSprite;

            if (_skyBackgroundImage != null)
            {
                if (targetSkySprite != null)
                {
                    _skyBackgroundImage.sprite = targetSkySprite;
                    _skyBackgroundImage.type = Image.Type.Simple;
                    _skyBackgroundImage.preserveAspect = true;
                    _skyBackgroundImage.color = Color.white;
                    CoverFill(_skyBackgroundImage.rectTransform, targetSkySprite);
                }
                else
                {
                    _skyBackgroundImage.sprite = null;
                    _skyBackgroundImage.color = targetSky;
                }
            }

            if (_groundBackgroundImage != null)
            {
                if (targetGroundSprite != null)
                {
                    _groundBackgroundImage.sprite = targetGroundSprite;
                    _groundBackgroundImage.type = Image.Type.Simple;
                    _groundBackgroundImage.preserveAspect = true;
                    _groundBackgroundImage.color = Color.white;
                    CoverFill(_groundBackgroundImage.rectTransform, targetGroundSprite);
                }
                else
                {
                    _groundBackgroundImage.sprite = null;
                    // Si no hay sprite de ground, lo dejamos transparente para no tapar el sky.
                    Color transparent = targetGround;
                    transparent.a = 0f;
                    _groundBackgroundImage.color = transparent;
                }
            }
        }

        /// <summary>
        /// Ajusta el rect transform para simular "cover": escala manteniendo aspect
        /// y recorta lo sobrante para llenar el panel.
        /// </summary>
        private void CoverFill(RectTransform rectTransform, Sprite sprite)
        {
            if (rectTransform == null || sprite == null)
            {
                return;
            }

            Rect panelRect = rectTransform.rect;
            float panelAspect = panelRect.width / Mathf.Max(0.0001f, panelRect.height);
            float spriteAspect = sprite.rect.width / Mathf.Max(0.0001f, sprite.rect.height);

            Vector3 scale = Vector3.one;
            if (spriteAspect > panelAspect)
            {
                // Sprite más "ancho": escalar por altura, recortar ancho.
                float heightScale = 1f;
                float widthScale = spriteAspect / panelAspect;
                scale = new Vector3(widthScale, heightScale, 1f);
            }
            else
            {
                // Sprite más "alto": escalar por ancho, recortar alto.
                float widthScale = 1f;
                float heightScale = panelAspect / spriteAspect;
                scale = new Vector3(widthScale, heightScale, 1f);
            }

            rectTransform.localScale = scale;
        }

        // ShowVictoryText, ShowDefeatText, ShowCombatResultText extraídos a CombatFeedbackView.
        // Acceso público via: _feedbackView.ShowVictoryText() / _feedbackView.ShowDefeatText()

        // OnDestroy: tweens de card buttons limpiados por CardHandView.OnDestroy(),
        // tweens de avatares/labels limpiados por CombatFeedbackView.OnDestroy().
    }
}


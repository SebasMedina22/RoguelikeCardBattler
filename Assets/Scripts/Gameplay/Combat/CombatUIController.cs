using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
        [SerializeField] private float playerSpriteScale = 1f;
        [SerializeField] private float enemySpriteScale = 0.8f;
        [SerializeField] private float playerSpriteYOffset = -40f;
        [SerializeField] private float enemySpriteYOffset = -20f;
        [SerializeField] private Color blockOverlayColor = new Color(0.2f, 0.6f, 1f, 0.35f);

        private Canvas _canvas;
        private RectTransform _handContainer;
        private Image _playerAvatarImage;
        private Image _enemyAvatarImage;
        [Header("Hero Animation")]
        [SerializeField] private List<Sprite> heroIdleFrames = new List<Sprite>();
        [SerializeField] private List<Sprite> heroAttackFrames = new List<Sprite>();
        [SerializeField] private float heroAttackFps = 16f;
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
        // Componentes extraídos que manejan áreas específicas de la UI de combate.
        private CombatBackgroundView _backgroundView;
        private CombatFeedbackView _feedbackView;
        private CardHandView _cardHandView;
        private CombatHudView _hudView;

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

            _hudView?.Sync();
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

            // BackgroundView se crea aquí (no en InitializeExtractedViews) para que
            // sus capas existan antes que el resto de la UI y queden al fondo del z-order.
            _backgroundView = gameObject.AddComponent<CombatBackgroundView>();
            _backgroundView.Initialize(turnManager);
            _backgroundView.TryCreateLayers(canvasRect);

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
            InitializeEnemyAnimator();

            RectTransform energyPanel = CreatePanel(
                "EnergyPanel",
                bottomBar,
                new Vector2(0.02f, 0.52f),
                new Vector2(0.17f, 0.95f),
                HudPanelColor);
            Image energyPanelImage = energyPanel.GetComponent<Image>();
            Text playerEnergyText = CreateText("PlayerEnergy", energyPanel, "Energy 0/0", 26, TextAnchor.UpperCenter);
            Text styleChargesText = CreateText("StyleCharges", energyPanel, "Estilo: 0/5", 18, TextAnchor.LowerCenter);

            RectTransform worldPanel = CreatePanel(
                "WorldPanel",
                topBar,
                new Vector2(0.38f, 0.1f),
                new Vector2(0.62f, 0.9f),
                HudPanelColor);
            Image worldPanelImage = worldPanel.GetComponent<Image>();
            Text worldLabel = CreateText("WorldLabel", worldPanel, "World: A", 24, TextAnchor.UpperCenter);
            Text playerTypeText = CreateText("PlayerType", worldPanel, "Tipo: —", 16, TextAnchor.MiddleCenter);
            Text worldSwitchesText = CreateText("WorldSwitches", worldPanel, "Switches: 0/0", 18, TextAnchor.LowerCenter);

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


            Text playerHpLabel = CreateText("PlayerHpLabel", _playerAvatarImage.rectTransform, "0/0", 24, TextAnchor.LowerCenter);
            RectTransform playerHpRect = playerHpLabel.GetComponent<RectTransform>();
            playerHpRect.anchorMin = new Vector2(0.1f, -0.15f);
            playerHpRect.anchorMax = new Vector2(0.9f, -0.02f);
            playerHpRect.offsetMin = Vector2.zero;
            playerHpRect.offsetMax = Vector2.zero;
            CreateBlockUI(_playerAvatarImage.rectTransform, "Player", out Image playerBlockOverlay, out Text playerBlockText);

            Text enemyHpLabel = CreateText("EnemyHpLabel", _enemyAvatarImage.rectTransform, "0/0", 24, TextAnchor.LowerCenter);
            RectTransform enemyHpRect = enemyHpLabel.GetComponent<RectTransform>();
            enemyHpRect.anchorMin = new Vector2(0.1f, -0.15f);
            enemyHpRect.anchorMax = new Vector2(0.9f, -0.02f);
            enemyHpRect.offsetMin = Vector2.zero;
            enemyHpRect.offsetMax = Vector2.zero;
            Text enemyTypeLabel = CreateText("EnemyTypeLabel", _enemyAvatarImage.rectTransform, "Type: —", 18, TextAnchor.UpperCenter);
            RectTransform enemyTypeRect = enemyTypeLabel.GetComponent<RectTransform>();
            enemyTypeRect.anchorMin = new Vector2(0.05f, 0.76f);
            enemyTypeRect.anchorMax = new Vector2(0.95f, 0.88f);
            enemyTypeRect.offsetMin = Vector2.zero;
            enemyTypeRect.offsetMax = Vector2.zero;
            CreateBlockUI(_enemyAvatarImage.rectTransform, "Enemy", out Image enemyBlockOverlay, out Text enemyBlockText);

            RectTransform intentPanel = CreatePanel(
                "EnemyIntentPanel",
                _enemyAvatarImage.rectTransform,
                new Vector2(0.1f, 1.02f),
                new Vector2(0.9f, 1.2f),
                HudPanelColor);
            Text enemyIntentText = CreateText("EnemyIntent", intentPanel, "?", 24, TextAnchor.MiddleCenter);

            Text hitFeedbackText = CreateText("HitFeedback", battlefield, "", 30, TextAnchor.UpperCenter);
            RectTransform hitRect = hitFeedbackText.GetComponent<RectTransform>();
            hitRect.anchorMin = new Vector2(0.35f, 0.92f);
            hitRect.anchorMax = new Vector2(0.65f, 0.99f);
            hitRect.offsetMin = Vector2.zero;
            hitRect.offsetMax = Vector2.zero;
            hitFeedbackText.gameObject.SetActive(false);

            // Toast para límite de mano: visible y centrado en el battlefield.
            Text handLimitText = CreateText("HandLimitText", battlefield, "", 28, TextAnchor.UpperCenter);
            handLimitText.fontStyle = FontStyle.Bold;
            RectTransform handLimitRect = handLimitText.GetComponent<RectTransform>();
            handLimitRect.anchorMin = new Vector2(0.35f, 0.86f);
            handLimitRect.anchorMax = new Vector2(0.65f, 0.93f);
            handLimitRect.offsetMin = Vector2.zero;
            handLimitRect.offsetMax = Vector2.zero;
            handLimitText.color = Color.white;
            handLimitText.gameObject.SetActive(false);

            Text drawPileText = CreateCornerCounter(bottomBar, new Vector2(0.02f, 0.05f), new Vector2(0.14f, 0.32f), "Draw: 0");
            Text discardPileText = CreateCornerCounter(bottomBar, new Vector2(0.84f, 0.05f), new Vector2(0.14f, 0.32f), "Discard: 0");

            Button endTurnButton = CreateButton(
                "EndTurnButton",
                bottomBar,
                new Vector2(0.82f, 0.52f),
                new Vector2(0.98f, 0.95f),
                "End Turn");
            Text endTurnLabel = endTurnButton.GetComponentInChildren<Text>();
            ApplyButtonStyle(endTurnButton);

            Button changeWorldButton = CreateButton(
                "ChangeWorldButton",
                topBar,
                new Vector2(0.74f, 0.1f),
                new Vector2(0.98f, 0.9f),
                "Change World");
            Text changeWorldLabel = changeWorldButton.GetComponentInChildren<Text>();
            ApplyButtonStyle(changeWorldButton);

            InitializePlayerAnimator();
            InitializeExtractedViews(
                hitFeedbackText, handLimitText,
                energyPanelImage, worldPanelImage,
                playerEnergyText, styleChargesText,
                playerHpLabel, enemyHpLabel, enemyTypeLabel,
                playerBlockText, enemyBlockText,
                drawPileText, discardPileText,
                enemyIntentText,
                worldLabel, playerTypeText, worldSwitchesText,
                endTurnButton, endTurnLabel,
                changeWorldButton, changeWorldLabel,
                playerBlockOverlay, enemyBlockOverlay);
        }

        /// <summary>
        /// Crea e inicializa los componentes extraídos (CombatFeedbackView, CardHandView,
        /// CombatHudView). Les pasa las referencias de UI que necesitan después de BuildUI().
        /// </summary>
        private void InitializeExtractedViews(
            Text hitFeedbackText, Text handLimitText,
            Image energyPanelImage, Image worldPanelImage,
            Text playerEnergyText, Text styleChargesText,
            Text playerHpLabel, Text enemyHpLabel, Text enemyTypeLabel,
            Text playerBlockText, Text enemyBlockText,
            Text drawPileText, Text discardPileText,
            Text enemyIntentText,
            Text worldLabel, Text playerTypeText, Text worldSwitchesText,
            Button endTurnButton, Text endTurnLabel,
            Button changeWorldButton, Text changeWorldLabel,
            Image playerBlockOverlay, Image enemyBlockOverlay)
        {
            _feedbackView = gameObject.AddComponent<CombatFeedbackView>();
            _feedbackView.Initialize(
                turnManager,
                hitFeedbackText,
                handLimitText,
                playerHpLabel,
                _playerAvatarImage,
                _enemyAvatarImage,
                _enemyAvatarSpriteImage,
                energyPanelImage,
                worldPanelImage,
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
                energyPanelImage,
                uiFont);

            _hudView = gameObject.AddComponent<CombatHudView>();
            _hudView.Initialize(
                turnManager,
                playerEnergyText, styleChargesText,
                playerHpLabel, enemyHpLabel, enemyTypeLabel,
                playerBlockText, enemyBlockText,
                drawPileText, discardPileText,
                enemyIntentText,
                worldLabel, playerTypeText, worldSwitchesText,
                endTurnButton, endTurnLabel,
                changeWorldButton, changeWorldLabel,
                playerBlockOverlay, enemyBlockOverlay,
                _playerAvatarImage, _enemyAvatarImage,
                energyPanelImage, worldPanelImage,
                _feedbackView, _cardHandView,
                blockOverlayColor);
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

    }
}


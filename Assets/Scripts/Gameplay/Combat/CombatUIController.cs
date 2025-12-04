using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Gameplay.Combat
{
    /// <summary>
    /// Runtime UI that builds a simple battle HUD, placeholders, and card buttons.
    /// </summary>
    public class CombatUIController : MonoBehaviour
    {
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private Font uiFont;

        private Canvas _canvas;
        private Text _playerEnergyText;
        private Text _playerHpLabel;
        private Text _enemyHpLabel;
        private Text _drawPileText;
        private Text _discardPileText;
        private Text _enemyIntentText;
        private Text _worldLabel;
        private Button _endTurnButton;
        private Button _changeWorldButton;
        private RectTransform _handContainer;
        private Image _playerAvatarImage;
        private Image _enemyAvatarImage;
        private Color _playerAvatarBaseColor;
        private Color _enemyAvatarBaseColor;
        private Image _skyBackgroundImage;
        private Image _groundBackgroundImage;
        [SerializeField] private Color worldASkyColor = new Color(0.07f, 0.12f, 0.3f, 1f);
        [SerializeField] private Color worldAGroundColor = new Color(0.02f, 0.07f, 0.09f, 1f);
        [SerializeField] private Color worldBSkyColor = new Color(0.2f, 0.05f, 0.05f, 1f);
        [SerializeField] private Color worldBGroundColor = new Color(0.09f, 0.02f, 0.04f, 1f);

        private readonly List<CardButtonBinding> _cardButtons = new List<CardButtonBinding>();
        private readonly List<CardDefinition> _handCache = new List<CardDefinition>();

        private class CardButtonBinding
        {
            public CardDefinition Card;
            public Button Button;
            public Text Label;
        }

        private void Awake()
        {
            if (turnManager == null)
            {
                turnManager = GetComponent<TurnManager>();
            }

            if (uiFont == null)
            {
                // Unity 6 reemplazó Arial por LegacyRuntime como built-in
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            EnsureEventSystem();
            BuildUI();
        }

        private void Update()
        {
            if (turnManager == null)
            {
                return;
            }

            UpdateInfoTexts();
            SyncHandButtons();
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemGO = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystemGO);
        }

        private void BuildUI()
        {
            _canvas = CreateCanvas("CombatCanvas");
            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();

            CreateBackgroundLayers(canvasRect);

            RectTransform battlefield = CreatePanel(
                "BattlefieldPanel",
                canvasRect,
                new Vector2(0.05f, 0.35f),
                new Vector2(0.95f, 0.9f),
                new Color(0.04f, 0.07f, 0.11f, 0.6f));

            _playerAvatarImage = CreateAvatar(
                "PlayerAvatar",
                battlefield,
                new Vector2(0.05f, 0.1f),
                new Vector2(0.3f, 0.9f),
                new Color(0.2f, 0.55f, 0.9f),
                "Pilot");
            _playerAvatarBaseColor = _playerAvatarImage.color;

            _enemyAvatarImage = CreateAvatar(
                "EnemyAvatar",
                battlefield,
                new Vector2(0.7f, 0.1f),
                new Vector2(0.95f, 0.9f),
                new Color(0.95f, 0.4f, 0.4f),
                "Slime");
            _enemyAvatarBaseColor = _enemyAvatarImage.color;

            RectTransform energyPanel = CreatePanel(
                "EnergyPanel",
                canvasRect,
                new Vector2(0.02f, 0.82f),
                new Vector2(0.17f, 0.95f),
                new Color(0.03f, 0.03f, 0.05f, 0.75f));
            _playerEnergyText = CreateText("PlayerEnergy", energyPanel, "Energy 0/0", 26, TextAnchor.MiddleCenter);

            RectTransform worldPanel = CreatePanel(
                "WorldPanel",
                canvasRect,
                new Vector2(0.42f, 0.82f),
                new Vector2(0.58f, 0.95f),
                new Color(0.03f, 0.03f, 0.05f, 0.75f));
            _worldLabel = CreateText("WorldLabel", worldPanel, "World: A", 26, TextAnchor.MiddleCenter);

            RectTransform handPanel = CreatePanel(
                "HandPanel",
                canvasRect,
                new Vector2(0.05f, 0.05f),
                new Vector2(0.95f, 0.25f),
                new Color(0.08f, 0.08f, 0.12f, 0.8f));
            HorizontalLayoutGroup layout = handPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12f;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            _handContainer = handPanel;

            _playerHpLabel = CreateText("PlayerHpLabel", _playerAvatarImage.rectTransform, "0/0", 24, TextAnchor.LowerCenter);
            RectTransform playerHpRect = _playerHpLabel.GetComponent<RectTransform>();
            playerHpRect.anchorMin = new Vector2(0.1f, -0.15f);
            playerHpRect.anchorMax = new Vector2(0.9f, -0.02f);
            playerHpRect.offsetMin = Vector2.zero;
            playerHpRect.offsetMax = Vector2.zero;

            _enemyHpLabel = CreateText("EnemyHpLabel", _enemyAvatarImage.rectTransform, "0/0", 24, TextAnchor.LowerCenter);
            RectTransform enemyHpRect = _enemyHpLabel.GetComponent<RectTransform>();
            enemyHpRect.anchorMin = new Vector2(0.1f, -0.15f);
            enemyHpRect.anchorMax = new Vector2(0.9f, -0.02f);
            enemyHpRect.offsetMin = Vector2.zero;
            enemyHpRect.offsetMax = Vector2.zero;

            _enemyIntentText = CreateText("EnemyIntent", _enemyAvatarImage.rectTransform, "?", 26, TextAnchor.LowerCenter);
            RectTransform intentRect = _enemyIntentText.GetComponent<RectTransform>();
            intentRect.anchorMin = new Vector2(0.1f, 1.02f);
            intentRect.anchorMax = new Vector2(0.9f, 1.18f);
            intentRect.offsetMin = Vector2.zero;
            intentRect.offsetMax = Vector2.zero;

            _drawPileText = CreateCornerCounter(canvasRect, new Vector2(0.02f, 0.02f), "Draw: 0");
            _discardPileText = CreateCornerCounter(canvasRect, new Vector2(0.82f, 0.02f), "Discard: 0");

            _endTurnButton = CreateButton(
                "EndTurnButton",
                canvasRect,
                new Vector2(0.83f, 0.88f),
                new Vector2(0.97f, 0.97f),
                "End Turn");
            _endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);

            _changeWorldButton = CreateButton(
                "ChangeWorldButton",
                canvasRect,
                new Vector2(0.64f, 0.88f),
                new Vector2(0.8f, 0.97f),
                "Change World");
            _changeWorldButton.onClick.AddListener(OnChangeWorldButtonClicked);
        }
        private Text CreateCornerCounter(RectTransform parent, Vector2 anchorMin, string label)
        {
            RectTransform panel = CreatePanel(
                label + "_Panel",
                parent,
                anchorMin,
                anchorMin + new Vector2(0.15f, 0.09f),
                new Color(0.03f, 0.03f, 0.05f, 0.65f));
            return CreateText(label + "_Text", panel, label, 22, TextAnchor.MiddleCenter);
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
                new Vector2(0, 0.45f),
                new Vector2(1, 1),
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

        private Image CreateAvatar(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Color tint, string label)
        {
            RectTransform rect = CreatePanel(name, parent, anchorMin, anchorMax, tint);
            Image image = rect.GetComponent<Image>();
            image.color = tint;

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
            image.color = new Color(0.25f, 0.25f, 0.35f, 0.95f);

            Button button = buttonGO.GetComponent<Button>();

            Text labelText = CreateText("Label", rect, label, 28, TextAnchor.MiddleCenter);
            labelText.fontStyle = FontStyle.Bold;

            return button;
        }

        private Button CreateCardButton(CardDefinition card, Transform parent)
        {
            GameObject buttonGO = new GameObject(card.name + "_Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGO.transform.SetParent(parent, false);

            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(230, 130);

            Image image = buttonGO.GetComponent<Image>();
            image.color = new Color(0.18f, 0.18f, 0.25f, 0.95f);

            LayoutElement layout = buttonGO.GetComponent<LayoutElement>();
            layout.preferredWidth = 230;
            layout.preferredHeight = 130;

            Button button = buttonGO.GetComponent<Button>();

            Text labelText = CreateText("Label", rect, BuildCardLabel(card), 20, TextAnchor.MiddleCenter);
            labelText.fontStyle = FontStyle.Bold;

            button.onClick.AddListener(() => OnCardButtonClicked(card));

            return button;
        }

        private void OnEndTurnButtonClicked()
        {
            if (turnManager == null || !turnManager.IsPlayerTurn())
            {
                return;
            }

            turnManager.EndPlayerTurn();
        }

        private void OnCardButtonClicked(CardDefinition card)
        {
            if (turnManager == null || card == null)
            {
                return;
            }

            bool played = turnManager.PlayCard(card);
            if (!played)
            {
                return;
            }

            SyncHandButtons(forceRebuild: true);
        }

        private void UpdateInfoTexts()
        {
            _playerEnergyText.text = $"Energy {turnManager.PlayerEnergy}/{turnManager.PlayerMaxEnergy}";
            _playerHpLabel.text = $"{turnManager.PlayerHP}/{turnManager.PlayerMaxHP}";
            _enemyHpLabel.text = $"{turnManager.EnemyHP}/{turnManager.EnemyMaxHP}";
            _drawPileText.text = $"Draw: {turnManager.PlayerDrawPileCount}";
            _discardPileText.text = $"Discard: {turnManager.PlayerDiscardPileCount}";
            _enemyIntentText.text = BuildEnemyIntentLabel();
            UpdateWorldVisuals();

            bool playerTurn = turnManager.IsPlayerTurn();
            _endTurnButton.interactable = playerTurn && !turnManager.IsCombatFinished;
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

        private void SyncHandButtons(bool forceRebuild = false)
        {
            if (forceRebuild || !IsHandCacheValid())
            {
                RebuildHandButtons();
            }

            bool canInteract = turnManager.IsPlayerTurn();
            int currentEnergy = turnManager.PlayerEnergy;
            foreach (CardButtonBinding binding in _cardButtons)
            {
                bool enoughEnergy = binding.Card != null && currentEnergy >= binding.Card.Cost;
                binding.Button.interactable = canInteract && enoughEnergy;
                binding.Label.text = BuildCardLabel(binding.Card);
            }
        }

        private bool IsHandCacheValid()
        {
            IReadOnlyList<CardDefinition> hand = turnManager.PlayerHand;
            if (hand.Count != _handCache.Count)
            {
                return false;
            }

            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i] != _handCache[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void RebuildHandButtons()
        {
            foreach (CardButtonBinding binding in _cardButtons)
            {
                if (binding?.Button != null)
                {
                    Destroy(binding.Button.gameObject);
                }
            }

            _cardButtons.Clear();
            _handCache.Clear();

            IReadOnlyList<CardDefinition> hand = turnManager.PlayerHand;
            foreach (CardDefinition card in hand)
            {
                Button button = CreateCardButton(card, _handContainer);
                Text label = button.GetComponentInChildren<Text>();
                _cardButtons.Add(new CardButtonBinding
                {
                    Card = card,
                    Button = button,
                    Label = label
                });
                _handCache.Add(card);
            }
        }

        private string BuildCardLabel(CardDefinition card)
        {
            if (card == null)
            {
                return "Unknown Card";
            }

            return $"{card.CardName} (Cost {card.Cost})\n{card.Description}";
        }

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

        private void OnChangeWorldButtonClicked()
        {
            if (turnManager == null)
            {
                return;
            }

            turnManager.ToggleWorldForDebug();
            UpdateWorldVisuals();
        }

        private void UpdateWorldVisuals()
        {
            if (turnManager == null || _worldLabel == null)
            {
                return;
            }

            string worldLabel = turnManager.CurrentWorld == TurnManager.WorldSide.A ? "A" : "B";
            _worldLabel.text = $"World: {worldLabel}";

            Color targetSky = turnManager.CurrentWorld == TurnManager.WorldSide.A ? worldASkyColor : worldBSkyColor;
            Color targetGround = turnManager.CurrentWorld == TurnManager.WorldSide.A ? worldAGroundColor : worldBGroundColor;

            if (_skyBackgroundImage != null)
            {
                _skyBackgroundImage.color = targetSky;
            }

            if (_groundBackgroundImage != null)
            {
                _groundBackgroundImage.color = targetGround;
            }
        }
    }
}


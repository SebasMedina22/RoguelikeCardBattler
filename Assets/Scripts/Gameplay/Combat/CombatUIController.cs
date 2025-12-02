using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RoguelikeCardBattler.Gameplay.Cards;

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
        private Text _playerInfoText;
        private Text _enemyInfoText;
        private Text _deckInfoText;
        private Button _endTurnButton;
        private RectTransform _handContainer;
        private Image _playerAvatarImage;
        private Image _enemyAvatarImage;
        private Color _playerAvatarBaseColor;
        private Color _enemyAvatarBaseColor;

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

            RectTransform infoPanel = CreatePanel(
                "InfoPanel",
                canvasRect,
                new Vector2(0.02f, 0.73f),
                new Vector2(0.35f, 0.97f),
                new Color(0.03f, 0.03f, 0.05f, 0.75f));
            VerticalLayoutGroup infoLayout = infoPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.childAlignment = TextAnchor.UpperLeft;
            infoLayout.spacing = 8f;
            infoLayout.padding = new RectOffset(12, 12, 12, 12);

            _playerInfoText = CreateText("PlayerInfo", infoPanel, "Player\nHP: 0/0\nEnergy: 0/0", 28);
            _enemyInfoText = CreateText("EnemyInfo", infoPanel, "Enemy\nHP: 0/0", 28);
            _deckInfoText = CreateText("DeckInfo", infoPanel, "Draw: 0  Discard: 0  Hand: 0", 24);

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

            _endTurnButton = CreateButton(
                "EndTurnButton",
                canvasRect,
                new Vector2(0.78f, 0.73f),
                new Vector2(0.97f, 0.9f),
                "END TURN");
            _endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
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
                new Color(0.07f, 0.12f, 0.3f, 1f));
            sky.SetAsFirstSibling();

            RectTransform ground = CreatePanel(
                "BackgroundGround",
                canvasRect,
                new Vector2(0, 0),
                new Vector2(1, 0.45f),
                new Color(0.02f, 0.07f, 0.09f, 1f));
            ground.SetSiblingIndex(1);
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
            _playerInfoText.text =
                $"PLAYER\nHP: {turnManager.PlayerHP}/{turnManager.PlayerMaxHP}\nEnergy: {turnManager.PlayerEnergy}/{turnManager.PlayerMaxEnergy}";

            _enemyInfoText.text =
                $"ENEMY\nHP: {turnManager.EnemyHP}/{turnManager.EnemyMaxHP}";

            _deckInfoText.text =
                $"Draw: {turnManager.PlayerDrawPileCount}  Discard: {turnManager.PlayerDiscardPileCount}  Hand: {turnManager.PlayerHandCount}";

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
    }
}


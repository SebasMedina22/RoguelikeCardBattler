using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Run.Shop
{
    /// <summary>
    /// Controlador del nodo Tienda. RunFlowController lo crea como hijo en
    /// runtime (no requiere setup manual). Construye un panel sobre el canvas
    /// del run con parallax 2D de 3 capas (pared / estanterías / mostrador) +
    /// el stock (cartas, Retazos, servicio "eliminar carta") + extras añadidos
    /// por Retazos vía OnShopStockBuilt. Espejo casi exacto de
    /// <c>CampfireNodeController</c>.
    ///
    /// La lógica de stock y compra vive en helpers static puros (BuildStock,
    /// TryPurchase) para que los tests EditMode la validen sin instanciar UI.
    /// </summary>
    public class ShopNodeController : MonoBehaviour
    {
        // Colores de fallback cuando los sprites del config son null. Permiten
        // probar la feature antes de tener arte: un fondo plano en cada capa.
        private static readonly Color FallbackWall = new Color(0.16f, 0.10f, 0.06f, 1f);    // #29190F
        private static readonly Color FallbackShelf = new Color(0.30f, 0.20f, 0.10f, 1f);   // #4D331A
        private static readonly Color FallbackCounter = new Color(0.45f, 0.30f, 0.15f, 1f); // #734D26

        private Canvas _canvas;
        private RunState _state;
        private RelicHookDispatcher _dispatcher;
        private ShopConfig _config;
        private Action<int> _onComplete;
        private Font _uiFont;

        private RectTransform _root;
        private RectTransform _stockPanel;
        private RectTransform _cardSelectPanel;
        private Image _wallLayer;
        private Image _shelfLayer;
        private Image _counterLayer;
        private Text _goldText;
        private readonly List<GameObject> _spawnedButtons = new List<GameObject>();
        private List<ShopItem> _stock = new List<ShopItem>();

        private int _activeNodeId = -1;
        private static Sprite _whiteSprite;

        public void Initialize(
            Canvas canvas,
            RunState state,
            RelicHookDispatcher dispatcher,
            ShopConfig config,
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

            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _cardSelectPanel.gameObject.SetActive(false);
            _stockPanel.gameObject.SetActive(true);

            // Stock determinista por nodo: misma tienda → mismo stock. Como el
            // nodo queda Completed al salir, no se re-entra; el seed por nodeId
            // sólo garantiza estabilidad si se redibuja en la misma visita.
            _stock = BuildStock(_config, _state, nodeId);

            // Los Retazos pueden agregar/modificar ítems del stock antes de dibujar.
            // TurnManager null porque estamos fuera de combate.
            if (_dispatcher != null)
            {
                _dispatcher.Dispatch(
                    RelicHook.OnShopStockBuilt,
                    new ShopStockBuiltHookData(_state, _dispatcher, _stock));
            }

            RefreshGoldText();
            DrawStock();
            StartParallaxAnimations();
        }

        // ──────────────────────────────────────────────
        // Helpers static puros (testeables sin UI)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Arma el stock base de la tienda de forma determinista por <paramref name="seed"/>:
        /// N cartas (filtradas ESTRICTO por tipo del jugador / None) + M Retazos
        /// (excluyendo los ya poseídos) + 1 servicio "eliminar carta" (precio
        /// escalante por tiendas completadas). No dispara el hook: eso lo hace el
        /// controller tras llamar aquí.
        /// </summary>
        public static List<ShopItem> BuildStock(ShopConfig config, RunState state, int seed)
        {
            List<ShopItem> stock = new List<ShopItem>();
            if (config == null || state == null) return stock;

            System.Random rng = new System.Random(seed);

            // --- Cartas ---
            List<CardDeckEntry> candidateCards = new List<CardDeckEntry>();
            foreach (CardDeckEntry entry in config.CardPool)
            {
                if (entry != null && entry.IsValid && EntryMatchesPlayerTypes(entry, state))
                {
                    candidateCards.Add(entry);
                }
            }
            Shuffle(candidateCards, rng);
            int cardCount = Mathf.Min(config.CardSlots, candidateCards.Count);
            for (int i = 0; i < cardCount; i++)
            {
                CardDeckEntry entry = candidateCards[i];
                CardDefinition rep = RepresentativeCard(entry);
                int price = config.PriceForRarity(rep != null ? rep.Rarity : CardRarity.Common);
                string title = rep != null ? rep.CardName : "Carta";
                stock.Add(new ShopItem(
                    ShopItemKind.Card,
                    title,
                    rep != null ? $"{rep.Rarity}" : string.Empty,
                    price,
                    cardPayload: entry));
            }

            // --- Retazos (excluyendo los ya poseídos) ---
            HashSet<RelicDefinition> owned = new HashSet<RelicDefinition>();
            foreach (RelicInstance inst in state.Relics)
            {
                if (inst != null && inst.Definition != null) owned.Add(inst.Definition);
            }
            List<RelicDefinition> candidateRelics = new List<RelicDefinition>();
            foreach (RelicDefinition relic in config.RelicPool)
            {
                if (relic != null && !owned.Contains(relic)) candidateRelics.Add(relic);
            }
            Shuffle(candidateRelics, rng);
            int relicCount = Mathf.Min(config.RelicSlots, candidateRelics.Count);
            for (int i = 0; i < relicCount; i++)
            {
                RelicDefinition relic = candidateRelics[i];
                string title = string.IsNullOrEmpty(relic.DisplayName) ? relic.name : relic.DisplayName;
                stock.Add(new ShopItem(
                    ShopItemKind.Relic,
                    title,
                    relic.Description,
                    config.RelicPrice,
                    relicPayload: relic));
            }

            // --- Servicio: eliminar carta (precio escalante) ---
            int removePrice = config.RemoveCardPriceFor(state.ShopsCompleted);
            stock.Add(new ShopItem(
                ShopItemKind.RemoveCard,
                "Eliminar carta",
                "Quita una carta de tu mazo",
                removePrice));

            return stock;
        }

        /// <summary>
        /// Intenta comprar un ítem. Guards: ítem nulo / ya comprado / oro
        /// insuficiente → devuelve false SIN mutar estado. En éxito descuenta el
        /// oro exacto, aplica el efecto según Kind y marca Purchased.
        /// Para <see cref="ShopItemKind.RemoveCard"/> se requiere
        /// <paramref name="removalTarget"/>: si no está en el mazo, no compra.
        /// </summary>
        public static bool TryPurchase(RunState state, ShopItem item, CardDeckEntry removalTarget = null)
        {
            if (state == null || item == null) return false;
            if (item.Purchased) return false;
            if (state.Gold < item.Price) return false;

            switch (item.Kind)
            {
                case ShopItemKind.Card:
                    if (item.CardPayload == null || !item.CardPayload.IsValid) return false;
                    state.AddCardToDeck(item.CardPayload); // clona internamente
                    break;
                case ShopItemKind.Relic:
                    if (item.RelicPayload == null) return false;
                    state.AddRelic(item.RelicPayload);
                    break;
                case ShopItemKind.RemoveCard:
                    if (removalTarget == null) return false;
                    if (!state.RemoveCardFromDeck(removalTarget)) return false;
                    break;
                default:
                    return false;
            }

            state.Gold -= item.Price;
            item.Purchased = true;
            item.OnPurchase?.Invoke();
            return true;
        }

        /// <summary>
        /// Filtrado ESTRICTO por tipo (decisión cerrada 3D): la entrada pasa sólo
        /// si todos sus lados de carta tienen un ElementType ∈ {tipo mundo A del
        /// jugador, tipo mundo B, None}. El factor de sinergia mazo↔stock está
        /// diferido post-3D (Insight 7).
        /// </summary>
        private static bool EntryMatchesPlayerTypes(CardDeckEntry entry, RunState state)
        {
            if (entry.DualCard != null)
            {
                return TypeAllowed(SideType(entry.DualCard.SideA), state)
                    && TypeAllowed(SideType(entry.DualCard.SideB), state);
            }
            return TypeAllowed(entry.SingleCard != null ? entry.SingleCard.ElementType : ElementType.None, state);
        }

        private static ElementType SideType(CardDefinition card) =>
            card != null ? card.ElementType : ElementType.None;

        private static bool TypeAllowed(ElementType type, RunState state) =>
            type == ElementType.None
            || type == state.PlayerWorldAType
            || type == state.PlayerWorldBType;

        private static CardDefinition RepresentativeCard(CardDeckEntry entry)
        {
            if (entry == null) return null;
            if (entry.SingleCard != null) return entry.SingleCard;
            return entry.DualCard != null ? entry.DualCard.SideA : null;
        }

        // Fisher-Yates con RNG inyectado: misma seed → mismo orden (determinismo).
        private static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ──────────────────────────────────────────────
        // Build (UI)
        // ──────────────────────────────────────────────

        private void BuildPanel()
        {
            GameObject rootGo = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
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

            // Parallax de 3 capas con profundidad (la pared al fondo, las
            // estanterías en la franja superior, el mostrador en primer plano
            // abajo). Wall se estira a pantalla completa (es la atmósfera de
            // fondo); Shelf y Counter usan preserveAspect para no deformar los
            // productos ni el mostrador (sus PNG tienen viñeta marrón que se
            // funde con el fondo cartón).
            _wallLayer = CreateLayer("WallLayer", _root, _config != null ? _config.WallSprite : null, FallbackWall,
                new Vector2(0f, 0f), new Vector2(1f, 1f), preserveAspect: false);
            _shelfLayer = CreateLayer("ShelfLayer", _root, _config != null ? _config.ShelfSprite : null, FallbackShelf,
                new Vector2(0.06f, 0.50f), new Vector2(0.94f, 1.0f), preserveAspect: true);
            _counterLayer = CreateLayer("CounterLayer", _root, _config != null ? _config.CounterSprite : null, FallbackCounter,
                new Vector2(0f, 0f), new Vector2(1f, 0.40f), preserveAspect: true);

            // Título
            Text title = CreateText("Title", _root, "~ Tienda ~", 40, TextAnchor.UpperCenter);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.85f);
            titleRect.anchorMax = new Vector2(0.9f, 0.95f);

            // Oro (debajo del título)
            _goldText = CreateText("GoldText", _root, "Oro: --", 24, TextAnchor.UpperCenter);
            RectTransform goldRect = _goldText.GetComponent<RectTransform>();
            goldRect.anchorMin = new Vector2(0.1f, 0.78f);
            goldRect.anchorMax = new Vector2(0.9f, 0.84f);

            // Panel del stock
            _stockPanel = CreateSubPanel("StockPanel", _root);
            _stockPanel.anchorMin = new Vector2(0.15f, 0.08f);
            _stockPanel.anchorMax = new Vector2(0.85f, 0.76f);

            // Panel selector de carta (para el servicio eliminar)
            _cardSelectPanel = CreateSubPanel("CardSelectPanel", _root);
            _cardSelectPanel.anchorMin = new Vector2(0.15f, 0.08f);
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
        // Stock flow
        // ──────────────────────────────────────────────

        private void DrawStock()
        {
            ClearSpawnedButtons();

            // "Salir" vive en una franja inferior fija del panel; los ítems se
            // reparten en el resto. El alto de cada ítem es adaptativo a la
            // cantidad de stock, así SIEMPRE entran todos (3 cartas + 2 Retazos +
            // 1 eliminar = 6, o más si un Retazo agrega ítems vía hook) — sin el
            // viejo clamp que recortaba el último botón ("Eliminar carta").
            const float exitBandTop = 0.09f; // franja inferior reservada a "Salir"
            const float itemsTop = 1.0f;
            const float gap = 0.015f;

            int count = _stock.Count;
            float region = itemsTop - exitBandTop;
            float slot = count > 0 ? region / count : region;
            float buttonHeight = Mathf.Max(0.05f, slot - gap);

            for (int i = 0; i < count; i++)
            {
                ShopItem item = _stock[i];
                float top = itemsTop - i * slot;
                float bottom = top - buttonHeight;
                CreateItemButton(_stockPanel, item, bottom, top);
            }

            // Botón "Salir" — anclado a la franja inferior, fuera del reparto de ítems.
            Button exit = CreateButtonRaw(_stockPanel, "ExitButton", "Salir", 0.3f, 0.0f, 0.7f, exitBandTop - 0.01f, true);
            exit.onClick.AddListener(CompleteShop);
            _spawnedButtons.Add(exit.gameObject);
        }

        private void CreateItemButton(RectTransform parent, ShopItem item, float yMin, float yMax)
        {
            bool affordable = !item.Purchased && _state.Gold >= item.Price;
            string label = item.Purchased
                ? $"{item.Title}  (comprado)"
                : $"{item.Title} — {item.Price} oro\n<{item.Description}>";
            Button btn = CreateButtonRaw(parent, $"Item_{item.Title}", label, 0.05f, yMin, 0.95f, yMax, affordable);
            if (affordable)
            {
                btn.onClick.AddListener(() => OnItemClicked(item));
            }
            _spawnedButtons.Add(btn.gameObject);
        }

        private void OnItemClicked(ShopItem item)
        {
            if (item.Kind == ShopItemKind.RemoveCard)
            {
                ShowCardSelectPanel(item);
                return;
            }

            if (TryPurchase(_state, item))
            {
                RefreshGoldText();
                DrawStock();
            }
        }

        private void ShowCardSelectPanel(ShopItem removeItem)
        {
            _stockPanel.gameObject.SetActive(false);
            _cardSelectPanel.gameObject.SetActive(true);
            ClearSpawnedButtons();

            Text header = CreateText("CardSelectHeader", _cardSelectPanel, "Elige una carta a eliminar", 26, TextAnchor.UpperCenter);
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.9f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            _spawnedButtons.Add(header.gameObject);

            // Snapshot: cualquier carta del mazo puede eliminarse.
            List<CardDeckEntry> deck = _state.GetDeckSnapshot();

            int cols = 2;
            float topY = 0.88f;
            float bottomY = 0.1f;
            float rowHeight = 0.1f;
            float colWidth = 0.48f;

            for (int i = 0; i < deck.Count; i++)
            {
                CardDeckEntry entry = deck[i];
                int col = i % cols;
                int row = i / cols;
                float xMin = 0.01f + col * (colWidth + 0.02f);
                float xMax = xMin + colWidth;
                float yMaxButton = topY - row * (rowHeight + 0.01f);
                float yMinButton = yMaxButton - rowHeight;
                if (yMinButton < bottomY) break; // overflow: mazos largos serían pageables, hoy clamp.

                string label = BuildCardSelectLabel(entry);
                Button btn = CreateButtonRaw(_cardSelectPanel, $"Card_{i}", label, xMin, yMinButton, xMax, yMaxButton, true);
                CardDeckEntry target = entry;
                btn.onClick.AddListener(() =>
                {
                    if (TryPurchase(_state, removeItem, target))
                    {
                        BackToStock();
                    }
                });
                _spawnedButtons.Add(btn.gameObject);
            }

            // Botón "Volver"
            Button back = CreateButtonRaw(_cardSelectPanel, "BackButton", "Volver", 0.35f, 0.0f, 0.65f, 0.07f, true);
            back.onClick.AddListener(BackToStock);
            _spawnedButtons.Add(back.gameObject);
        }

        private void BackToStock()
        {
            _cardSelectPanel.gameObject.SetActive(false);
            _stockPanel.gameObject.SetActive(true);
            RefreshGoldText();
            DrawStock();
        }

        private void CompleteShop()
        {
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
        /// Etiqueta para el botón de selección de carta. Para duales muestra ambos
        /// lados (A / B); para singles el nombre. Espejo de CampfireNodeController.
        /// </summary>
        private static string BuildCardSelectLabel(CardDeckEntry entry)
        {
            if (entry == null) return "Carta";
            if (entry.DualCard != null)
            {
                CardDefinition a = entry.DualCard.SideA;
                CardDefinition b = entry.DualCard.SideB;
                string nameA = a != null ? a.CardName : "?";
                string nameB = b != null ? b.CardName : "?";
                return nameA == nameB ? nameA : $"{nameA} / {nameB}";
            }
            return entry.SingleCard != null ? entry.SingleCard.CardName : "Carta";
        }

        private void RefreshGoldText()
        {
            if (_goldText != null) _goldText.text = $"Oro: {_state.Gold}";
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
                ? new Color(0.12f, 0.10f, 0.08f, 0.92f)
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
            if (_shelfLayer != null)
            {
                RectTransform shelfRect = _shelfLayer.rectTransform;
                Vector2 baseAnchor = shelfRect.anchoredPosition;
                DOTween.Kill(shelfRect);
                DOTween.To(
                        () => shelfRect.anchoredPosition,
                        v => shelfRect.anchoredPosition = v,
                        baseAnchor + new Vector2(10f, 0f),
                        2.5f)
                    .SetTarget(shelfRect)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }

            if (_counterLayer != null)
            {
                Transform counterT = _counterLayer.transform;
                DOTween.Kill(counterT);
                counterT.localScale = Vector3.one;
                DOTween.To(
                        () => counterT.localScale,
                        v => counterT.localScale = v,
                        new Vector3(1.02f, 1.02f, 1f),
                        1.2f)
                    .SetTarget(counterT)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }
        }

        private void StopParallaxAnimations()
        {
            if (_shelfLayer != null) DOTween.Kill(_shelfLayer.rectTransform);
            if (_counterLayer != null)
            {
                DOTween.Kill(_counterLayer.transform);
                _counterLayer.transform.localScale = Vector3.one;
            }
        }

        private void OnDestroy()
        {
            StopParallaxAnimations();
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RoguelikeCardBattler.Core.UI;
using RoguelikeCardBattler.Gameplay.Combat;

namespace RoguelikeCardBattler.Gameplay.Cards.UI
{
    /// <summary>
    /// Vista de solo lectura del mazo completo del run. Clase de presentación pura
    /// (no MonoBehaviour) — molde de RelicInventoryView. Crea un sub-Canvas overlay
    /// propio (sortingOrder=100) que flota sobre cualquier panel opaco (Tienda,
    /// Hoguera) en RunScene y sobre el HUD de combate en BattleScene.
    /// Montada por RunFlowController (RunScene) y CombatUIController (BattleScene).
    /// Helpers static puros (SortForDisplay/BuildRowLabel/BuildTooltip/BuildUpgradePreview)
    /// son testeables sin UI, siguiendo el patrón de ShopNodeController.BuildStock.
    /// </summary>
    public class DeckViewerView
    {
        private static readonly Color DimmerColor      = new Color(0f,    0f,    0f,    0.78f);
        private static readonly Color PanelBg          = new Color(0.07f, 0.07f, 0.11f, 0.97f);
        private static readonly Color BadgeBg          = new Color(0.12f, 0.12f, 0.20f, 0.92f);
        private static readonly Color RowEven          = new Color(0.10f, 0.10f, 0.15f, 0.88f);
        private static readonly Color RowOdd           = new Color(0.07f, 0.07f, 0.11f, 0.88f);
        private static readonly Color TooltipBg        = new Color(0.04f, 0.04f, 0.06f, 0.97f);
        private static readonly Color TextColor        = new Color(1f,    0.95f, 0.85f, 1f);

        private readonly Font  _font;
        private readonly Canvas _subCanvas;

        private Button _badgeButton;
        private Text   _badgeLabel;

        private RectTransform      _modalOverlay;
        private Text               _modalTitle;
        private RectTransform      _scrollContent;
        private readonly List<GameObject> _rowGos = new List<GameObject>();

        private GameObject  _tooltipGo;
        private CanvasGroup _tooltipGroup;
        private Text        _tooltipText;
        private RectTransform _tooltipRect;

        private IReadOnlyList<CardDeckEntry> _cachedDeck;
        private bool _isOpen;

        private static Sprite _whiteSprite;

        // ────────────────────────────────────────
        // Construcción
        // ────────────────────────────────────────

        public DeckViewerView(Canvas hostCanvas, Font font)
        {
            _font = font;

            // Sub-Canvas overlay independiente: flota sobre paneles opacos de la
            // escena sin necesitar coordinación con cada controller de nodo.
            GameObject canvasGo = new GameObject("DeckViewerCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(hostCanvas.transform, false);

            RectTransform canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.anchorMin = Vector2.zero;
            canvasRt.anchorMax = Vector2.one;
            canvasRt.offsetMin = Vector2.zero;
            canvasRt.offsetMax = Vector2.zero;

            _subCanvas = canvasGo.GetComponent<Canvas>();
            _subCanvas.overrideSorting = true;
            _subCanvas.sortingOrder   = 100;

            RectTransform subRect = (RectTransform)_subCanvas.transform;
            BuildBadge(subRect);
            BuildModal(subRect);
            BuildTooltipGo(subRect);
        }

        // ────────────────────────────────────────
        // API pública de instancia
        // ────────────────────────────────────────

        /// <summary>
        /// Actualiza el badge y, si el modal está abierto, reconstruye la lista.
        /// Null-safe: deck == null → badge "Mazo (0)", lista vacía, sin excepción.
        /// </summary>
        public void Refresh(IReadOnlyList<CardDeckEntry> deck)
        {
            _cachedDeck = deck;
            UpdateBadgeText();
            if (_isOpen) RebuildList();
        }

        public void Open()
        {
            _isOpen = true;
            if (_modalOverlay != null) _modalOverlay.gameObject.SetActive(true);
            UpdateBadgeText();
            RebuildList();
        }

        public void Close()
        {
            _isOpen = false;
            if (_modalOverlay != null) _modalOverlay.gameObject.SetActive(false);
            HideTooltip();
            UpdateBadgeText();
        }

        public void Toggle()
        {
            if (_isOpen) Close(); else Open();
        }

        /// <summary>
        /// Destruye los GameObjects del overlay (llamado por los owners en transiciones
        /// de escena, espejo de RelicInventoryView.Cleanup).
        /// </summary>
        public void Cleanup()
        {
            if (_subCanvas != null)
                UnityEngine.Object.Destroy(_subCanvas.gameObject);
        }

        // ────────────────────────────────────────
        // Helpers static puros — testeables sin UI
        // ────────────────────────────────────────

        /// <summary>
        /// Devuelve una lista NUEVA ordenada por CardType→coste→nombre→id.
        /// No muta la entrada. Orden estable (LINQ OrderBy/ThenBy).
        /// Filtra entradas inválidas y duales con ambos lados null.
        /// </summary>
        public static List<CardDeckEntry> SortForDisplay(IReadOnlyList<CardDeckEntry> deck)
        {
            if (deck == null) return new List<CardDeckEntry>();

            return deck
                .Where(e => e != null && e.IsValid && RepresentativeCard(e) != null)
                .OrderBy(e =>  (int)RepresentativeCard(e).Type)
                .ThenBy(e =>  RepresentativeCard(e).Cost)
                .ThenBy(e =>  RepresentativeCard(e).CardName)
                .ThenBy(e =>  RepresentativeCard(e).Id)
                .ToList();
        }

        /// <summary>
        /// Línea de fila con rich-text: [Tipo] Nombre · coste⚡ + marcador ★/+.
        /// Duales: ambos lados; colapsa a un token solo si nombre Y tipo coinciden
        /// en ambos lados no-null. Lados null → "?". Nunca lanza NRE.
        /// </summary>
        public static string BuildRowLabel(CardDeckEntry entry)
        {
            if (entry == null || !entry.IsValid) return "";

            string body;
            if (entry.DualCard != null)
            {
                CardDefinition a   = entry.DualCard.SideA;
                CardDefinition b   = entry.DualCard.SideB;
                string tokenA      = CardToken(a);
                string tokenB      = CardToken(b);
                bool sameName      = NameOf(a) == NameOf(b);
                bool sameType      = SideType(a) == SideType(b);
                bool bothNonNull   = a != null && b != null;
                string display     = (sameName && sameType && bothNonNull) ? tokenA : $"{tokenA} / {tokenB}";

                CardDefinition rep = RepresentativeCard(entry);
                body = rep != null ? $"{display} · {rep.Cost}⚡" : display;
            }
            else
            {
                CardDefinition c = entry.SingleCard;
                body = c != null ? $"{CardToken(c)} · {c.Cost}⚡" : "";
            }

            string marker = entry.IsUpgraded    ? " ★"
                          : entry.CanUpgrade()  ? " +"
                          :                       "";
            return body + marker;
        }

        /// <summary>
        /// Cuerpo del tooltip: por lado tipo/nombre/coste/descripción + bloque de
        /// preview de mejora al final. Nunca lanza NRE.
        /// </summary>
        public static string BuildTooltip(CardDeckEntry entry)
        {
            if (entry == null || !entry.IsValid) return "";

            var sb = new StringBuilder();
            if (entry.DualCard != null)
            {
                AppendSideTooltip(sb, entry.DualCard.SideA, "A");
                sb.AppendLine();
                AppendSideTooltip(sb, entry.DualCard.SideB, "B");
            }
            else
            {
                AppendSideTooltip(sb, entry.SingleCard, null);
            }

            string preview = BuildUpgradePreview(entry);
            if (!string.IsNullOrEmpty(preview))
            {
                sb.AppendLine();
                sb.Append(preview);
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Bloque de preview de mejora.
        /// ""              → no hay mejora configurada.
        /// "★ Mejorada"    → ya se aplicó la mejora.
        /// "Mejora ▸ ..."  → muestra en qué se convertiría la carta.
        ///                   En duales: PER-LADO — coincide con CreateUpgradedClone()
        ///                   (solo mejora el lado con HasUpgrade; el otro sin cambios).
        /// Lee CardUpgradeDef directo, nunca llama CreateUpgradedClone.
        /// </summary>
        public static string BuildUpgradePreview(CardDeckEntry entry)
        {
            if (entry == null || !entry.IsValid) return "";
            if (entry.IsUpgraded) return "★ Mejorada";

            if (entry.DualCard != null)
            {
                CardDefinition a  = entry.DualCard.SideA;
                CardDefinition b  = entry.DualCard.SideB;
                bool aHas = a != null && a.Upgrade != null && a.Upgrade.HasUpgrade;
                bool bHas = b != null && b.Upgrade != null && b.Upgrade.HasUpgrade;
                if (!aHas && !bHas) return "";

                var sb = new StringBuilder("Mejora ▸\n");
                sb.AppendLine(BuildSideUpgradeSnippet(a, aHas));
                sb.Append(BuildSideUpgradeSnippet(b, bHas));
                return sb.ToString().TrimEnd();
            }
            else
            {
                CardDefinition c = entry.SingleCard;
                if (c == null || c.Upgrade == null || !c.Upgrade.HasUpgrade) return "";
                return "Mejora ▸\n" + BuildSideUpgradeSnippet(c, true);
            }
        }

        // ────────────────────────────────────────
        // Helpers estáticos internos
        // ────────────────────────────────────────

        /// <summary>
        /// Carta representante para orden/coste/tipo. Espejo de ShopNodeController.
        /// Fallback SideA → SideB para duales con SideA null.
        /// </summary>
        private static CardDefinition RepresentativeCard(CardDeckEntry entry)
        {
            if (entry == null) return null;
            if (entry.SingleCard != null) return entry.SingleCard;
            if (entry.DualCard   != null) return entry.DualCard.SideA ?? entry.DualCard.SideB;
            return null;
        }

        // Token rich-text "[Tipo] Nombre" para un lado. null → "?".
        private static string CardToken(CardDefinition c)
        {
            if (c == null) return "?";
            string prefix = ElementTypeColors.TypePrefix(c.ElementType);
            return string.IsNullOrEmpty(prefix) ? c.CardName : $"{prefix} {c.CardName}";
        }

        private static ElementType SideType(CardDefinition c) =>
            c != null ? c.ElementType : ElementType.None;

        private static string NameOf(CardDefinition c) =>
            c != null ? c.CardName : "?";

        private static void AppendSideTooltip(StringBuilder sb, CardDefinition c, string sideLabel)
        {
            if (c == null)
            {
                if (sideLabel != null) sb.AppendLine($"[{sideLabel}] ?");
                return;
            }
            string prefix = sideLabel != null ? $"[{sideLabel}] " : "";
            string typeToken = ElementTypeColors.TypePrefix(c.ElementType);
            string header = string.IsNullOrEmpty(typeToken)
                ? $"{prefix}{c.CardName} · {c.Cost}⚡"
                : $"{prefix}{typeToken} {c.CardName} · {c.Cost}⚡";
            sb.AppendLine($"<b>{header}</b>");
            if (!string.IsNullOrEmpty(c.Description))
                sb.AppendLine(c.Description);
        }

        // Snippet de un lado en el bloque "Mejora ▸".
        // hasUpgrade=true  → valores mejorados.
        // hasUpgrade=false → lado sin cambios (mostrar igual que un "upgraded" pero con valores base).
        private static string BuildSideUpgradeSnippet(CardDefinition c, bool hasUpgrade)
        {
            if (c == null) return "?";
            if (!hasUpgrade)
            {
                // Lado no afectado por el upgrade; se muestra sin cambios.
                string p = ElementTypeColors.TypePrefix(c.ElementType);
                string h = string.IsNullOrEmpty(p) ? $"{c.CardName} · {c.Cost}⚡" : $"{p} {c.CardName} · {c.Cost}⚡";
                return string.IsNullOrEmpty(c.Description) ? h : $"{h}\n{c.Description}";
            }
            CardUpgradeDef u    = c.Upgrade;
            string name         = !string.IsNullOrEmpty(u.UpgradedName)        ? u.UpgradedName        : c.CardName + "+";
            string desc         = !string.IsNullOrEmpty(u.UpgradedDescription) ? u.UpgradedDescription : c.Description;
            int    cost         = u.OverrideCost ? u.UpgradedCost : c.Cost;
            string p2           = ElementTypeColors.TypePrefix(c.ElementType);
            string header       = string.IsNullOrEmpty(p2) ? $"{name} · {cost}⚡" : $"{p2} {name} · {cost}⚡";
            return string.IsNullOrEmpty(desc) ? header : $"{header}\n{desc}";
        }

        // ────────────────────────────────────────
        // Instancia — lógica interna de UI
        // ────────────────────────────────────────

        private void UpdateBadgeText()
        {
            int count = _cachedDeck != null ? _cachedDeck.Count : 0;
            if (_badgeLabel  != null) _badgeLabel.text  = $"Mazo ({count})";
            if (_modalTitle  != null) _modalTitle.text  = $"Mazo ({count})";
        }

        private void RebuildList()
        {
            foreach (GameObject go in _rowGos)
                if (go != null) UnityEngine.Object.Destroy(go);
            _rowGos.Clear();

            List<CardDeckEntry> sorted = SortForDisplay(_cachedDeck);
            for (int i = 0; i < sorted.Count; i++)
                AddRow(sorted[i], i);
        }

        private void AddRow(CardDeckEntry entry, int index)
        {
            GameObject rowGo = new GameObject($"Row_{index}",
                typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(EventTrigger));
            rowGo.transform.SetParent(_scrollContent, false);

            LayoutElement le    = rowGo.GetComponent<LayoutElement>();
            le.preferredHeight  = 50f;
            le.minHeight        = 40f;
            le.flexibleWidth    = 1f;

            Image bg    = rowGo.GetComponent<Image>();
            bg.sprite   = GetWhiteSprite();
            bg.color    = index % 2 == 0 ? RowEven : RowOdd;

            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(rowGo.transform, false);
            RectTransform textRt    = (RectTransform)textGo.transform;
            textRt.anchorMin        = Vector2.zero;
            textRt.anchorMax        = Vector2.one;
            textRt.offsetMin        = new Vector2(8f, 4f);
            textRt.offsetMax        = new Vector2(-8f, -4f);

            Text label                  = textGo.GetComponent<Text>();
            label.font                  = _font;
            label.fontSize              = 20;
            label.color                 = TextColor;
            label.alignment             = TextAnchor.MiddleLeft;
            label.supportRichText       = true;
            label.horizontalOverflow    = HorizontalWrapMode.Overflow;
            label.verticalOverflow      = VerticalWrapMode.Overflow;
            label.text                  = BuildRowLabel(entry);

            // Tooltip por fila — usa la posición del GO en pantalla (ScreenSpaceOverlay).
            string tooltipText = BuildTooltip(entry);
            EventTrigger trigger = rowGo.GetComponent<EventTrigger>();

            EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => ShowTooltip(tooltipText, rowGo.transform.position));
            trigger.triggers.Add(enter);

            EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => HideTooltip());
            trigger.triggers.Add(exit);

            _rowGos.Add(rowGo);
        }

        // ────────────────────────────────────────
        // Build de componentes UI
        // ────────────────────────────────────────

        private void BuildBadge(RectTransform parent)
        {
            // Esquina superior: zona entre el WorldPanel (38-62%) y el borde derecho,
            // por encima de la RelicBar → el badge no colisiona con ningún HUD existente.
            GameObject badgeGo = new GameObject("DeckViewerBadge",
                typeof(RectTransform), typeof(Image), typeof(Button));
            badgeGo.transform.SetParent(parent, false);

            RectTransform rt = (RectTransform)badgeGo.transform;
            rt.anchorMin = new Vector2(0.72f, 0.93f);
            rt.anchorMax = new Vector2(0.88f, 0.99f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bg   = badgeGo.GetComponent<Image>();
            bg.sprite  = GetWhiteSprite();
            bg.color   = BadgeBg;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(badgeGo.transform, false);
            RectTransform labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(6f, 2f);
            labelRt.offsetMax = new Vector2(-6f, -2f);

            _badgeLabel                 = labelGo.GetComponent<Text>();
            _badgeLabel.font            = _font;
            _badgeLabel.fontSize        = 20;
            _badgeLabel.color           = Color.white;
            _badgeLabel.alignment       = TextAnchor.MiddleCenter;
            _badgeLabel.supportRichText = false;
            _badgeLabel.text            = "Mazo (0)";

            _badgeButton = badgeGo.GetComponent<Button>();
            _badgeButton.onClick.AddListener(Toggle);
        }

        private void BuildModal(RectTransform parent)
        {
            // Dimmer de pantalla completa — bloquea raycasts para impedir clicks
            // en la mano durante combate (el combate es por turnos: no hace falta pausar).
            GameObject overlayGo = new GameObject("DeckViewerModal",
                typeof(RectTransform), typeof(Image));
            overlayGo.transform.SetParent(parent, false);
            _modalOverlay              = (RectTransform)overlayGo.transform;
            _modalOverlay.anchorMin    = Vector2.zero;
            _modalOverlay.anchorMax    = Vector2.one;
            _modalOverlay.offsetMin    = Vector2.zero;
            _modalOverlay.offsetMax    = Vector2.zero;

            Image dimmer          = overlayGo.GetComponent<Image>();
            dimmer.sprite         = GetWhiteSprite();
            dimmer.color          = DimmerColor;
            dimmer.raycastTarget  = true;

            // Título centrado en la franja superior del modal.
            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(_modalOverlay, false);
            RectTransform titleRt = (RectTransform)titleGo.transform;
            titleRt.anchorMin = new Vector2(0.2f, 0.88f);
            titleRt.anchorMax = new Vector2(0.8f, 0.95f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            _modalTitle                  = titleGo.GetComponent<Text>();
            _modalTitle.font             = _font;
            _modalTitle.fontSize         = 28;
            _modalTitle.color            = Color.white;
            _modalTitle.alignment        = TextAnchor.MiddleCenter;
            _modalTitle.text             = "Mazo (0)";

            BuildScrollView(_modalOverlay);

            // Botón Cerrar en la franja inferior del modal.
            GameObject closeGo = new GameObject("CloseButton",
                typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(_modalOverlay, false);
            RectTransform closeRt = (RectTransform)closeGo.transform;
            closeRt.anchorMin = new Vector2(0.38f, 0.02f);
            closeRt.anchorMax = new Vector2(0.62f, 0.08f);
            closeRt.offsetMin = Vector2.zero;
            closeRt.offsetMax = Vector2.zero;

            Image closeBg  = closeGo.GetComponent<Image>();
            closeBg.sprite = GetWhiteSprite();
            closeBg.color  = new Color(0.2f, 0.2f, 0.3f, 0.9f);

            GameObject closeLabelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            closeLabelGo.transform.SetParent(closeGo.transform, false);
            RectTransform closeLabelRt = (RectTransform)closeLabelGo.transform;
            closeLabelRt.anchorMin = Vector2.zero;
            closeLabelRt.anchorMax = Vector2.one;
            closeLabelRt.offsetMin = Vector2.zero;
            closeLabelRt.offsetMax = Vector2.zero;

            Text closeText           = closeLabelGo.GetComponent<Text>();
            closeText.font           = _font;
            closeText.fontSize       = 22;
            closeText.color          = Color.white;
            closeText.alignment      = TextAnchor.MiddleCenter;
            closeText.text           = "Cerrar";

            Button closeBtn = closeGo.GetComponent<Button>();
            closeBtn.onClick.AddListener(Close);

            _modalOverlay.gameObject.SetActive(false);
        }

        private void BuildScrollView(RectTransform modalOverlay)
        {
            // ScrollRect estándar vertical: Viewport(Mask) → Content(VLG+CSF).
            GameObject scrollGo = new GameObject("ScrollView",
                typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(modalOverlay, false);
            RectTransform scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = new Vector2(0.1f, 0.09f);
            scrollRt.anchorMax = new Vector2(0.9f, 0.87f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;

            Image scrollBg  = scrollGo.GetComponent<Image>();
            scrollBg.sprite = GetWhiteSprite();
            scrollBg.color  = PanelBg;

            // Viewport (necesita Image para que Mask funcione).
            GameObject vpGo = new GameObject("Viewport",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            vpGo.transform.SetParent(scrollGo.transform, false);
            RectTransform vpRt = (RectTransform)vpGo.transform;
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;

            Image vpImg        = vpGo.GetComponent<Image>();
            vpImg.sprite       = GetWhiteSprite();
            vpImg.color        = new Color(0f, 0f, 0f, 0.01f); // casi invisible, necesario para la Mask
            Mask mask          = vpGo.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content: crece hacia abajo desde el techo del viewport.
            GameObject contentGo = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(vpGo.transform, false);
            _scrollContent             = (RectTransform)contentGo.transform;
            _scrollContent.anchorMin   = new Vector2(0f, 1f);
            _scrollContent.anchorMax   = new Vector2(1f, 1f);
            _scrollContent.pivot       = new Vector2(0.5f, 1f);
            _scrollContent.offsetMin   = Vector2.zero;
            _scrollContent.offsetMax   = Vector2.zero;

            VerticalLayoutGroup vlg   = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing               = 2f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;
            vlg.padding                = new RectOffset(4, 4, 4, 4);

            ContentSizeFitter csf = contentGo.GetComponent<ContentSizeFitter>();
            csf.verticalFit       = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit     = ContentSizeFitter.FitMode.Unconstrained;

            ScrollRect sr         = scrollGo.GetComponent<ScrollRect>();
            sr.viewport           = vpRt;
            sr.content            = _scrollContent;
            sr.horizontal         = false;
            sr.vertical           = true;
            sr.scrollSensitivity  = 25f;
            sr.movementType       = ScrollRect.MovementType.Clamped;
        }

        private void BuildTooltipGo(RectTransform parent)
        {
            // Tooltip bajo el sub-Canvas (no bajo relicBar como en RelicInventoryView).
            // Se posiciona en espacio de pantalla al hacer hover sobre una fila.
            _tooltipGo = new GameObject("DeckViewerTooltip",
                typeof(RectTransform), typeof(Image), typeof(CanvasGroup),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _tooltipGo.transform.SetParent(parent, false);
            _tooltipRect           = (RectTransform)_tooltipGo.transform;
            _tooltipRect.sizeDelta = new Vector2(440f, 0f);
            _tooltipRect.pivot     = new Vector2(0f, 1f);
            _tooltipRect.anchorMin = Vector2.zero;
            _tooltipRect.anchorMax = Vector2.zero;

            Image bg    = _tooltipGo.GetComponent<Image>();
            bg.sprite   = GetWhiteSprite();
            bg.color    = TooltipBg;

            VerticalLayoutGroup vlg   = _tooltipGo.GetComponent<VerticalLayoutGroup>();
            vlg.padding               = new RectOffset(12, 12, 10, 10);
            vlg.spacing               = 4;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;

            ContentSizeFitter csf = _tooltipGo.GetComponent<ContentSizeFitter>();
            csf.verticalFit       = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit     = ContentSizeFitter.FitMode.Unconstrained;

            _tooltipGroup                  = _tooltipGo.GetComponent<CanvasGroup>();
            _tooltipGroup.alpha            = 0f;
            _tooltipGroup.blocksRaycasts   = false;
            _tooltipGroup.interactable     = false;

            GameObject textGo = new GameObject("TooltipText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(_tooltipGo.transform, false);

            _tooltipText                      = textGo.GetComponent<Text>();
            _tooltipText.font                 = _font;
            _tooltipText.fontSize             = 18;
            _tooltipText.color                = new Color(1f, 0.95f, 0.85f, 1f);
            _tooltipText.alignment            = TextAnchor.UpperLeft;
            _tooltipText.supportRichText      = true;
            _tooltipText.horizontalOverflow   = HorizontalWrapMode.Wrap;
            _tooltipText.verticalOverflow     = VerticalWrapMode.Overflow;
        }

        private void ShowTooltip(string text, Vector3 rowWorldPos)
        {
            if (_tooltipGo == null) return;
            _tooltipText.text     = text;
            _tooltipRect.position = rowWorldPos + new Vector3(8f, 8f, 0f);
            UIAnimationHelper.FadeIn(_tooltipGroup, 0.15f);
        }

        private void HideTooltip()
        {
            if (_tooltipGo == null) return;
            UIAnimationHelper.FadeOut(_tooltipGroup, 0.1f);
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

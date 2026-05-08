using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using RoguelikeCardBattler.Core.UI;

namespace RoguelikeCardBattler.Gameplay.Relics.UI
{
    /// <summary>
    /// Fila de íconos de Retazos en HUD (estilo StS). Clase de presentación pura
    /// (no MonoBehaviour) — la posee CombatUIController, que la rebuildea desde
    /// Update() cuando la lista cambia. Expone Refresh() para sincronizar y
    /// AddRelic() para pulse-on-acquire. RunMapView se integra en Sub-PR 3F.
    /// </summary>
    public class RelicInventoryView
    {
        private const int IconSize = 56;
        private const int IconSpacing = 8;
        private static readonly Color IconBgColor = new Color(0.1f, 0.08f, 0.05f, 0.85f);
        private static readonly Color IconBgFallback = new Color(0.55f, 0.45f, 0.25f, 1f);
        private static readonly Color TooltipBg = new Color(0.04f, 0.04f, 0.06f, 0.95f);

        private readonly RectTransform _container;
        private readonly Font _font;
        private readonly Canvas _rootCanvas;
        private readonly Dictionary<RelicInstance, GameObject> _iconByInstance = new Dictionary<RelicInstance, GameObject>();
        private readonly HashSet<RelicInstance> _seen = new HashSet<RelicInstance>();
        private GameObject _tooltipGo;
        private CanvasGroup _tooltipGroup;
        private Text _tooltipText;
        private RectTransform _tooltipRect;

        public RelicInventoryView(RectTransform parent, Font font)
        {
            _font = font;
            _rootCanvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;

            GameObject containerGo = new GameObject("RelicInventory", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            _container = containerGo.GetComponent<RectTransform>();
            _container.SetParent(parent, false);
            _container.anchorMin = new Vector2(0f, 0f);
            _container.anchorMax = new Vector2(0.7f, 1f);
            _container.offsetMin = new Vector2(8f, 4f);
            _container.offsetMax = new Vector2(-8f, -4f);

            HorizontalLayoutGroup layout = containerGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = IconSpacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            BuildTooltip();
        }

        public void Refresh(IReadOnlyList<RelicInstance> relics)
        {
            if (_container == null) return;
            HashSet<RelicInstance> current = new HashSet<RelicInstance>();
            if (relics != null)
            {
                for (int i = 0; i < relics.Count; i++)
                {
                    RelicInstance inst = relics[i];
                    if (inst == null) continue;
                    current.Add(inst);
                    if (!_iconByInstance.ContainsKey(inst))
                    {
                        CreateIcon(inst);
                    }
                }
            }
            // Eliminar íconos cuyas instancias ya no están (improbable en M3 pero defensivo).
            List<RelicInstance> toRemove = null;
            foreach (KeyValuePair<RelicInstance, GameObject> kv in _iconByInstance)
            {
                if (!current.Contains(kv.Key))
                {
                    toRemove = toRemove ?? new List<RelicInstance>();
                    toRemove.Add(kv.Key);
                }
            }
            if (toRemove != null)
            {
                foreach (RelicInstance r in toRemove)
                {
                    if (_iconByInstance.TryGetValue(r, out GameObject go) && go != null) Object.Destroy(go);
                    _iconByInstance.Remove(r);
                    _seen.Remove(r);
                }
            }
        }

        public void AddRelic(RelicInstance relic)
        {
            if (relic == null) return;
            if (!_iconByInstance.TryGetValue(relic, out GameObject go))
            {
                go = CreateIcon(relic);
            }
            _seen.Add(relic);
            if (go == null) return;
            UIAnimationHelper.ScaleIn(go.transform, 0.35f);
            UIAnimationHelper.PulseLoop(go.transform, scaleAmount: 1.18f, duration: 0.45f);
            // Detener el pulso después de 3 ciclos (3 * 0.45 ≈ 1.35s).
            DOVirtual.DelayedCall(1.5f, () =>
            {
                if (go != null) UIAnimationHelper.StopPulse(go.transform);
            });
        }

        public void Cleanup()
        {
            foreach (var kv in _iconByInstance)
            {
                if (kv.Value != null) Object.Destroy(kv.Value);
            }
            _iconByInstance.Clear();
            _seen.Clear();
            if (_tooltipGo != null) Object.Destroy(_tooltipGo);
        }

        private GameObject CreateIcon(RelicInstance instance)
        {
            if (instance == null || instance.Definition == null) return null;
            RelicDefinition def = instance.Definition;

            GameObject iconGo = new GameObject($"Relic_{def.DisplayName}",
                typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(EventTrigger));
            iconGo.transform.SetParent(_container, false);

            LayoutElement le = iconGo.GetComponent<LayoutElement>();
            le.preferredWidth = IconSize;
            le.preferredHeight = IconSize;
            le.minWidth = IconSize;
            le.minHeight = IconSize;

            Image bg = iconGo.GetComponent<Image>();
            bg.color = IconBgColor;

            // Sprite del Retazo si existe; sino badge de fallback con la inicial.
            if (def.Icon != null)
            {
                bg.sprite = def.Icon;
                bg.color = Color.white;
            }
            else
            {
                bg.color = IconBgFallback;
                GameObject letterGo = new GameObject("Letter", typeof(RectTransform), typeof(Text));
                letterGo.transform.SetParent(iconGo.transform, false);
                RectTransform lt = (RectTransform)letterGo.transform;
                lt.anchorMin = Vector2.zero; lt.anchorMax = Vector2.one;
                lt.offsetMin = Vector2.zero; lt.offsetMax = Vector2.zero;
                Text letter = letterGo.GetComponent<Text>();
                letter.font = _font;
                letter.fontSize = 26;
                letter.color = new Color(1f, 0.92f, 0.7f, 1f);
                letter.alignment = TextAnchor.MiddleCenter;
                letter.text = string.IsNullOrEmpty(def.DisplayName) ? "R" : def.DisplayName.Substring(0, 1).ToUpper();
            }

            EventTrigger trigger = iconGo.GetComponent<EventTrigger>();
            EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => ShowTooltip(def, iconGo.transform.position));
            trigger.triggers.Add(enter);
            EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => HideTooltip());
            trigger.triggers.Add(exit);

            _iconByInstance[instance] = iconGo;
            return iconGo;
        }

        private void BuildTooltip()
        {
            if (_rootCanvas == null) return;
            _tooltipGo = new GameObject("RelicTooltip",
                typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _tooltipGo.transform.SetParent(_rootCanvas.transform, false);
            _tooltipRect = (RectTransform)_tooltipGo.transform;
            _tooltipRect.sizeDelta = new Vector2(360f, 0f);
            _tooltipRect.pivot = new Vector2(0f, 1f);
            _tooltipRect.anchorMin = new Vector2(0f, 0f);
            _tooltipRect.anchorMax = new Vector2(0f, 0f);

            Image bg = _tooltipGo.GetComponent<Image>();
            bg.color = TooltipBg;

            VerticalLayoutGroup vlg = _tooltipGo.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 10, 10);
            vlg.spacing = 4;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            ContentSizeFitter csf = _tooltipGo.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _tooltipGroup = _tooltipGo.GetComponent<CanvasGroup>();
            _tooltipGroup.alpha = 0f;
            _tooltipGroup.blocksRaycasts = false;
            _tooltipGroup.interactable = false;

            GameObject textGo = new GameObject("TooltipText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(_tooltipGo.transform, false);
            _tooltipText = textGo.GetComponent<Text>();
            _tooltipText.font = _font;
            _tooltipText.fontSize = 18;
            _tooltipText.color = new Color(1f, 0.95f, 0.85f, 1f);
            _tooltipText.alignment = TextAnchor.UpperLeft;
            _tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tooltipText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void ShowTooltip(RelicDefinition def, Vector3 worldPos)
        {
            if (_tooltipGo == null || def == null) return;
            string flavor = string.IsNullOrEmpty(def.FlavorText) ? string.Empty : $"\n\n<i>{def.FlavorText}</i>";
            _tooltipText.text = $"<b>{def.DisplayName}</b>\n{def.Description}{flavor}";
            _tooltipText.supportRichText = true;
            _tooltipRect.position = worldPos + new Vector3(IconSize * 0.6f, IconSize * 1.2f, 0f);
            UIAnimationHelper.FadeIn(_tooltipGroup, 0.15f);
        }

        private void HideTooltip()
        {
            if (_tooltipGo == null) return;
            UIAnimationHelper.FadeOut(_tooltipGroup, 0.1f);
        }
    }
}

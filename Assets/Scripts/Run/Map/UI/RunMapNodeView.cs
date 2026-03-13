using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using RoguelikeCardBattler.Core.UI;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Vista individual de un nodo en el mapa de run.
    /// Crea un botón UI posicionado manualmente y aplica estado visual
    /// (Locked/Available/Completed) leyendo NodeState.
    /// Available nodes pulse gently; transitioning to Completed triggers a punch.
    /// </summary>
    public class RunMapNodeView
    {
        private static readonly Color LockedBg = new Color(0.18f, 0.18f, 0.22f, 0.6f);
        private static readonly Color AvailableBg = new Color(0.15f, 0.3f, 0.55f, 1f);
        private static readonly Color CompletedBg = new Color(0.2f, 0.45f, 0.25f, 1f);
        private static readonly Color BossAvailableBg = new Color(0.55f, 0.12f, 0.12f, 1f);
        private static readonly Color BossLockedBg = new Color(0.3f, 0.1f, 0.1f, 0.6f);

        public int NodeId { get; }
        public NodeType Type { get; }
        public RectTransform Rect { get; }
        public NodeState CurrentState { get; private set; } = NodeState.Locked;

        private readonly Button _button;
        private readonly Image _bgImage;
        private readonly Text _label;
        private readonly CanvasGroup _canvasGroup;
        private bool _entrancePlaying;

        public RunMapNodeView(int nodeId, NodeType type, RectTransform parent,
            Vector2 position, Vector2 size, Font font, Sprite whiteSprite,
            Action<int> onClick)
        {
            NodeId = nodeId;
            Type = type;

            GameObject go = new GameObject($"MapNode_{nodeId}",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);

            Rect = go.GetComponent<RectTransform>();
            Rect.anchorMin = new Vector2(0.5f, 1f);
            Rect.anchorMax = new Vector2(0.5f, 1f);
            Rect.pivot = new Vector2(0.5f, 0.5f);
            Rect.anchoredPosition = position;
            Rect.sizeDelta = size;

            _bgImage = go.GetComponent<Image>();
            _bgImage.sprite = whiteSprite;
            _bgImage.type = Image.Type.Simple;
            _bgImage.raycastTarget = true;

            _canvasGroup = go.GetComponent<CanvasGroup>();

            _button = go.GetComponent<Button>();
            int capturedId = nodeId;
            _button.onClick.AddListener(() => onClick?.Invoke(capturedId));

            GameObject textGo = new GameObject("Label",
                typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 2f);
            textRect.offsetMax = new Vector2(-4f, -2f);

            _label = textGo.GetComponent<Text>();
            _label.font = font;
            _label.fontSize = type == NodeType.Boss ? 18 : 16;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = Color.white;
            _label.raycastTarget = false;

            Outline outline = textGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        public void ApplyState(NodeState state)
        {
            NodeState previous = CurrentState;
            bool isBoss = Type == NodeType.Boss;
            bool completed = state == NodeState.Completed;
            bool available = state == NodeState.Available;

            if (completed)
            {
                _bgImage.color = CompletedBg;
                _label.color = new Color(0.8f, 0.9f, 0.8f, 1f);
                _button.interactable = false;
            }
            else if (available)
            {
                _bgImage.color = isBoss ? BossAvailableBg : AvailableBg;
                _label.color = Color.white;
                _button.interactable = true;
            }
            else
            {
                _bgImage.color = isBoss ? BossLockedBg : LockedBg;
                _label.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                _button.interactable = false;
            }

            _label.text = FormatLabel(completed);
            CurrentState = state;

            if (_entrancePlaying)
            {
                return;
            }

            if (previous == NodeState.Available && state == NodeState.Completed)
            {
                UIAnimationHelper.StopPulse(Rect);
                UIAnimationHelper.Punch(Rect, 0.12f, 0.3f);
            }
            else if (state == NodeState.Available)
            {
                UIAnimationHelper.PulseLoop(Rect, 1.08f, 0.7f);
            }
            else if (state != NodeState.Available)
            {
                UIAnimationHelper.StopPulse(Rect);
            }
        }

        /// <summary>
        /// Staggered entrance: node starts invisible and at scale 0, then
        /// pops in with a combined scale + fade animation after the given delay.
        /// OnComplete re-calls ApplyState so PulseLoop starts for Available nodes
        /// only after the entrance finishes.
        /// </summary>
        public void PlayEntrance(float delay)
        {
            _entrancePlaying = true;
            DOTween.Kill(Rect);
            _canvasGroup.alpha = 0f;
            Rect.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence()
                .SetTarget(Rect)
                .SetUpdate(true)
                .SetDelay(delay);

            seq.Append(
                DOTween.To(() => Rect.localScale, x => Rect.localScale = x, Vector3.one, 0.25f)
                    .SetEase(Ease.OutBack));
            seq.Join(
                DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 1f, 0.25f)
                    .SetEase(Ease.OutQuad));
            seq.OnComplete(() =>
            {
                _entrancePlaying = false;
                ApplyState(CurrentState);
            });
        }

        private string FormatLabel(bool completed)
        {
            string icon = Type switch
            {
                NodeType.Combat => "\u2694",
                NodeType.Event => "?",
                NodeType.Shop => "$",
                NodeType.Campfire => "\u25b3",
                NodeType.Elite => "\u2694\u2694",
                NodeType.Boss => "\u2620 BOSS",
                _ => "\u2022"
            };

            string check = completed ? " \u2713" : "";
            return Type == NodeType.Boss
                ? $"{icon}{check}"
                : $"{icon} {NodeId + 1}{check}";
        }
    }
}

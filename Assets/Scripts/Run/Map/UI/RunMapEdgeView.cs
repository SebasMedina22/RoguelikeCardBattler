using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Dibuja una línea UI entre dos puntos del mapa usando un Image estirado y rotado.
    /// No recibe input (raycastTarget = false).
    /// Supports highlight animation when a connected node becomes available.
    /// </summary>
    public class RunMapEdgeView
    {
        private static readonly Color DefaultEdgeColor = new Color(0.4f, 0.5f, 0.6f, 0.5f);
        private static readonly Color HighlightColor = new Color(0.85f, 0.75f, 0.4f, 0.8f);

        public RectTransform Rect { get; }

        private readonly Image _image;
        private readonly Color _defaultColor;

        public static RunMapEdgeView Create(RectTransform parent, Vector2 from, Vector2 to,
            float thickness, Sprite whiteSprite)
        {
            return new RunMapEdgeView(parent, from, to, thickness, whiteSprite);
        }

        private RunMapEdgeView(RectTransform parent, Vector2 from, Vector2 to,
            float thickness, Sprite whiteSprite)
        {
            Vector2 diff = to - from;
            float distance = diff.magnitude;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            GameObject go = new GameObject("Edge", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            Rect = go.GetComponent<RectTransform>();
            Rect.anchorMin = new Vector2(0.5f, 1f);
            Rect.anchorMax = new Vector2(0.5f, 1f);
            Rect.pivot = new Vector2(0.5f, 0.5f);
            Rect.anchoredPosition = (from + to) / 2f;
            Rect.sizeDelta = new Vector2(distance, thickness);
            Rect.localRotation = Quaternion.Euler(0f, 0f, angle);

            _image = go.GetComponent<Image>();
            _image.sprite = whiteSprite;
            _image.type = Image.Type.Simple;
            _image.color = DefaultEdgeColor;
            _image.raycastTarget = false;
            _defaultColor = DefaultEdgeColor;
        }

        public void SetColor(Color color)
        {
            _image.color = color;
        }

        /// <summary>
        /// Animates the edge color from default to a golden highlight,
        /// signaling that the connected path has been unlocked.
        /// </summary>
        public void AnimateHighlight(float duration = 0.5f)
        {
            DOTween.Kill(_image);
            DOTween.To(() => _image.color, c => _image.color = c, HighlightColor, duration)
                .SetTarget(_image)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        /// <summary>
        /// Kills any running tweens on this edge. Call before scene unload to avoid
        /// "destroyed RectTransform" / Safe Mode warnings.
        /// </summary>
        public void KillTweens()
        {
            DOTween.Kill(_image);
        }

        /// <summary>
        /// Immediately kills any running tween and resets to the default color.
        /// </summary>
        public void ResetColor()
        {
            DOTween.Kill(_image);
            _image.color = _defaultColor;
        }

        /// <summary>
        /// Staggered entrance: starts invisible then fades to the default color.
        /// Called by RunMapView during the entrance animation sequence.
        /// </summary>
        public void PlayEntrance(float delay)
        {
            Color transparent = new Color(_defaultColor.r, _defaultColor.g, _defaultColor.b, 0f);
            _image.color = transparent;
            DOTween.To(() => _image.color, c => _image.color = c, _defaultColor, 0.25f)
                .SetTarget(_image)
                .SetDelay(delay)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }
    }
}

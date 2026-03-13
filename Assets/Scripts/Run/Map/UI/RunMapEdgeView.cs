using UnityEngine;
using UnityEngine.UI;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Dibuja una línea UI entre dos puntos del mapa usando un Image estirado y rotado.
    /// No recibe input (raycastTarget = false).
    /// </summary>
    public class RunMapEdgeView
    {
        private static readonly Color EdgeColor = new Color(0.4f, 0.5f, 0.6f, 0.5f);

        public RectTransform Rect { get; }

        private readonly Image _image;

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
            _image.color = EdgeColor;
            _image.raycastTarget = false;
        }

        public void SetColor(Color color)
        {
            _image.color = color;
        }
    }
}

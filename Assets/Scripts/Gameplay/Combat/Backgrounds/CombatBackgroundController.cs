using System;
using System.Collections.Generic;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Combat;

namespace RoguelikeCardBattler.Gameplay.Combat.Backgrounds
{
    /// <summary>
    /// Controlador scene-owned que aplica sprites por mundo y parallax simple
    /// a capas de background (SpriteRenderer). No contiene lógica de combate.
    /// </summary>
    public class CombatBackgroundController : MonoBehaviour
    {
        [Serializable]
        public class LayerBinding
        {
            public string layerName = "BG_Far";
            public SpriteRenderer renderer;
            [Range(0f, 0.2f)] public float parallaxFactor = 0.02f;

            [NonSerialized] public Vector3 baseLocalPosition;
        }

        [Header("Scene References")]
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private CombatBackgroundTheme theme;
        [SerializeField] private List<LayerBinding> layers = new List<LayerBinding>();

        [Header("Auto Fit")]
        [SerializeField] private bool autoFitToCamera = true;
        [SerializeField] private Camera targetCamera;
        [SerializeField, Min(1f)] private float pixelsPerUnitFallback = 100f;

        [Header("Parallax")]
        [SerializeField] private Transform parallaxTarget;
        [SerializeField, Range(0f, 0.25f)] private float mouseInfluence = 0.05f;

        private Vector3 _startTargetPos;
        private TurnManager.WorldSide _lastWorld;
        private bool _initialized;

        private void Awake()
        {
            if (turnManager == null)
            {
                turnManager = FindFirstObjectByType<TurnManager>();
            }

            if (parallaxTarget == null && Camera.main != null)
            {
                parallaxTarget = Camera.main.transform;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            _startTargetPos = parallaxTarget != null ? parallaxTarget.position : Vector3.zero;
            CacheBasePositions();
            ApplyWorld(turnManager != null ? turnManager.CurrentWorld : TurnManager.WorldSide.A);
            _initialized = true;
        }

        private void LateUpdate()
        {
            if (!_initialized)
            {
                return;
            }

            if (turnManager != null && turnManager.CurrentWorld != _lastWorld)
            {
                ApplyWorld(turnManager.CurrentWorld);
            }

            ApplyParallax();
        }

        private void CacheBasePositions()
        {
            foreach (LayerBinding layer in layers)
            {
                if (layer?.renderer != null)
                {
                    layer.baseLocalPosition = layer.renderer.transform.localPosition;
                }
            }
        }

        private void ApplyWorld(TurnManager.WorldSide world)
        {
            _lastWorld = world;
            if (theme == null)
            {
                return;
            }

            foreach (LayerBinding layer in layers)
            {
                if (layer?.renderer == null)
                {
                    continue;
                }

                CombatBackgroundTheme.LayerSprites data = theme.GetLayer(layer.layerName);
                if (data == null)
                {
                    continue;
                }

                layer.renderer.sprite = world == TurnManager.WorldSide.A
                    ? data.worldASprite
                    : data.worldBSprite;
                layer.parallaxFactor = data.parallaxFactor;
                if (autoFitToCamera)
                {
                    bool alignBottom = layer.layerName == "FG";
                    FitLayerToCamera(layer, alignBottom);
                }
            }

            CacheBasePositions();
        }

        private void ApplyParallax()
        {
            Vector3 targetDelta = Vector3.zero;
            if (parallaxTarget != null)
            {
                targetDelta = parallaxTarget.position - _startTargetPos;
            }

            Vector2 mouseOffset = Vector2.zero;
            if (mouseInfluence > 0f)
            {
                Vector2 mouse = Input.mousePosition;
                mouseOffset = new Vector2(
                    (mouse.x / Screen.width) - 0.5f,
                    (mouse.y / Screen.height) - 0.5f);
            }

            foreach (LayerBinding layer in layers)
            {
                if (layer?.renderer == null)
                {
                    continue;
                }

                Vector3 parallax = new Vector3(targetDelta.x, targetDelta.y, 0f) * layer.parallaxFactor;
                parallax += (Vector3)mouseOffset * (layer.parallaxFactor * mouseInfluence);
                layer.renderer.transform.localPosition = layer.baseLocalPosition + parallax;
            }
        }

        /// <summary>
        /// Ajusta la capa para cubrir el viewport de cámara. Para FG se alinea al borde inferior.
        /// Esto evita tener que ajustar transforms a mano con cada sprite del pack.
        /// </summary>
        private void FitLayerToCamera(LayerBinding layer, bool alignBottom)
        {
            if (layer?.renderer == null || targetCamera == null)
            {
                return;
            }

            if (!targetCamera.orthographic)
            {
                return;
            }

            Sprite sprite = layer.renderer.sprite;
            if (sprite == null)
            {
                return;
            }

            float height = 2f * targetCamera.orthographicSize;
            float width = height * targetCamera.aspect;

            float ppu = sprite.pixelsPerUnit > 0f ? sprite.pixelsPerUnit : pixelsPerUnitFallback;
            Vector2 spriteSize = sprite.rect.size / ppu;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            {
                return;
            }

            float scaleToFitWidth = width / spriteSize.x;
            float scaleToFitHeight = height / spriteSize.y;
            float scale = alignBottom ? scaleToFitWidth : Mathf.Max(scaleToFitWidth, scaleToFitHeight);

            Transform t = layer.renderer.transform;
            t.localScale = new Vector3(scale, scale, 1f);

            if (alignBottom)
            {
                float scaledHeight = spriteSize.y * scale;
                Vector3 camPos = targetCamera.transform.position;
                float bottomY = camPos.y - (height * 0.5f);
                t.position = new Vector3(camPos.x, bottomY + (scaledHeight * 0.5f), t.position.z);
            }
        }
    }
}

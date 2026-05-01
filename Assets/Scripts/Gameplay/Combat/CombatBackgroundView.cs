using UnityEngine;
using UnityEngine.UI;

namespace RoguelikeCardBattler.Gameplay.Combat
{
    /// <summary>
    /// Gestiona las capas de fondo del combate (sky y ground): creación en runtime,
    /// sprites/colores por mundo, y CoverFill. Detecta cambios de mundo mediante
    /// polling autónomo (compara CurrentWorld cada frame) en lugar de recibir
    /// callbacks desde CombatHudView o CombatUIController.
    ///
    /// Extraído de CombatUIController como parte de la descomposición en componentes
    /// independientes (ver Docs/dev/COMBAT_ARCHITECTURE.md — Fase 4).
    ///
    /// Por qué polling y no callback: en M2 las cartas con efecto WorldSwitch
    /// cambiarán el mundo vía TurnManager sin pasar por el botón Change World.
    /// Con polling el view reacciona automáticamente a cualquier origen de cambio,
    /// sin que haya que coordinar cada nuevo punto de llamada manualmente.
    /// </summary>
    public class CombatBackgroundView : MonoBehaviour
    {
        // ── SerializeFields: movidos desde CombatUIController ──
        [Header("Background (Canvas Placeholder)")]
        [SerializeField] private bool useCanvasBackground = false;
        [SerializeField] private Color worldASkyColor = new Color(0.07f, 0.12f, 0.3f, 1f);
        [SerializeField] private Color worldAGroundColor = new Color(0.02f, 0.07f, 0.09f, 1f);
        [SerializeField] private Color worldBSkyColor = new Color(0.2f, 0.05f, 0.05f, 1f);
        [SerializeField] private Color worldBGroundColor = new Color(0.09f, 0.02f, 0.04f, 1f);
        [Header("Background Sprites")]
        [SerializeField] private Sprite worldASkySprite;
        [SerializeField] private Sprite worldAGroundSprite;
        [SerializeField] private Sprite worldBSkySprite;
        [SerializeField] private Sprite worldBGroundSprite;

        // ── Referencias inyectadas ──
        private TurnManager _turnManager;

        // ── Estado de fondo ──
        private Image _skyBackgroundImage;
        private Image _groundBackgroundImage;

        // Centinela: valor fuera del enum para forzar un refresh en el primer Update.
        private TurnManager.WorldSide _lastKnownWorld = (TurnManager.WorldSide)(-1);
        private bool _initialized;

        /// <summary>
        /// Inyecta el TurnManager para leer CurrentWorld. Llamado por CombatUIController
        /// antes de TryCreateLayers().
        /// </summary>
        public void Initialize(TurnManager turnManager)
        {
            _turnManager = turnManager;
            _initialized = true;
        }

        /// <summary>
        /// Crea las capas sky/ground como primeros hijos del canvas (z-order).
        /// Solo actúa si useCanvasBackground = true. Se llama desde BuildUI() justo
        /// después de CreateCanvas() para garantizar el orden de capas correcto antes
        /// de que el resto de la UI se construya encima.
        /// </summary>
        public void TryCreateLayers(RectTransform canvasRect)
        {
            if (!useCanvasBackground) return;

            GameObject skyGO = new GameObject("BackgroundSky", typeof(RectTransform), typeof(Image));
            skyGO.transform.SetParent(canvasRect, false);
            RectTransform sky = skyGO.GetComponent<RectTransform>();
            sky.anchorMin = new Vector2(0f, 0f);
            sky.anchorMax = new Vector2(1f, 1f);
            sky.offsetMin = Vector2.zero;
            sky.offsetMax = Vector2.zero;
            sky.pivot = new Vector2(0.5f, 0.5f);
            sky.SetAsFirstSibling();
            _skyBackgroundImage = skyGO.GetComponent<Image>();
            _skyBackgroundImage.color = worldASkyColor;
            _skyBackgroundImage.raycastTarget = false;

            GameObject groundGO = new GameObject("BackgroundGround", typeof(RectTransform), typeof(Image));
            groundGO.transform.SetParent(canvasRect, false);
            RectTransform ground = groundGO.GetComponent<RectTransform>();
            ground.anchorMin = new Vector2(0f, 0f);
            ground.anchorMax = new Vector2(1f, 0.45f);
            ground.offsetMin = Vector2.zero;
            ground.offsetMax = Vector2.zero;
            ground.pivot = new Vector2(0.5f, 0.5f);
            ground.SetSiblingIndex(1);
            _groundBackgroundImage = groundGO.GetComponent<Image>();
            _groundBackgroundImage.color = worldAGroundColor;
            _groundBackgroundImage.raycastTarget = false;

            // Pintado inicial para que el primer frame no muestre el estado centinela.
            _lastKnownWorld = _turnManager != null ? _turnManager.CurrentWorld : TurnManager.WorldSide.A;
            UpdateBackgroundVisuals();
        }

        private void Update()
        {
            if (!_initialized || _turnManager == null) return;
            if (_turnManager.CurrentWorld != _lastKnownWorld)
            {
                _lastKnownWorld = _turnManager.CurrentWorld;
                UpdateBackgroundVisuals();
            }
        }

        private void UpdateBackgroundVisuals()
        {
            if (_turnManager == null) return;

            bool isWorldA = _turnManager.CurrentWorld == TurnManager.WorldSide.A;
            Color targetSky = isWorldA ? worldASkyColor : worldBSkyColor;
            Color targetGround = isWorldA ? worldAGroundColor : worldBGroundColor;
            Sprite targetSkySprite = isWorldA ? worldASkySprite : worldBSkySprite;
            Sprite targetGroundSprite = isWorldA ? worldAGroundSprite : worldBGroundSprite;

            if (_skyBackgroundImage != null)
            {
                if (targetSkySprite != null)
                {
                    _skyBackgroundImage.sprite = targetSkySprite;
                    _skyBackgroundImage.type = Image.Type.Simple;
                    _skyBackgroundImage.preserveAspect = true;
                    _skyBackgroundImage.color = Color.white;
                    CoverFill(_skyBackgroundImage.rectTransform, targetSkySprite);
                }
                else
                {
                    _skyBackgroundImage.sprite = null;
                    _skyBackgroundImage.color = targetSky;
                }
            }

            if (_groundBackgroundImage != null)
            {
                if (targetGroundSprite != null)
                {
                    _groundBackgroundImage.sprite = targetGroundSprite;
                    _groundBackgroundImage.type = Image.Type.Simple;
                    _groundBackgroundImage.preserveAspect = true;
                    _groundBackgroundImage.color = Color.white;
                    CoverFill(_groundBackgroundImage.rectTransform, targetGroundSprite);
                }
                else
                {
                    _groundBackgroundImage.sprite = null;
                    // Sin sprite de ground, transparente para no tapar el sky.
                    Color transparent = targetGround;
                    transparent.a = 0f;
                    _groundBackgroundImage.color = transparent;
                }
            }
        }

        /// <summary>
        /// Simula "cover": escala manteniendo aspect ratio y recorta lo sobrante
        /// para llenar el panel sin deformar el sprite.
        /// </summary>
        private void CoverFill(RectTransform rectTransform, Sprite sprite)
        {
            if (rectTransform == null || sprite == null) return;

            Rect panelRect = rectTransform.rect;
            float panelAspect = panelRect.width / Mathf.Max(0.0001f, panelRect.height);
            float spriteAspect = sprite.rect.width / Mathf.Max(0.0001f, sprite.rect.height);

            Vector3 scale;
            if (spriteAspect > panelAspect)
            {
                // Sprite más ancho: escalar por altura, recortar ancho.
                scale = new Vector3(spriteAspect / panelAspect, 1f, 1f);
            }
            else
            {
                // Sprite más alto: escalar por ancho, recortar alto.
                scale = new Vector3(1f, panelAspect / spriteAspect, 1f);
            }

            rectTransform.localScale = scale;
        }
    }
}

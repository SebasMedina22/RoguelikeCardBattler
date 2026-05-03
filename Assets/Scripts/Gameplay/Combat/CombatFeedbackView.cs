using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using RoguelikeCardBattler.Core.Audio;
using RoguelikeCardBattler.Core.UI;

namespace RoguelikeCardBattler.Gameplay.Combat
{
    /// <summary>
    /// Maneja toda la retroalimentación visual de combate: popups de efectividad
    /// (WEAK/RESIST/+ESTILO), shake de enemigos al recibir daño, flash de paneles,
    /// toast de límite de mano, y textos de victoria/derrota.
    ///
    /// Extraído de CombatUIController como parte de la descomposición en componentes
    /// independientes (ver Docs/dev/COMBAT_ARCHITECTURE.md — Fase 1).
    ///
    /// Este componente es SOLO presentación — nunca muta estado de gameplay.
    /// Se suscribe directamente a eventos de TurnManager en OnEnable/OnDisable.
    /// </summary>
    public class CombatFeedbackView : MonoBehaviour
    {
        // ── Referencias inyectadas por CombatUIController.InitializeFeedbackView() ──
        private TurnManager _turnManager;
        private Text _hitFeedbackText;
        private Text _handLimitText;
        private Text _playerHpLabel;
        private Image _playerAvatarImage;
        private Image _enemyAvatarImage;
        private Image _enemyAvatarSpriteImage;
        private Image _energyPanelImage;
        private Image _worldPanelImage;
        private SpriteFrameAnimatorUI _enemyAnimator;
        private Canvas _canvas;
        private Font _uiFont;

        // ── Configuración (copiada desde los SerializeField de CombatUIController) ──
        private float _hitFeedbackDuration = 0.85f;
        private float _handLimitToastDuration = 0.7f;
        private float _handLimitToastCooldown = 0.6f;

        // ── Estado interno ──
        private float _hitFeedbackTimer;
        private float _handLimitTimer;
        private float _handLimitCooldownRemaining;
        private Coroutine _panelFlashRoutine;
        private bool _initialized;

        // Feedback visual: color de flash para paneles de error (ej. sin energía).
        private static readonly Color HudFlashColor = new Color(1f, 0.4f, 0.4f, 0.9f);

        /// <summary>
        /// Inyecta todas las referencias necesarias. Llamado por CombatUIController
        /// después de BuildUI(). Sin esta llamada, el componente no hace nada.
        /// </summary>
        public void Initialize(
            TurnManager turnManager,
            Text hitFeedbackText,
            Text handLimitText,
            Text playerHpLabel,
            Image playerAvatarImage,
            Image enemyAvatarImage,
            Image enemyAvatarSpriteImage,
            Image energyPanelImage,
            Image worldPanelImage,
            SpriteFrameAnimatorUI enemyAnimator,
            Canvas canvas,
            Font uiFont,
            float hitFeedbackDuration,
            float handLimitToastDuration,
            float handLimitToastCooldown)
        {
            _turnManager = turnManager;
            _hitFeedbackText = hitFeedbackText;
            _handLimitText = handLimitText;
            _playerHpLabel = playerHpLabel;
            _playerAvatarImage = playerAvatarImage;
            _enemyAvatarImage = enemyAvatarImage;
            _enemyAvatarSpriteImage = enemyAvatarSpriteImage;
            _energyPanelImage = energyPanelImage;
            _worldPanelImage = worldPanelImage;
            _enemyAnimator = enemyAnimator;
            _canvas = canvas;
            _uiFont = uiFont;
            _hitFeedbackDuration = hitFeedbackDuration;
            _handLimitToastDuration = handLimitToastDuration;
            _handLimitToastCooldown = handLimitToastCooldown;
            _initialized = true;

            SubscribeEvents();
        }

        private void OnEnable()
        {
            if (_initialized)
            {
                SubscribeEvents();
            }
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (_turnManager == null) return;
            _turnManager.PlayerHitEffectiveness += OnPlayerHitEffectiveness;
            _turnManager.EnemyTookDamage += OnEnemyTookDamage;
            _turnManager.PlayerHandLimitReached += OnPlayerHandLimitReached;
        }

        private void UnsubscribeEvents()
        {
            if (_turnManager == null) return;
            _turnManager.PlayerHitEffectiveness -= OnPlayerHitEffectiveness;
            _turnManager.EnemyTookDamage -= OnEnemyTookDamage;
            _turnManager.PlayerHandLimitReached -= OnPlayerHandLimitReached;
        }

        private void Update()
        {
            if (!_initialized) return;
            UpdateHitFeedbackTimer();
            UpdateHandLimitTimer();
        }

        // ────────────────────────────────────────────────────────
        // Efectividad: popups WEAK / RESIST / +ESTILO
        // ────────────────────────────────────────────────────────

        private void OnPlayerHitEffectiveness(Effectiveness effectiveness, bool styleChargeGranted)
        {
            if (_hitFeedbackText == null) return;

            string message = effectiveness switch
            {
                Effectiveness.SuperEficaz => styleChargeGranted ? "WEAK!\n+ESTILO" : "WEAK!",
                Effectiveness.PocoEficaz => "RESIST",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(message))
            {
                _hitFeedbackText.gameObject.SetActive(false);
                return;
            }

            _hitFeedbackText.text = message;
            _hitFeedbackText.gameObject.SetActive(true);
            _hitFeedbackTimer = _hitFeedbackDuration;

            // Juice: player avatar shake + HP label red flash + SFX
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.HitSFX);
            if (_playerAvatarImage != null)
            {
                UIAnimationHelper.Punch(_playerAvatarImage.transform, 0.15f, 0.3f);
            }

            if (_playerHpLabel != null)
            {
                _playerHpLabel.color = new Color(1f, 0.3f, 0.3f, 1f);
                DOTween.To(
                    () => _playerHpLabel.color,
                    x => _playerHpLabel.color = x,
                    Color.white, 0.4f)
                    .SetTarget(_playerHpLabel)
                    .SetUpdate(true);
            }
        }

        // ────────────────────────────────────────────────────────
        // Daño al enemigo: shake + hit animation
        // ────────────────────────────────────────────────────────

        private void OnEnemyTookDamage(int damage)
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.HitSFX);
            if (damage <= 0) return;

            if (_enemyAnimator != null)
            {
                _enemyAnimator.PlayAttackOnce();
            }
            else
            {
                // Fallback: DOTween-based enemy shake
                if (_enemyAvatarImage != null)
                {
                    UIAnimationHelper.Punch(_enemyAvatarImage.transform, 0.2f, 0.25f);
                }

                if (_enemyAvatarSpriteImage != null)
                {
                    Color originalColor = _enemyAvatarSpriteImage.color;
                    _enemyAvatarSpriteImage.color = Color.white;
                    DOTween.To(
                        () => _enemyAvatarSpriteImage.color,
                        x => _enemyAvatarSpriteImage.color = x,
                        originalColor, 0.15f)
                        .SetTarget(_enemyAvatarSpriteImage)
                        .SetUpdate(true);
                }
            }
        }

        // ────────────────────────────────────────────────────────
        // Hand limit toast
        // ────────────────────────────────────────────────────────

        private void OnPlayerHandLimitReached(int maxHandSize)
        {
            if (_handLimitText == null) return;
            if (_handLimitCooldownRemaining > 0f) return;

            _handLimitText.text = $"Hand limit: {maxHandSize}";
            _handLimitText.gameObject.SetActive(true);
            _handLimitText.transform.SetAsLastSibling();
            _handLimitTimer = _handLimitToastDuration;
            _handLimitCooldownRemaining = _handLimitToastCooldown;
            StartCoroutine(HandLimitCooldownRoutine());
        }

        private IEnumerator HandLimitCooldownRoutine()
        {
            while (_handLimitCooldownRemaining > 0f)
            {
                _handLimitCooldownRemaining -= Time.deltaTime;
                yield return null;
            }
            _handLimitCooldownRemaining = 0f;
        }

        // ────────────────────────────────────────────────────────
        // Panel flash (energy/world panels cuando accion invalida)
        // ────────────────────────────────────────────────────────

        /// <summary>
        /// Flash visual en un panel para indicar acción inválida (ej. sin energía).
        /// Público para que CombatUIController pueda delegarlo desde OnEndTurnButtonClicked
        /// y OnChangeWorldButtonClicked.
        /// </summary>
        public void FlashPanel(Image panelImage)
        {
            if (panelImage == null) return;
            if (_panelFlashRoutine != null)
            {
                StopCoroutine(_panelFlashRoutine);
            }
            _panelFlashRoutine = StartCoroutine(FlashPanelRoutine(panelImage));
        }

        private IEnumerator FlashPanelRoutine(Image panelImage)
        {
            Color original = panelImage.color;
            panelImage.color = HudFlashColor;
            yield return new WaitForSeconds(0.12f);
            panelImage.color = original;
            _panelFlashRoutine = null;
        }

        // Accesores para que CombatUIController delegue FlashPanel en los paneles correctos.
        public Image EnergyPanelImage => _energyPanelImage;
        public Image WorldPanelImage => _worldPanelImage;

        // ────────────────────────────────────────────────────────
        // Victory / Defeat text
        // ────────────────────────────────────────────────────────

        /// <summary>
        /// Muestra texto animado "VICTORY" centrado en pantalla.
        /// </summary>
        public void ShowVictoryText()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.VictorySFX);
            ShowCombatResultText("VICTORY", new Color(0.2f, 1f, 0.4f));
        }

        /// <summary>
        /// Muestra texto animado "DEFEAT" centrado en pantalla.
        /// </summary>
        public void ShowDefeatText()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.DefeatSFX);
            ShowCombatResultText("DEFEAT", new Color(1f, 0.3f, 0.3f));
        }

        private void ShowCombatResultText(string message, Color color)
        {
            if (_canvas == null) return;

            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();

            // Crea texto temporal con animación scale-in + fade-in, auto-destrucción a 2s.
            GameObject textGO = new GameObject("CombatResultText", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(canvasRect, false);

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.3f);
            textRect.anchorMax = new Vector2(0.9f, 0.7f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text resultText = textGO.GetComponent<Text>();
            resultText.font = _uiFont;
            resultText.fontSize = 60;
            resultText.alignment = TextAnchor.MiddleCenter;
            resultText.fontStyle = FontStyle.Bold;
            resultText.color = color;
            resultText.text = message;

            CanvasGroup group = textGO.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            UIAnimationHelper.ScaleIn(textRect.transform, 0.4f);
            UIAnimationHelper.FadeIn(group, 0.3f);

            Destroy(textGO, 2f);
        }

        // ────────────────────────────────────────────────────────
        // Timers internos
        // ────────────────────────────────────────────────────────

        private void UpdateHitFeedbackTimer()
        {
            if (_hitFeedbackText == null || !_hitFeedbackText.gameObject.activeSelf) return;

            _hitFeedbackTimer -= Time.deltaTime;
            if (_hitFeedbackTimer <= 0f)
            {
                _hitFeedbackText.gameObject.SetActive(false);
            }
        }

        private void UpdateHandLimitTimer()
        {
            if (_handLimitText == null || !_handLimitText.gameObject.activeSelf) return;

            _handLimitTimer -= Time.deltaTime;
            if (_handLimitTimer <= 0f)
            {
                _handLimitText.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (_playerAvatarImage != null) DOTween.Kill(_playerAvatarImage.transform);
            if (_enemyAvatarImage != null) DOTween.Kill(_enemyAvatarImage.transform);
            if (_playerHpLabel != null) DOTween.Kill(_playerHpLabel);
            if (_enemyAvatarSpriteImage != null) DOTween.Kill(_enemyAvatarSpriteImage);
        }
    }
}

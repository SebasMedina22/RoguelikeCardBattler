using UnityEngine;
using UnityEngine.UI;
using RoguelikeCardBattler.Core.Audio;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Gameplay.Combat
{
    /// <summary>
    /// Maneja todos los textos y botones del HUD de combate: energía, Contador de Estilo,
    /// HP, bloqueo, intent del enemigo, tipo, mundo, switches, draw/discard, y los
    /// botones End Turn / Change World. También controla el highlight del avatar
    /// según el turno activo.
    ///
    /// Extraído de CombatUIController como parte de la descomposición en componentes
    /// independientes (ver Docs/dev/COMBAT_ARCHITECTURE.md — Fase 3).
    ///
    /// Este componente es SOLO presentación — nunca muta estado de gameplay.
    /// A diferencia de CombatFeedbackView, NO se suscribe a eventos: hace polling
    /// vía Sync() invocado por CombatUIController.Update() cada frame, igual que
    /// el código original pre-extracción.
    /// </summary>
    public class CombatHudView : MonoBehaviour
    {
        // ── Referencias inyectadas por CombatUIController.InitializeExtractedViews() ──
        private TurnManager _turnManager;

        // Textos del HUD
        private Text _playerEnergyText;
        private Text _styleChargesText;
        private Text _playerHpLabel;
        private Text _enemyHpLabel;
        private Text _enemyTypeLabel;
        private Text _playerBlockText;
        private Text _enemyBlockText;
        private Text _drawPileText;
        private Text _discardPileText;
        private Text _enemyIntentText;
        private Text _worldLabel;
        private Text _playerTypeText;
        private Text _worldSwitchesText;

        // Botones
        private Button _endTurnButton;
        private Text _endTurnLabel;
        private Button _changeWorldButton;
        private Text _changeWorldLabel;

        // Imágenes
        private Image _playerBlockOverlay;
        private Image _enemyBlockOverlay;
        private Image _playerAvatarImage;
        private Image _enemyAvatarImage;
        private Image _energyPanelImage;
        private Image _worldPanelImage;

        // Cross-refs
        private CombatFeedbackView _feedbackView;
        private CardHandView _cardHandView;

        // Constantes de estilo (movidas desde CombatUIController)
        private static readonly Color DisabledLabelColor = new Color(0.75f, 0.75f, 0.75f, 0.7f);
        private static readonly Color WarningLabelColor = new Color(1f, 0.6f, 0.6f, 1f);

        // Color de overlay de bloqueo (inyectado, definido en CombatUIController.SerializeField)
        private Color _blockOverlayColor;

        // Colores base de avatares, capturados al Initialize.
        private Color _playerAvatarBaseColor;
        private Color _enemyAvatarBaseColor;

        private bool _initialized;

        /// <summary>
        /// Inyecta todas las referencias necesarias. Llamado por CombatUIController
        /// después de BuildUI(). Sin esta llamada, el componente no hace nada.
        /// </summary>
        public void Initialize(
            TurnManager turnManager,
            // Textos
            Text playerEnergyText, Text styleChargesText,
            Text playerHpLabel, Text enemyHpLabel, Text enemyTypeLabel,
            Text playerBlockText, Text enemyBlockText,
            Text drawPileText, Text discardPileText,
            Text enemyIntentText,
            Text worldLabel, Text playerTypeText, Text worldSwitchesText,
            // Botones
            Button endTurnButton, Text endTurnLabel,
            Button changeWorldButton, Text changeWorldLabel,
            // Imágenes
            Image playerBlockOverlay, Image enemyBlockOverlay,
            Image playerAvatarImage, Image enemyAvatarImage,
            Image energyPanelImage, Image worldPanelImage,
            // Refs cruzadas
            CombatFeedbackView feedbackView,
            CardHandView cardHandView,
            // Constante
            Color blockOverlayColor)
        {
            _turnManager = turnManager;
            _playerEnergyText = playerEnergyText;
            _styleChargesText = styleChargesText;
            _playerHpLabel = playerHpLabel;
            _enemyHpLabel = enemyHpLabel;
            _enemyTypeLabel = enemyTypeLabel;
            _playerBlockText = playerBlockText;
            _enemyBlockText = enemyBlockText;
            _drawPileText = drawPileText;
            _discardPileText = discardPileText;
            _enemyIntentText = enemyIntentText;
            _worldLabel = worldLabel;
            _playerTypeText = playerTypeText;
            _worldSwitchesText = worldSwitchesText;
            _endTurnButton = endTurnButton;
            _endTurnLabel = endTurnLabel;
            _changeWorldButton = changeWorldButton;
            _changeWorldLabel = changeWorldLabel;
            _playerBlockOverlay = playerBlockOverlay;
            _enemyBlockOverlay = enemyBlockOverlay;
            _playerAvatarImage = playerAvatarImage;
            _enemyAvatarImage = enemyAvatarImage;
            _energyPanelImage = energyPanelImage;
            _worldPanelImage = worldPanelImage;
            _feedbackView = feedbackView;
            _cardHandView = cardHandView;
            _blockOverlayColor = blockOverlayColor;

            // Capturamos el color base de los avatares aquí — antes vivía en CombatUIController.
            // Se usa para dim/restore en UpdateAvatarHighlight cada frame.
            if (_playerAvatarImage != null) _playerAvatarBaseColor = _playerAvatarImage.color;
            if (_enemyAvatarImage != null) _enemyAvatarBaseColor = _enemyAvatarImage.color;

            // Listeners de los botones: se agregan aquí, no en BuildUI, para mantener
            // toda la responsabilidad del HUD encapsulada en este view.
            if (_endTurnButton != null)
            {
                _endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
            }
            if (_changeWorldButton != null)
            {
                _changeWorldButton.onClick.AddListener(OnChangeWorldButtonClicked);
            }

            _initialized = true;
        }

        // ────────────────────────────────────────────────────────
        // Polling: CombatUIController.Update() llama a Sync() cada frame.
        // Refleja el estado actual del TurnManager en los textos del HUD.
        // ────────────────────────────────────────────────────────

        public void Sync()
        {
            if (!_initialized || _turnManager == null) return;

            _playerEnergyText.text = $"Energy {_turnManager.PlayerEnergy}/{_turnManager.PlayerMaxEnergy}";
            if (_styleChargesText != null)
            {
                _styleChargesText.text = $"Estilo: {_turnManager.StyleCharges}/5";
            }
            if (_worldSwitchesText != null)
            {
                string switchesLabel = _turnManager.DebugUnlimitedWorldSwitches
                    ? "Switches: ∞"
                    : $"Switches: {_turnManager.WorldSwitchesUsed}/{_turnManager.TotalAvailableWorldSwitches}";
                _worldSwitchesText.text = switchesLabel;
            }
            _playerHpLabel.text = $"{_turnManager.PlayerHP}/{_turnManager.PlayerMaxHP}";
            _enemyHpLabel.text = $"{_turnManager.EnemyHP}/{_turnManager.EnemyMaxHP}";
            _enemyTypeLabel.text = BuildEnemyTypeLabel();
            EnemyDefinition enemyDef = _turnManager.CurrentEnemyDefinition;
            if (enemyDef != null && enemyDef.IsTransdimensional)
            {
                // Dos tipos: el color lo aporta el rich-text inline por tipo, así que
                // el label debe ir en blanco (un único Text no puede tener dos .color).
                _enemyTypeLabel.color = Color.white;
            }
            else
            {
                ElementType enemyType = _turnManager.EnemyElementType;
                _enemyTypeLabel.color = enemyType == ElementType.None
                    ? Color.white
                    : ElementTypeColors.ReadableOnDark(enemyType);
            }
            _drawPileText.text = $"Draw: {_turnManager.PlayerDrawPileCount}";
            _discardPileText.text = $"Discard: {_turnManager.PlayerDiscardPileCount}";
            _enemyIntentText.text = BuildEnemyIntentLabel();
            UpdateBlockVisuals(_playerBlockText, _playerBlockOverlay, _turnManager.PlayerBlock);
            UpdateBlockVisuals(_enemyBlockText, _enemyBlockOverlay, _turnManager.EnemyBlock);

            // Label de mundo: el texto se actualiza acá; los fondos (sky/ground)
            // los maneja CombatBackgroundView con polling autónomo.
            if (_worldLabel != null)
            {
                string worldLabel = _turnManager.CurrentWorld == TurnManager.WorldSide.A ? "A" : "B";
                _worldLabel.text = $"World: {worldLabel}";
            }

            if (_playerTypeText != null)
            {
                ElementType activeType = _turnManager.PlayerActiveType;
                _playerTypeText.text = activeType == ElementType.None ? "Tipo: —" : $"Tipo: {activeType}";
                // Tinte por tipo (C8): el label del tipo activo del jugador toma su color.
                _playerTypeText.color = activeType == ElementType.None
                    ? Color.white
                    : ElementTypeColors.ReadableOnDark(activeType);
            }

            bool playerTurn = _turnManager.IsPlayerTurn();
            _endTurnButton.interactable = playerTurn && !_turnManager.IsCombatFinished;
            if (_endTurnLabel != null)
            {
                _endTurnLabel.color = _endTurnButton.interactable ? Color.white : DisabledLabelColor;
            }
            bool worldSwitchAvailable = _turnManager.DebugUnlimitedWorldSwitches
                || _turnManager.WorldSwitchesUsed < _turnManager.TotalAvailableWorldSwitches;
            _changeWorldButton.interactable = playerTurn
                && !_turnManager.IsCombatFinished
                && worldSwitchAvailable;
            if (_changeWorldLabel != null)
            {
                _changeWorldLabel.color = _changeWorldButton.interactable ? Color.white : DisabledLabelColor;
            }
            if (_worldSwitchesText != null)
            {
                _worldSwitchesText.color = worldSwitchAvailable ? Color.white : WarningLabelColor;
            }
            UpdateAvatarHighlight(playerTurn);
        }

        // ────────────────────────────────────────────────────────
        // Highlight de avatares: el del turno activo a color base, el otro atenuado.
        // ────────────────────────────────────────────────────────

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

        // ────────────────────────────────────────────────────────
        // Helpers de etiquetas (intent, tipo) y overlay de bloqueo.
        // ────────────────────────────────────────────────────────

        private string BuildEnemyIntentLabel()
        {
            EnemyIntentType intentType = _turnManager?.PlannedEnemyIntentType ?? EnemyIntentType.Unknown;
            int value = _turnManager?.PlannedEnemyIntentValue ?? 0;

            return intentType switch
            {
                EnemyIntentType.Attack when value > 0 => $"ATTACK {value}",
                EnemyIntentType.Defend when value > 0 => $"DEFEND {value}",
                EnemyIntentType.Attack => "ATTACK",
                EnemyIntentType.Defend => "DEFEND",
                _ => "?"
            };
        }

        // Factor de atenuación del tipo del mundo inactivo en la ficha transdim.
        private const float TransdimDimFactor = 0.45f;

        private string BuildEnemyTypeLabel()
        {
            EnemyDefinition def = _turnManager?.CurrentEnemyDefinition;
            if (_turnManager == null || def == null)
            {
                return "Type: —";
            }

            if (!def.IsTransdimensional)
            {
                // Tipo único o ancla → un solo tipo (resuelto por el getter).
                ElementType type = _turnManager.EnemyElementType;
                return type == ElementType.None ? "Type: —" : $"Type: {type}";
            }

            // Transdimensional → dos tipos: el del mundo activo a color pleno, el otro atenuado.
            bool activeIsA = _turnManager.CurrentWorld == TurnManager.WorldSide.A;
            string prefixA = activeIsA
                ? ElementTypeColors.TypePrefix(def.ElementType)
                : ElementTypeColors.TypePrefix(def.ElementType, TransdimDimFactor);
            string prefixB = activeIsA
                ? ElementTypeColors.TypePrefix(def.TypeWorldB, TransdimDimFactor)
                : ElementTypeColors.TypePrefix(def.TypeWorldB);
            return $"Type: {prefixA} / {prefixB}";
        }

        private void UpdateBlockVisuals(Text label, Image overlay, int blockValue)
        {
            if (label != null)
            {
                label.text = $"Block {blockValue}";
            }

            if (overlay != null)
            {
                overlay.color = new Color(
                    _blockOverlayColor.r,
                    _blockOverlayColor.g,
                    _blockOverlayColor.b,
                    blockValue > 0 ? _blockOverlayColor.a : 0f);
            }
        }

        // ────────────────────────────────────────────────────────
        // Handlers de botones: invocados por los listeners agregados en Initialize.
        // ────────────────────────────────────────────────────────

        private void OnEndTurnButtonClicked()
        {
            if (_turnManager == null || !_turnManager.IsPlayerTurn())
            {
                // Click inválido: flash rojo del energy panel para feedback visual.
                _feedbackView?.FlashPanel(_energyPanelImage);
                return;
            }

            _turnManager.EndPlayerTurn();
        }

        private void OnChangeWorldButtonClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.WorldChangeSFX);
            if (_turnManager == null)
            {
                return;
            }

            bool changed = _turnManager.TryChangeWorld();
            // El label de mundo se actualiza en el próximo Sync().
            // Los fondos (sky/ground) los detecta CombatBackgroundView por polling
            // autónomo — no hace falta ninguna llamada explícita desde aquí.
            _cardHandView?.SyncHandButtons(forceRebuild: true);
            if (!changed)
            {
                _feedbackView?.FlashPanel(_worldPanelImage);
            }
        }

        // ────────────────────────────────────────────────────────
        // Cleanup defensivo: removemos los listeners al destruir el componente.
        // ────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            if (_endTurnButton != null)
            {
                _endTurnButton.onClick.RemoveListener(OnEndTurnButtonClicked);
            }
            if (_changeWorldButton != null)
            {
                _changeWorldButton.onClick.RemoveListener(OnChangeWorldButtonClicked);
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;

namespace RoguelikeCardBattler.Run.NewRun
{
    /// <summary>
    /// Configuración de la pantalla de arranque de run (NewRunScene, Sub-PR 3E).
    /// Espejo de <c>ShopConfig</c>: mantiene fuera del código el pool de caras
    /// para el draft, los tipos seleccionables y los parámetros de feel, para que
    /// el contenido y el balance se ajusten sin recompilar.
    ///
    /// El pool de caras (<see cref="DraftFaces"/>) son cartas SIMPLES tipadas: el
    /// jugador elige una cara filtrada por el tipo del Mundo A y otra por el del
    /// Mundo B, y <c>StarterDraft.ComposeDualCard</c> las compone en una carta dual
    /// en runtime. El pool placeholder debe cubrir los 6 tipos con ≥3 caras por
    /// tipo (ver <c>NewRunConfigSetup</c>) para que cualquier elección rinda
    /// <see cref="OptionsPerWorld"/> opciones por columna.
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike/New Run Config", fileName = "NewRunConfig")]
    public class NewRunConfig : ScriptableObject
    {
        [Header("Pool de caras para el draft (cartas simples tipadas)")]
        [SerializeField] private List<CardDefinition> draftFaces = new List<CardDefinition>();

        [Header("Tipos elementales seleccionables (uno por mundo, deben ser distintos)")]
        [SerializeField] private List<ElementType> selectableTypes = new List<ElementType>
        {
            ElementType.Rojo,
            ElementType.Amarillo,
            ElementType.Azul,
            ElementType.Morado,
            ElementType.Negro,
            ElementType.Blanco
        };

        [Header("Draft")]
        [SerializeField, Min(1)] private int optionsPerWorld = 3;

        [Header("Feel")]
        [SerializeField, Tooltip("Sonido al confirmar la build. Si es null se usa el ClickSFX del AudioManager.")]
        private AudioClip confirmClip;

        public IReadOnlyList<CardDefinition> DraftFaces => draftFaces;
        public IReadOnlyList<ElementType> SelectableTypes => selectableTypes;
        public int OptionsPerWorld => Mathf.Max(1, optionsPerWorld);
        public AudioClip ConfirmClip => confirmClip;

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        /// <summary>
        /// Setter de datos para tests EditMode (mismo patrón que
        /// <c>ShopConfig.SetDebugData</c>). Permite armar un NewRunConfig sin asset
        /// en disco.
        /// </summary>
        public void SetDebugData(
            List<CardDefinition> faces,
            List<ElementType> types,
            int optionsPerWorldValue)
        {
            draftFaces = faces ?? new List<CardDefinition>();
            selectableTypes = types ?? new List<ElementType>();
            optionsPerWorld = optionsPerWorldValue;
        }
#endif

#if UNITY_EDITOR
        /// <summary>
        /// Puebla SÓLO el pool de caras desde el editor tooling
        /// (NewRunConfigSetup). Deja tipos seleccionables / optionsPerWorld /
        /// confirmClip intactos para no pisar ajustes manuales. Idempotente:
        /// reemplaza el contenido del pool en cada corrida.
        /// </summary>
        public void EditorPopulateFaces(List<CardDefinition> faces)
        {
            draftFaces = faces ?? new List<CardDefinition>();
        }
#endif
    }
}

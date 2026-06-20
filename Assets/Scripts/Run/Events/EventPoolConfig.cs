using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeCardBattler.Run.Events
{
    /// <summary>
    /// Pool de eventos del acto. <see cref="EventResolver.SelectEvent"/> elige uno
    /// de forma determinista por nodo+seed (espejo de cómo <c>EnemyPoolConfig</c>
    /// alimenta a <c>RunMapGenerator.AssignEnemies</c>). El sprite de fondo es
    /// opcional: el panel usa un color de fallback si es null (la feature funciona
    /// antes de tener arte).
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike/Event Pool Config", fileName = "EventPoolConfig")]
    public class EventPoolConfig : ScriptableObject
    {
        [SerializeField] private List<EventDefinition> events = new List<EventDefinition>();
        [Header("Fondo del panel (opcional; fallback de color si null)")]
        [SerializeField] private Sprite backgroundSprite;

        public IReadOnlyList<EventDefinition> Events => events;
        public Sprite BackgroundSprite => backgroundSprite;

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        /// <summary>Setter de datos para tests EditMode (sin asset en disco).</summary>
        public void SetDebugData(List<EventDefinition> newEvents)
        {
            events = newEvents ?? new List<EventDefinition>();
        }
#endif

#if UNITY_EDITOR
        /// <summary>
        /// Puebla SÓLO el pool de eventos desde el editor tooling (EventConfigSetup).
        /// Deja el sprite intacto. Idempotente: reemplaza el contenido en cada corrida.
        /// </summary>
        public void EditorPopulateEvents(List<EventDefinition> newEvents)
        {
            events = newEvents ?? new List<EventDefinition>();
        }
#endif
    }
}

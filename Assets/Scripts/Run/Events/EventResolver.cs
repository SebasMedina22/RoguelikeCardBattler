using System.Collections.Generic;

namespace RoguelikeCardBattler.Run.Events
{
    /// <summary>
    /// Selección determinista de eventos por nodo y resolución de variantes
    /// multidimensionales. Static y sin UI (testeable).
    /// </summary>
    public static class EventResolver
    {
        /// <summary>
        /// Devuelve el EventDefinition asignado al nodo <paramref name="nodeId"/>
        /// para la <paramref name="seed"/> dada. Determinista: (seed, nodeId) → mismo
        /// evento siempre. Devuelve null si el pool es nulo o vacío.
        /// </summary>
        public static EventDefinition SelectEvent(EventPoolConfig pool, int nodeId, int seed)
        {
            if (pool == null) return null;

            var events = pool.Events;
            if (events == null || events.Count == 0) return null;

            // Combinar seed y nodeId en un único seed por nodo: la misma run (seed)
            // fija el evento de cada nodo; nodos distintos divergen entre sí y runs
            // distintas (seed distinta) reordenan todo.
            int combined = unchecked(seed * 397) ^ nodeId;
            System.Random rng = new System.Random(combined);
            int index = rng.Next(events.Count);
            return events[index];
        }

        /// <summary>
        /// Devuelve las choices de la variante elegida para un evento multidimensional.
        /// <paramref name="world"/> 0=A / 1=B. Si la definición no es multidimensional o
        /// es null, devuelve las Choices raíz del EventDefinition.
        /// </summary>
        public static IReadOnlyList<EventChoice> ResolveVariant(EventDefinition def, int world)
        {
            if (def == null) return null;
            if (!def.IsMultidimensional) return def.Choices;
            EventVariant variant = world == 0 ? def.WorldA : def.WorldB;
            return variant?.Choices;
        }

        /// <summary>
        /// Devuelve la variante completa (Body + Choices) para un evento multidimensional.
        /// Null si la definición no es multidimensional.
        /// </summary>
        public static EventVariant ResolveVariantFull(EventDefinition def, int world)
        {
            if (def == null || !def.IsMultidimensional) return null;
            return world == 0 ? def.WorldA : def.WorldB;
        }
    }
}

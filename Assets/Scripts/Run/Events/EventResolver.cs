namespace RoguelikeCardBattler.Run.Events
{
    /// <summary>
    /// Selección determinista de eventos por nodo. Static y sin UI (testeable):
    /// la misma seed fija el evento de cada nodo al generar el mapa (mismo patrón
    /// que <c>RunMapGenerator.AssignEnemies</c>), una seed distinta produce otra
    /// distribución.
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
    }
}

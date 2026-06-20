namespace RoguelikeCardBattler.Run.Quests
{
    /// <summary>
    /// Estado runtime del quest activo en el run. Solo datos — la BFS de resolución
    /// del destino vive en <see cref="QuestDestinationResolver"/> (Golden Rule §7:
    /// RunState = solo datos). Reset en <c>RunState.Reset</c>.
    /// </summary>
    public class QuestState
    {
        public bool Active { get; set; }
        // Id del nodo que el jugador debe alcanzar para completar el quest.
        // Resuelto en runtime al aceptar (no autorado en el SO), garantizado alcanzable
        // por BFS forward. -1 = sin quest activo.
        public int DestinationNodeId { get; set; } = -1;
        public int FinalRewardGold { get; set; }
        // "A" o "B" según el mundo elegido al aceptar el quest (flavor para el mapa).
        public string SourceWorldLabel { get; set; }
    }
}

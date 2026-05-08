using System.Collections.Generic;

namespace RoguelikeCardBattler.Gameplay.Relics
{
    /// <summary>
    /// Wrapper runtime de un RelicDefinition. Vive en RunState.Relics.
    /// AcquisitionOrder define el orden de ejecución en el dispatcher (asc).
    /// Counters son estado mutable per-instance cuyo lifecycle queda en manos
    /// del propio Retazo: el dispatcher NO los toca (ni resetea, ni inicializa,
    /// ni inspecciona). Cada IRelicEffect es responsable del namespacing y del
    /// reset de sus claves (ej: limpiar en OnPlayerTurnStart si son por turno).
    /// </summary>
    public class RelicInstance
    {
        public RelicDefinition Definition { get; }
        public int AcquisitionOrder { get; }
        public Dictionary<string, int> Counters { get; }

        public IRelicEffect Effect => Definition != null ? Definition.Effect : null;

        public RelicInstance(RelicDefinition definition, int acquisitionOrder)
        {
            Definition = definition;
            AcquisitionOrder = acquisitionOrder;
            Counters = new Dictionary<string, int>();
        }
    }
}

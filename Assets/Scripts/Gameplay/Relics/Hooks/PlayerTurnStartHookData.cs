using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Gameplay.Relics.Hooks
{
    /// <summary>
    /// Disparado dentro de BeginPlayerTurn, después de ResetEnergy + ClearBlock
    /// y antes de DrawCards. Permite Retazos que modifican draw size, energía
    /// extra, etc. Observable: no muta campos.
    /// Nota (3A): los Retazos que necesiten contar turnos mantienen su propio
    /// contador en RelicInstance.Counters (lifecycle del propio Retazo).
    /// </summary>
    public class PlayerTurnStartHookData : RelicHookContext
    {
        public TurnManager.WorldSide CurrentWorld { get; }

        public PlayerTurnStartHookData(
            RunState runState,
            TurnManager turnManager,
            RelicHookDispatcher dispatcher,
            TurnManager.WorldSide currentWorld)
            : base(runState, turnManager, dispatcher)
        {
            CurrentWorld = currentWorld;
        }
    }
}

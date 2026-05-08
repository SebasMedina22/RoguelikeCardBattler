using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Gameplay.Relics.Hooks
{
    /// <summary>
    /// Disparado dentro de TryChangeWorld, después de mutar currentWorld y antes
    /// de incrementar _worldSwitchesUsed. Observable: From/To del cambio.
    /// Nota (3A): WasFreeSwitch removido — el pool base+bonus es fungible en
    /// TryChangeWorld y el campo era semánticamente ambiguo.
    /// </summary>
    public class WorldSwitchHookData : RelicHookContext
    {
        public TurnManager.WorldSide From { get; }
        public TurnManager.WorldSide To { get; }

        public WorldSwitchHookData(
            RunState runState,
            TurnManager turnManager,
            RelicHookDispatcher dispatcher,
            TurnManager.WorldSide from,
            TurnManager.WorldSide to)
            : base(runState, turnManager, dispatcher)
        {
            From = from;
            To = to;
        }
    }
}

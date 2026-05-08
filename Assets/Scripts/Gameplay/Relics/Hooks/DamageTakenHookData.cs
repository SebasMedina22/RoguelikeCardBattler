using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Gameplay.Relics.Hooks
{
    /// <summary>
    /// Daño enemigo → jugador, post-efectividad y antes de encolar el DamageAction.
    /// Mutable: los Retazos defensivos ("daño recibido -1") modifican Amount.
    /// Eff y AttackerType son contexto observable.
    /// </summary>
    public class DamageTakenHookData : RelicHookContext
    {
        public int Amount;

        public Effectiveness Eff { get; }
        public ElementType AttackerType { get; }
        public ICombatActor Source { get; }

        public DamageTakenHookData(
            RunState runState,
            TurnManager turnManager,
            RelicHookDispatcher dispatcher,
            int amount,
            Effectiveness eff,
            ElementType attackerType,
            ICombatActor source)
            : base(runState, turnManager, dispatcher)
        {
            Amount = amount;
            Eff = eff;
            AttackerType = attackerType;
            Source = source;
        }
    }
}

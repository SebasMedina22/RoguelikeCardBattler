using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Gameplay.Relics.Hooks
{
    /// <summary>
    /// Daño jugador → enemigo, post-efectividad y antes de encolar el DamageAction.
    /// Mutable: los Retazos pueden modificar Amount; las mutaciones se encadenan
    /// en orden de adquisición (asc) y el valor final es el que se pasa a
    /// new DamageAction(...). Eff y AttackerType son contexto observable.
    /// </summary>
    public class DamageDealtHookData : RelicHookContext
    {
        // Campo público mutable: los Retazos modifican esto en su OnHook.
        public int Amount;

        public Effectiveness Eff { get; }
        public ElementType AttackerType { get; }
        public ICombatActor Target { get; }

        public DamageDealtHookData(
            RunState runState,
            TurnManager turnManager,
            RelicHookDispatcher dispatcher,
            int amount,
            Effectiveness eff,
            ElementType attackerType,
            ICombatActor target)
            : base(runState, turnManager, dispatcher)
        {
            Amount = amount;
            Eff = eff;
            AttackerType = attackerType;
            Target = target;
        }
    }
}

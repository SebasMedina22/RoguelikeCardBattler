using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Gameplay.Relics.Hooks
{
    /// <summary>
    /// Payload base que reciben todos los IRelicEffect. Cada *HookData extiende
    /// esta clase y agrega campos específicos del evento (ver spec §Payloads).
    /// La API de mutación es deliberadamente limitada: los Retazos NO reciben el
    /// TurnManager entero, sino métodos puntuales que delegan a métodos internal
    /// del TurnManager con guards (Victory/Defeat → no-op). Ampliar la API
    /// requiere nueva sub-PR + actualización del spec ([CERRADO 4]).
    /// </summary>
    public class RelicHookContext
    {
        public RunState RunState { get; }
        public TurnManager TurnManager { get; }
        public RelicHookDispatcher Dispatcher { get; }

        // El dispatcher setea esto antes de cada invocación a OnHook.
        // Se expone público para que un Retazo pueda inspeccionar sus propios
        // Counters durante el handler (ctx.CurrentRelic.Counters["..."]).
        public RelicInstance CurrentRelic { get; internal set; }

        protected RelicHookContext(RunState runState, TurnManager turnManager, RelicHookDispatcher dispatcher)
        {
            RunState = runState;
            TurnManager = turnManager;
            Dispatcher = dispatcher;
        }

        // API limitada de mutaciones permitidas a Retazos. Cada método es un
        // pasamanos a TurnManager: el TurnManager aplica guards (Victory/Defeat)
        // y la mutación real sobre el estado de combate. Si TurnManager es null
        // (hook fuera de combate, ej. nodos en 3C/3D), los Grant* son no-op
        // silenciosos — el Retazo escribe directo a RunState para esos casos.

        public void GrantBlock(ICombatActor actor, int amount)
        {
            if (TurnManager == null || actor == null || amount <= 0) return;
            TurnManager.RelicGrantBlock(actor, amount);
        }

        public void GrantHeal(ICombatActor actor, int amount)
        {
            if (TurnManager == null || actor == null || amount <= 0) return;
            TurnManager.RelicGrantHeal(actor, amount);
        }

        public void GrantDrawCards(ICombatActor actor, int amount)
        {
            if (TurnManager == null || actor == null || amount <= 0) return;
            TurnManager.RelicGrantDrawCards(actor, amount);
        }

        public void GrantEnergy(ICombatActor actor, int amount)
        {
            if (TurnManager == null || actor == null || amount <= 0) return;
            TurnManager.RelicGrantEnergy(actor, amount);
        }

        public void GrantStyleCharge(int amount)
        {
            if (TurnManager == null || amount <= 0) return;
            TurnManager.RelicGrantStyleCharge(amount);
        }

        public void GrantBonusWorldSwitch()
        {
            if (TurnManager == null) return;
            TurnManager.RelicGrantBonusWorldSwitch();
        }

        public void EnqueueExtraDamage(ICombatActor target, int amount, ElementType type)
        {
            if (TurnManager == null || target == null || amount <= 0) return;
            TurnManager.RelicEnqueueExtraDamage(target, amount, type);
        }
    }
}

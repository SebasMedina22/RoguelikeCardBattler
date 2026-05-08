using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 6 — R-TURN-3 "Reloj de Cocina": cada Period turnos, +Amount energía.
    // Counter "r-6:turns" se incrementa cada PlayerTurnStart; al llegar a Period
    // se otorga energía y se resetea. Vive per-instance en RelicInstance.Counters.
    [Serializable]
    public class RelicTurnEnergyEveryNEffect : IRelicEffect
    {
        public int Period = 3;
        public int Amount = 1;

        private const string CounterKey = "r-6:turns";

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnPlayerTurnStart) return;
            int turns = ctx.CurrentRelic.Counters.TryGetValue(CounterKey, out int v) ? v : 0;
            turns++;
            if (Period > 0 && turns >= Period)
            {
                // TurnManager puede ser null en tests; el counter avanza igual
                if (ctx.TurnManager != null)
                    ctx.GrantEnergy(ctx.TurnManager.Player, Amount);
                turns = 0;
            }
            ctx.CurrentRelic.Counters[CounterKey] = turns;
        }
    }
}

using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 8 — R-DMG-2 "Sorpresa Guardada": +Amount al primer ataque del combate.
    // Counter se resetea en OnCombatStart y se setea en el primer OnDamageDealt.
    [Serializable]
    public class RelicDmgFirstHitEffect : IRelicEffect
    {
        public int Amount = 5;

        private const string CounterKey = "r-8:hit";

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook == RelicHook.OnCombatStart)
            {
                ctx.CurrentRelic.Counters[CounterKey] = 0;
                return;
            }
            if (hook != RelicHook.OnDamageDealt) return;
            int done = ctx.CurrentRelic.Counters.TryGetValue(CounterKey, out int v) ? v : 0;
            if (done != 0) return;
            DamageDealtHookData data = ctx as DamageDealtHookData;
            if (data == null) return;
            data.Amount += Amount;
            ctx.CurrentRelic.Counters[CounterKey] = 1;
        }
    }
}

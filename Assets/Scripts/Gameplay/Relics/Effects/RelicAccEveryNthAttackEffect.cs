using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 11 — R-ACC-2 "Tres en Raya": cada Period-ésimo ataque del combate +Amount.
    // Counter "r-11:attacks" se resetea en OnCombatStart e incrementa en OnDamageDealt.
    [Serializable]
    public class RelicAccEveryNthAttackEffect : IRelicEffect
    {
        public int Period = 3;
        public int Amount = 4;

        private const string CounterKey = "r-11:attacks";

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook == RelicHook.OnCombatStart)
            {
                ctx.CurrentRelic.Counters[CounterKey] = 0;
                return;
            }
            if (hook != RelicHook.OnDamageDealt) return;
            DamageDealtHookData data = ctx as DamageDealtHookData;
            if (data == null) return;
            int attacks = ctx.CurrentRelic.Counters.TryGetValue(CounterKey, out int v) ? v : 0;
            attacks++;
            if (Period > 0 && attacks % Period == 0)
            {
                data.Amount += Amount;
            }
            ctx.CurrentRelic.Counters[CounterKey] = attacks;
        }
    }
}

using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 23 — R-BOSS-1 "Hilo de Costura Maldita": al ganar un combate, el siguiente
    // arranca con +EnergyBonus energía y +BlockBonus bloque.
    // Counter "r-23:won" persiste entre combates dentro de RelicInstance.Counters
    // (no se resetea automáticamente por el dispatcher). Solo se resetea DESPUÉS de
    // consumirlo en OnCombatStart, garantizando que no se pierda entre escenas.
    [Serializable]
    public class RelicBossLastStitchEffect : IRelicEffect
    {
        public int EnergyBonus = 1;
        public int BlockBonus = 5;

        private const string CounterKey = "r-23:won";

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            var counters = ctx.CurrentRelic.Counters;
            if (hook == RelicHook.OnCombatEnd)
            {
                CombatEndHookData ce = ctx as CombatEndHookData;
                if (ce != null && ce.Victory)
                {
                    counters[CounterKey] = 1;
                }
                return;
            }
            if (hook == RelicHook.OnCombatStart && ctx.TurnManager != null)
            {
                int won = counters.TryGetValue(CounterKey, out int v) ? v : 0;
                if (won != 1) return;
                ctx.GrantEnergy(ctx.TurnManager.Player, EnergyBonus);
                ctx.GrantBlock(ctx.TurnManager.Player, BlockBonus);
                counters[CounterKey] = 0;
            }
        }
    }
}

using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 3 — R-OPEN-3 "Sorbo Robado": +Amount energía en T1.
    [Serializable]
    public class RelicOpenEnergyEffect : IRelicEffect
    {
        public int Amount = 1;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnCombatStart || ctx.TurnManager == null) return;
            ctx.GrantEnergy(ctx.TurnManager.Player, Amount);
        }
    }
}

using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 2 — R-OPEN-2 "Bolsillo Roto": +Amount cartas en mano inicial.
    // Respeta cap de mano: GrantDrawCards delega a actor.DrawCards (cumple cap).
    [Serializable]
    public class RelicOpenDrawEffect : IRelicEffect
    {
        public int Amount = 1;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnCombatStart || ctx.TurnManager == null) return;
            ctx.GrantDrawCards(ctx.TurnManager.Player, Amount);
        }
    }
}

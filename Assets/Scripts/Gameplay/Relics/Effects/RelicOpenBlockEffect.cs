using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 1 — R-OPEN-1 "Tapa de Caja de Galletas": +Amount bloque al iniciar combate.
    [Serializable]
    public class RelicOpenBlockEffect : IRelicEffect
    {
        public int Amount = 4;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnCombatStart || ctx.TurnManager == null) return;
            ctx.GrantBlock(ctx.TurnManager.Player, Amount);
        }
    }
}

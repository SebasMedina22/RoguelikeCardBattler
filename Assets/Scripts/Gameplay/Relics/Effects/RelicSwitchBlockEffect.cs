using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 16 — R-SW-1 "Escudo de Costuras" (Switch): +Amount bloque al cambiar.
    [Serializable]
    public class RelicSwitchBlockEffect : IRelicEffect
    {
        public int Amount = 5;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnWorldSwitch || ctx.TurnManager == null) return;
            ctx.GrantBlock(ctx.TurnManager.Player, Amount);
        }
    }
}

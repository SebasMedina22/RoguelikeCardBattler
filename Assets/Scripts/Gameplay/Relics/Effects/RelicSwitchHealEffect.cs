using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 15 — R-SW-4 "Aliento Entre Mundos": +Amount HP al cambiar de mundo.
    [Serializable]
    public class RelicSwitchHealEffect : IRelicEffect
    {
        public int Amount = 2;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnWorldSwitch || ctx.TurnManager == null) return;
            ctx.GrantHeal(ctx.TurnManager.Player, Amount);
        }
    }
}

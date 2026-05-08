using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 21 — R-ELITE-3 "Caja Intacta": victoria con HP completo → +Amount oro.
    [Serializable]
    public class RelicElitePuristEffect : IRelicEffect
    {
        public int Amount = 10;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnCombatEnd || ctx.RunState == null) return;
            CombatEndHookData data = ctx as CombatEndHookData;
            if (data == null || !data.Victory) return;
            if (ctx.RunState.PlayerCurrentHP < ctx.RunState.PlayerMaxHP) return;
            ctx.RunState.Gold += Amount;
        }
    }
}

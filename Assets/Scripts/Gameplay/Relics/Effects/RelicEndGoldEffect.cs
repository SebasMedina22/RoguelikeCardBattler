using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 12 — R-END-1 "Mochila de Botín": +Amount oro por victoria.
    [Serializable]
    public class RelicEndGoldEffect : IRelicEffect
    {
        public int Amount = 5;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnCombatEnd || ctx.RunState == null) return;
            CombatEndHookData data = ctx as CombatEndHookData;
            if (data == null || !data.Victory) return;
            ctx.RunState.Gold += Amount;
        }
    }
}

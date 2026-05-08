using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 14 — R-END-3 "Frasco de Confianza": +Amount oro por Elite ganado.
    [Serializable]
    public class RelicEndEliteGoldEffect : IRelicEffect
    {
        public int Amount = 10;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnCombatEnd || ctx.RunState == null) return;
            CombatEndHookData data = ctx as CombatEndHookData;
            if (data == null || !data.Victory || !data.IsElite) return;
            ctx.RunState.Gold += Amount;
        }
    }
}

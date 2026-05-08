using System;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 13 — R-END-2 "Curita con Estampa": +Amount HP por victoria.
    // Mutación directa de RunState (NO GrantHeal): post-victoria el actor puede
    // no estar referenciable; ver [CERRADO 3] del spec 3A.
    [Serializable]
    public class RelicEndHealEffect : IRelicEffect
    {
        public int Amount = 4;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnCombatEnd || ctx.RunState == null) return;
            CombatEndHookData data = ctx as CombatEndHookData;
            if (data == null || !data.Victory) return;
            int max = ctx.RunState.PlayerMaxHP;
            int current = ctx.RunState.PlayerCurrentHP;
            ctx.RunState.PlayerCurrentHP = max > 0 ? Mathf.Min(max, current + Amount) : current + Amount;
        }
    }
}

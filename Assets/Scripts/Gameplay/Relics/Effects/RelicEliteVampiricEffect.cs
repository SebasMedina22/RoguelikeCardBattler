using System;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 19 — R-ELITE-1 "Diente Afilado": al hacer SuperEficaz, heal Amount HP.
    [Serializable]
    public class RelicEliteVampiricEffect : IRelicEffect
    {
        public int Amount = 2;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnDamageDealt || ctx.TurnManager == null) return;
            DamageDealtHookData data = ctx as DamageDealtHookData;
            if (data == null || data.Eff != Effectiveness.SuperEficaz) return;
            ctx.GrantHeal(ctx.TurnManager.Player, Amount);
        }
    }
}

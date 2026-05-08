using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 22 — R-ELITE-4 "Ritmo Encendido": con ≥Threshold cargas de Estilo,
    // tus ataques hacen +Amount.
    [Serializable]
    public class RelicEliteChargeBoostEffect : IRelicEffect
    {
        public int Threshold = 3;
        public int Amount = 4;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnDamageDealt || ctx.TurnManager == null) return;
            DamageDealtHookData data = ctx as DamageDealtHookData;
            if (data == null) return;
            if (ctx.TurnManager.StyleCharges < Threshold) return;
            data.Amount += Amount;
        }
    }
}

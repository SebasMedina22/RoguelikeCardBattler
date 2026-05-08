using System;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 20 — R-ELITE-2 "Erizo de Cartón": al recibir daño, devolvés Amount raw.
    [Serializable]
    public class RelicEliteSpinesEffect : IRelicEffect
    {
        public int Amount = 3;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnDamageTaken) return;
            DamageTakenHookData data = ctx as DamageTakenHookData;
            if (data == null || data.Source == null) return;
            ctx.EnqueueExtraDamage(data.Source, Amount, ElementType.None);
        }
    }
}

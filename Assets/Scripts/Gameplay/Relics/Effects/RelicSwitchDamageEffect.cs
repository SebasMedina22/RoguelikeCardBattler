using System;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 18 — R-SW-3 "Onda Dimensional" (Switch): +Amount daño raw al cambiar.
    // EnqueueExtraDamage no aplica efectividad ni re-dispara OnDamageDealt.
    [Serializable]
    public class RelicSwitchDamageEffect : IRelicEffect
    {
        public int Amount = 5;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnWorldSwitch || ctx.TurnManager == null) return;
            ctx.EnqueueExtraDamage(ctx.TurnManager.Enemy, Amount, ElementType.None);
        }
    }
}

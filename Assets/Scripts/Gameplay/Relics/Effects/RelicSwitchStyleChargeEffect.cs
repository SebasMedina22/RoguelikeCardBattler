using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 17 — R-SW-2 "Estilo Doble" (Switch): +Amount carga(s) de Estilo al cambiar.
    // GrantStyleCharge aplica regla §4 (5 cargas → bonus switch + reset).
    [Serializable]
    public class RelicSwitchStyleChargeEffect : IRelicEffect
    {
        public int Amount = 1;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnWorldSwitch) return;
            ctx.GrantStyleCharge(Amount);
        }
    }
}

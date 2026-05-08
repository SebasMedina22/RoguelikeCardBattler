using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 7 — R-DMG-1 "Punta de Lápiz": +Amount daño a tus ataques.
    // Aplica a path tipado y neutral (8º dispatch del 3B).
    [Serializable]
    public class RelicDmgFlatBoostEffect : IRelicEffect
    {
        public int Amount = 2;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnDamageDealt) return;
            DamageDealtHookData data = ctx as DamageDealtHookData;
            if (data == null) return;
            data.Amount += Amount;
        }
    }
}

using System;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 9 — R-DMG-3 "Cinta de Embalar": -Amount al daño recibido (mínimo 0).
    [Serializable]
    public class RelicDmgReduceEffect : IRelicEffect
    {
        public int Amount = 1;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnDamageTaken) return;
            DamageTakenHookData data = ctx as DamageTakenHookData;
            if (data == null) return;
            data.Amount = Mathf.Max(0, data.Amount - Amount);
        }
    }
}

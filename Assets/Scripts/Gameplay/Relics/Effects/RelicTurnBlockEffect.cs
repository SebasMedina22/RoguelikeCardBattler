using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 5 — R-TURN-2 "Almohadón Reforzado": +Amount bloque cada turno.
    // Se otorga DESPUÉS de ClearBlock (hook se dispara post-clear), persiste el turno.
    [Serializable]
    public class RelicTurnBlockEffect : IRelicEffect
    {
        public int Amount = 3;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnPlayerTurnStart || ctx.TurnManager == null) return;
            ctx.GrantBlock(ctx.TurnManager.Player, Amount);
        }
    }
}

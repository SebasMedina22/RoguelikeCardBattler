using System;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 4 — R-TURN-1 "Mano de Más": +Amount cartas cada turno.
    [Serializable]
    public class RelicTurnDrawEffect : IRelicEffect
    {
        public int Amount = 1;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnPlayerTurnStart || ctx.TurnManager == null) return;
            ctx.GrantDrawCards(ctx.TurnManager.Player, Amount);
        }
    }
}

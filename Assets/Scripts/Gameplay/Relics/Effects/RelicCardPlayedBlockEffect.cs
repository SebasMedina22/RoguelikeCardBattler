using System;
using System.Linq;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // MCguffin Mundo B — "Disco de la resistencia": +Amount bloqueo al jugar una carta
    // de escudo. Pasivo incondicional al mundo (PlayedInWorld no se filtra: el mundo del
    // evento es elección local del encuentro, no estado persistente; spec §RelicCardPlayedBlockEffect).
    // Detección de "carta de escudo": convención del codebase = EffectType.Block en Effects.
    [Serializable]
    public class RelicCardPlayedBlockEffect : IRelicEffect
    {
        public int Amount = 1;

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            if (hook != RelicHook.OnCardPlayed || ctx.TurnManager == null) return;
            CardPlayedHookData cp = ctx as CardPlayedHookData;
            if (cp?.Card == null) return;
            bool isBlockCard = cp.Card.Effects.Any(e => e.effectType == EffectType.Block);
            if (!isBlockCard) return;
            ctx.GrantBlock(ctx.TurnManager.Player, Amount);
        }
    }
}

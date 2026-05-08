using System;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;

namespace RoguelikeCardBattler.Gameplay.Relics.Effects
{
    // Slot 10 — R-ACC-1 "Cuaderno Cuadriculado": cada Period skills jugadas en un
    // turno → +Amount energía al siguiente turno.
    // Counters: skills (skills jugados este turno), pending (energía a otorgar).
    // OnPlayerTurnStart consume pending y resetea skills.
    [Serializable]
    public class RelicAccSkillStackerEffect : IRelicEffect
    {
        public int Period = 3;
        public int Amount = 1;

        private const string SkillsKey = "r-10:skillsThisTurn";
        private const string PendingKey = "r-10:energyPending";

        public void OnHook(RelicHook hook, RelicHookContext ctx)
        {
            var counters = ctx.CurrentRelic.Counters;
            if (hook == RelicHook.OnCardPlayed)
            {
                CardPlayedHookData cp = ctx as CardPlayedHookData;
                if (cp == null || cp.Card == null) return;
                if (cp.Card.Type != CardType.Skill) return;
                int skills = counters.TryGetValue(SkillsKey, out int s) ? s : 0;
                skills++;
                if (Period > 0 && skills >= Period)
                {
                    int pending = counters.TryGetValue(PendingKey, out int p) ? p : 0;
                    counters[PendingKey] = pending + Amount;
                    skills = 0;
                }
                counters[SkillsKey] = skills;
                return;
            }
            if (hook == RelicHook.OnPlayerTurnStart && ctx.TurnManager != null)
            {
                int pending = counters.TryGetValue(PendingKey, out int p) ? p : 0;
                if (pending > 0) ctx.GrantEnergy(ctx.TurnManager.Player, pending);
                counters[PendingKey] = 0;
                counters[SkillsKey] = 0;
            }
        }
    }
}

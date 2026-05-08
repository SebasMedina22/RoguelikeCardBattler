using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Gameplay.Relics.Hooks
{
    /// <summary>
    /// Disparado dentro de ResolvePreparedCardPlay, después de ProcessAll y
    /// DiscardCard, antes de CheckCombatEndConditions ([CERRADO 2]). La carta
    /// ya se resolvió: el Retazo puede contar/encolar acciones extra (que se
    /// procesan en el siguiente ProcessAll natural del flujo) o mutar RunState.
    /// </summary>
    public class CardPlayedHookData : RelicHookContext
    {
        public CardDefinition Card { get; }
        public TurnManager.WorldSide PlayedInWorld { get; }
        public int EnergySpent { get; }

        public CardPlayedHookData(
            RunState runState,
            TurnManager turnManager,
            RelicHookDispatcher dispatcher,
            CardDefinition card,
            TurnManager.WorldSide playedInWorld,
            int energySpent)
            : base(runState, turnManager, dispatcher)
        {
            Card = card;
            PlayedInWorld = playedInWorld;
            EnergySpent = energySpent;
        }
    }
}

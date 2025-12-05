using System;
using RoguelikeCardBattler.Gameplay.Combat;
using UnityEngine;

namespace RoguelikeCardBattler.Gameplay.Cards
{
    [Serializable]
    public class CardDeckEntry
    {
        [SerializeField] private CardDefinition singleCard;
        [SerializeField] private DualCardDefinition dualCard;

        public CardDefinition SingleCard => singleCard;
        public DualCardDefinition DualCard => dualCard;

        public bool IsValid => singleCard != null || dualCard != null;

        public CardDefinition GetActiveCard(TurnManager.WorldSide worldSide)
        {
            if (dualCard != null)
            {
                return dualCard.GetSide(worldSide);
            }

            return singleCard;
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetSingleCard(CardDefinition card)
        {
            singleCard = card;
            dualCard = null;
        }

        public void SetDualCard(DualCardDefinition card)
        {
            dualCard = card;
            singleCard = null;
        }
#endif
    }
}


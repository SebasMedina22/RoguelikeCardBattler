using System;
using RoguelikeCardBattler.Gameplay.Combat;
using UnityEngine;

namespace RoguelikeCardBattler.Gameplay.Cards
{
    [Serializable]
    /// <summary>
    /// Entrada que puede apuntar a una carta simple o dual. Resuelve la activa
    /// según el WorldSide actual.
    /// </summary>
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

        public void SetSingleCardRuntime(CardDefinition card)
        {
            singleCard = card;
            dualCard = null;
        }

        public void SetDualCardRuntime(DualCardDefinition card)
        {
            dualCard = card;
            singleCard = null;
        }

        public static CardDeckEntry CreateSingle(CardDefinition card)
        {
            var entry = new CardDeckEntry();
            entry.SetSingleCardRuntime(card);
            return entry;
        }

        public static CardDeckEntry CreateDual(DualCardDefinition card)
        {
            var entry = new CardDeckEntry();
            entry.SetDualCardRuntime(card);
            return entry;
        }

        public CardDeckEntry Clone()
        {
            if (dualCard != null)
            {
                return CreateDual(dualCard);
            }

            return CreateSingle(singleCard);
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetSingleCard(CardDefinition card)
        {
            SetSingleCardRuntime(card);
        }

        public void SetDualCard(DualCardDefinition card)
        {
            SetDualCardRuntime(card);
        }
#endif
    }
}


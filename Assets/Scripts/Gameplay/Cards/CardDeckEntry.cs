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
        // No serializado: el flag es runtime-only por run; al guardar el run en
        // disco (no implementado) habrá que persistirlo, pero hoy no aplica.
        private bool _isUpgraded = false;

        public CardDefinition SingleCard => singleCard;
        public DualCardDefinition DualCard => dualCard;
        public bool IsUpgraded => _isUpgraded;

        public bool IsValid => singleCard != null || dualCard != null;

        /// <summary>
        /// Una entrada se puede mejorar si aún no fue mejorada y al menos uno de
        /// sus lados (single, o sideA/sideB de dual) define datos de upgrade.
        /// </summary>
        public bool CanUpgrade()
        {
            if (_isUpgraded) return false;
            if (singleCard != null)
            {
                return singleCard.Upgrade != null && singleCard.Upgrade.HasUpgrade;
            }
            if (dualCard != null)
            {
                bool aHas = dualCard.SideA != null && dualCard.SideA.Upgrade != null && dualCard.SideA.Upgrade.HasUpgrade;
                bool bHas = dualCard.SideB != null && dualCard.SideB.Upgrade != null && dualCard.SideB.Upgrade.HasUpgrade;
                return aHas || bHas;
            }
            return false;
        }

        /// <summary>
        /// Reemplaza la referencia interna con la versión clonada/mejorada y
        /// marca <see cref="IsUpgraded"/>. Idempotente: si ya está mejorada o no
        /// puede mejorarse, no hace nada.
        /// </summary>
        public void ApplyUpgrade()
        {
            if (_isUpgraded || !CanUpgrade()) return;

            if (singleCard != null)
            {
                singleCard = singleCard.CreateUpgradedClone();
            }
            else if (dualCard != null)
            {
                dualCard = dualCard.CreateUpgradedClone();
            }

            _isUpgraded = true;
        }

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
            CardDeckEntry copy = dualCard != null
                ? CreateDual(dualCard)
                : CreateSingle(singleCard);
            copy._isUpgraded = _isUpgraded;
            return copy;
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


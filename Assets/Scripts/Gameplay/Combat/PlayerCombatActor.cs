using System;
using System.Collections.Generic;
using RoguelikeCardBattler.Gameplay.Cards;

namespace RoguelikeCardBattler.Gameplay.Combat
{
    public class PlayerCombatActor : ICombatActor
    {
        private readonly List<CardDefinition> _drawPile = new List<CardDefinition>();
        private readonly List<CardDefinition> _discardPile = new List<CardDefinition>();
        private readonly List<CardDefinition> _hand = new List<CardDefinition>();
        private readonly Random _random;

        public string Id { get; }
        public string DisplayName { get; }
        public int CurrentHP { get; private set; }
        public int MaxHP { get; }
        public int Block { get; private set; }
        public int CurrentEnergy { get; private set; }
        public int MaxEnergy { get; }

        public IReadOnlyList<CardDefinition> Hand => _hand;
        public int DrawPileCount => _drawPile.Count;
        public int DiscardPileCount => _discardPile.Count;
        public int HandCount => _hand.Count;

        public PlayerCombatActor(
            string id,
            string displayName,
            int maxHP,
            int baseEnergy,
            IEnumerable<CardDefinition> startingDeck,
            Random random)
        {
            Id = id;
            DisplayName = displayName;
            MaxHP = Math.Max(1, maxHP);
            CurrentHP = MaxHP;
            MaxEnergy = Math.Max(0, baseEnergy);
            CurrentEnergy = MaxEnergy;
            _random = random ?? new Random();

            if (startingDeck != null)
            {
                _drawPile.AddRange(startingDeck);
            }

            Shuffle(_drawPile);
        }

        public void ResetEnergy() => CurrentEnergy = MaxEnergy;

        public bool CanPayEnergy(int cost) => CurrentEnergy >= cost;

        public bool SpendEnergy(int amount)
        {
            if (!CanPayEnergy(amount))
            {
                return false;
            }

            CurrentEnergy -= amount;
            return true;
        }

        public void GainEnergy(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentEnergy = Math.Min(MaxEnergy, CurrentEnergy + amount);
        }

        public void TakeDamage(int amount, ICombatActor source = null)
        {
            if (amount <= 0 || CurrentHP <= 0)
            {
                return;
            }

            int remaining = amount;
            if (Block > 0)
            {
                int absorbed = Math.Min(Block, remaining);
                LoseBlock(absorbed);
                remaining -= absorbed;
            }

            if (remaining <= 0)
            {
                return;
            }

            CurrentHP = Math.Max(0, CurrentHP - remaining);
        }

        public void GainBlock(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Block += amount;
        }

        public void LoseBlock(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Block = Math.Max(0, Block - amount);
        }

        public void DrawCards(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            for (int i = 0; i < amount; i++)
            {
                CardDefinition card = DrawSingleCard();
                if (card == null)
                {
                    break;
                }

                _hand.Add(card);
            }
        }

        public bool IsCardInHand(CardDefinition card) => card != null && _hand.Contains(card);

        public bool RemoveCardFromHand(CardDefinition card)
        {
            if (card == null)
            {
                return false;
            }

            return _hand.Remove(card);
        }

        public void DiscardCard(CardDefinition card)
        {
            if (card == null)
            {
                return;
            }

            _discardPile.Add(card);
        }

        public void DiscardHand()
        {
            if (_hand.Count == 0)
            {
                return;
            }

            _discardPile.AddRange(_hand);
            _hand.Clear();
        }

        public void ShuffleDiscardIntoDraw()
        {
            if (_discardPile.Count == 0)
            {
                return;
            }

            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_drawPile);
        }

        private CardDefinition DrawSingleCard()
        {
            if (_drawPile.Count == 0)
            {
                ShuffleDiscardIntoDraw();
            }

            if (_drawPile.Count == 0)
            {
                return null;
            }

            int lastIndex = _drawPile.Count - 1;
            CardDefinition card = _drawPile[lastIndex];
            _drawPile.RemoveAt(lastIndex);
            return card;
        }

        private void Shuffle(List<CardDefinition> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}


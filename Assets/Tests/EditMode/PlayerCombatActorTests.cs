using System;
using System.Collections.Generic;
using NUnit.Framework;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;

namespace RoguelikeCardBattler.Tests.EditMode
{
    public class PlayerCombatActorTests : CombatTestBase
    {
        private PlayerCombatActor CreatePlayer(List<CardDefinition> deck)
        {
            return new PlayerCombatActor(
                "player",
                "Test Player",
                maxHP: 20,
                baseEnergy: 3,
                startingDeck: deck,
                random: new Random(0));
        }

        [Test]
        public void DamageConsumesBlockBeforeHP()
        {
            var deck = new List<CardDefinition>();
            var player = CreatePlayer(deck);

            player.GainBlock(5);
            player.TakeDamage(8);

            Assert.AreEqual(0, player.Block, "Block should be consumed first.");
            Assert.AreEqual(17, player.CurrentHP, "Only damage beyond block reduces HP.");

            player.TakeDamage(999);
            Assert.AreEqual(0, player.CurrentHP, "HP should never go negative.");
        }

        [Test]
        public void DrawCardsMovesCardsAndCountsStayConsistent()
        {
            var drawEffect = CreateEffect(EffectType.DrawCards, 1, EffectTarget.Self);
            var cardA = CreateCard("cardA", CardType.Skill, CardTarget.Self, cost: 1, drawEffect);
            var cardB = CreateCard("cardB", CardType.Skill, CardTarget.Self, cost: 1, drawEffect);

            var deck = new List<CardDefinition> { cardA, cardB };
            var player = CreatePlayer(deck);

            player.DrawCards(1);
            Assert.AreEqual(1, player.HandCount);
            Assert.AreEqual(1, player.DrawPileCount);
            Assert.AreEqual(0, player.DiscardPileCount);

            player.DiscardHand();
            Assert.AreEqual(0, player.HandCount);
            Assert.AreEqual(0, player.DrawPileCount);
            Assert.AreEqual(1, player.DiscardPileCount);

            player.DrawCards(2); // should reshuffle discard into draw
            Assert.AreEqual(2, player.HandCount);
            Assert.AreEqual(0, player.DrawPileCount);
            Assert.AreEqual(0, player.DiscardPileCount);
        }

        [Test]
        public void EnemyCombatActorDamageAlsoConsumesBlock()
        {
            var enemyDefinition = CreateEnemyDefinition(
                "enemy",
                "Test Enemy",
                maxHp: 15,
                pattern: Gameplay.Enemies.EnemyAIPattern.Sequence,
                moves: new List<Gameplay.Enemies.EnemyMove>());
            var enemy = new EnemyCombatActor(enemyDefinition);

            enemy.GainBlock(4);
            enemy.TakeDamage(6);

            Assert.AreEqual(0, enemy.Block);
            Assert.AreEqual(13, enemy.CurrentHP);
        }
    }
}


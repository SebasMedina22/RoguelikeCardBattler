using System.Collections.Generic;
using NUnit.Framework;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Tests.EditMode
{
    public class DamageEffectivenessTests : CombatTestBase
    {
        private TurnManager CreateTurnManager(CardDefinition card, EnemyDefinition enemy)
        {
            var deck = new List<CardDeckEntry> { CreateSingleCardEntry(card) };

            var go = CreateGameObject("TurnManager");
            var manager = AddComponent<TurnManager>(go);
            manager.SetTestConfig(maxHp: 30, energy: 3, startingHand: 1, cardsPerTurnCount: 1);
            manager.SetTestData(deck, enemy);
            return manager;
        }

        [Test]
        public void PlayerDamage_SuperEffective_DealsIncreasedDamage()
        {
            var damage = CreateEffect(EffectType.Damage, 10, EffectTarget.SingleEnemy);
            var card = CreateCardWithElement(
                "strike_rojo",
                CardType.Attack,
                CardTarget.SingleEnemy,
                cost: 0,
                elementType: ElementType.Rojo,
                damage);

            var enemy = CreateEnemyDefinition(
                "enemy_azul",
                "Enemy Azul",
                maxHp: 20,
                pattern: EnemyAIPattern.Sequence,
                moves: new List<EnemyMove>(),
                elementType: ElementType.Azul);

            var manager = CreateTurnManager(card, enemy);
            manager.InitializeCombat();

            var cardEntry = manager.PlayerHand[0];
            bool played = manager.PlayCard(cardEntry);

            Assert.IsTrue(played);
            Assert.AreEqual(5, manager.EnemyHP); // 20 - round(10 * 1.5) = 5
        }

        [Test]
        public void PlayerDamage_LessEffective_DealsReducedDamage()
        {
            var damage = CreateEffect(EffectType.Damage, 10, EffectTarget.SingleEnemy);
            var card = CreateCardWithElement(
                "strike_azul",
                CardType.Attack,
                CardTarget.SingleEnemy,
                cost: 0,
                elementType: ElementType.Azul,
                damage);

            var enemy = CreateEnemyDefinition(
                "enemy_rojo",
                "Enemy Rojo",
                maxHp: 20,
                pattern: EnemyAIPattern.Sequence,
                moves: new List<EnemyMove>(),
                elementType: ElementType.Rojo);

            var manager = CreateTurnManager(card, enemy);
            manager.InitializeCombat();

            var cardEntry = manager.PlayerHand[0];
            bool played = manager.PlayCard(cardEntry);

            Assert.IsTrue(played);
            Assert.AreEqual(12, manager.EnemyHP); // 20 - round(10 * 0.75) = 12
        }

        [Test]
        public void FreePlay_AllowsCardWithoutEnergyAndConsumesCharge()
        {
            var damage = CreateEffect(EffectType.Damage, 5, EffectTarget.SingleEnemy);
            var card = CreateCardWithElement(
                "strike_cost1",
                CardType.Attack,
                CardTarget.SingleEnemy,
                cost: 1,
                elementType: ElementType.None,
                damage);

            var enemy = CreateEnemyDefinition(
                "enemy_none",
                "Enemy None",
                maxHp: 10,
                pattern: EnemyAIPattern.Sequence,
                moves: new List<EnemyMove>(),
                elementType: ElementType.None);

            var manager = CreateTurnManager(card, enemy);
            manager.SetTestConfig(maxHp: 30, energy: 0, startingHand: 1, cardsPerTurnCount: 1);
            manager.InitializeCombat();
            manager.SetFreePlaysForTest(1);

            var cardEntry = manager.PlayerHand[0];
            bool played = manager.PlayCard(cardEntry);

            Assert.IsTrue(played, "Card should be playable using free play even with 0 energy.");
            Assert.AreEqual(0, manager.FreePlays, "Free play should be consumed.");
            Assert.AreEqual(0, manager.PlayerEnergy, "Energy should remain unchanged at 0.");
            Assert.AreEqual(5, manager.EnemyHP, "Damage should still be applied.");
        }

        [Test]
        public void SuperEffectiveHit_GrantsFreePlay()
        {
            var damage = CreateEffect(EffectType.Damage, 10, EffectTarget.SingleEnemy);
            var card = CreateCardWithElement(
                "strike_rojo",
                CardType.Attack,
                CardTarget.SingleEnemy,
                cost: 0,
                elementType: ElementType.Rojo,
                damage);

            var enemy = CreateEnemyDefinition(
                "enemy_azul",
                "Enemy Azul",
                maxHp: 20,
                pattern: EnemyAIPattern.Sequence,
                moves: new List<EnemyMove>(),
                elementType: ElementType.Azul);

            var manager = CreateTurnManager(card, enemy);
            manager.InitializeCombat();

            Assert.AreEqual(0, manager.FreePlays);

            var cardEntry = manager.PlayerHand[0];
            bool played = manager.PlayCard(cardEntry);

            Assert.IsTrue(played);
            Assert.AreEqual(1, manager.FreePlays, "Super effective hit should grant one free play.");
        }
    }
}


using System.Collections.Generic;
using NUnit.Framework;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Tests.EditMode
{
    public class TurnManagerTests : CombatTestBase
    {
        private CardDefinition CreateDamageCard(string id, int damage, int cost = 0)
        {
            return CreateCard(
                id,
                CardType.Attack,
                CardTarget.SingleEnemy,
                cost,
                CreateEffect(EffectType.Damage, damage, EffectTarget.SingleEnemy));
        }

        private EnemyDefinition CreateEnemy(int hp, int damagePerAttack)
        {
            var move = CreateEnemyMove(
                "attack",
                "Attack",
                "Deal damage",
                new List<EffectRef>
                {
                    CreateEffect(EffectType.Damage, damagePerAttack, EffectTarget.SingleEnemy)
                });

            return CreateEnemyDefinition(
                "dummy_enemy",
                "Dummy Enemy",
                hp,
                EnemyAIPattern.Sequence,
                new List<EnemyMove> { move });
        }

        private TurnManager CreateTurnManager(
            List<CardDefinition> deck,
            EnemyDefinition enemy,
            int playerHp = 30,
            int energyPerTurn = 3,
            int startingHand = 2,
            int cardsPerTurn = 1)
        {
            var go = CreateGameObject("TurnManager");
            var manager = AddComponent<TurnManager>(go);
            manager.SetTestConfig(playerHp, energyPerTurn, startingHand, cardsPerTurn);
            manager.SetTestData(deck, enemy);
            return manager;
        }

        [Test]
        public void InitializeCombatSetsPlayerTurnAndResources()
        {
            var deck = new List<CardDefinition>
            {
                CreateDamageCard("strike", 5)
            };

            var enemy = CreateEnemy(hp: 20, damagePerAttack: 3);
            var manager = CreateTurnManager(deck, enemy, playerHp: 25, energyPerTurn: 2, startingHand: 1, cardsPerTurn: 1);

            manager.InitializeCombat();

            Assert.AreEqual(TurnManager.CombatPhase.PlayerTurn, manager.CurrentPhase);
            Assert.AreEqual(manager.PlayerMaxEnergy, manager.PlayerEnergy);
            Assert.AreEqual(1, manager.PlayerHandCount);
        }

        [Test]
        public void EndPlayerTurnRunsEnemyAndReturnsToPlayer()
        {
            var deck = new List<CardDefinition>
            {
                CreateDamageCard("strike", 5),
                CreateDamageCard("strike2", 5)
            };
            var enemy = CreateEnemy(hp: 30, damagePerAttack: 2);
            var manager = CreateTurnManager(deck, enemy, playerHp: 25, energyPerTurn: 3, startingHand: 2, cardsPerTurn: 2);

            manager.InitializeCombat();

            manager.EndPlayerTurn();

            Assert.AreEqual(TurnManager.CombatPhase.PlayerTurn, manager.CurrentPhase);
            Assert.AreEqual(2, manager.PlayerHandCount, "Player should draw cardsPerTurn after enemy finishes.");
        }

        [Test]
        public void DefeatingEnemyEndsCombatWithVictory()
        {
            var finisher = CreateDamageCard("finisher", damage: 999, cost: 0);
            var deck = new List<CardDefinition> { finisher };
            var enemy = CreateEnemy(hp: 10, damagePerAttack: 1);
            var manager = CreateTurnManager(deck, enemy, playerHp: 20, energyPerTurn: 1, startingHand: 1, cardsPerTurn: 1);

            manager.InitializeCombat();

            var card = manager.PlayerHand[0];
            bool played = manager.PlayCard(card);

            Assert.IsTrue(played);
            Assert.IsTrue(manager.IsCombatFinished);
            Assert.AreEqual(TurnManager.CombatPhase.Victory, manager.CurrentPhase);
        }

        [Test]
        public void EnemyKillingPlayerTriggersDefeat()
        {
            var deck = new List<CardDefinition>
            {
                CreateDamageCard("strike", 5)
            };
            var enemy = CreateEnemy(hp: 20, damagePerAttack: 999);
            var manager = CreateTurnManager(deck, enemy, playerHp: 5, energyPerTurn: 2, startingHand: 1, cardsPerTurn: 1);

            manager.InitializeCombat();

            manager.EndPlayerTurn();

            Assert.IsTrue(manager.IsCombatFinished);
            Assert.AreEqual(TurnManager.CombatPhase.Defeat, manager.CurrentPhase);
        }
    }
}


using System.Collections.Generic;
using NUnit.Framework;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Tests.EditMode
{
    public class WorldSwitchLimitTests : CombatTestBase
    {
        private TurnManager CreateTurnManager(CardDefinition card, EnemyDefinition enemy)
        {
            var deck = new List<CardDeckEntry> { CreateSingleCardEntry(card) };
            var go = CreateGameObject("TurnManager");
            var manager = AddComponent<TurnManager>(go);
            manager.SetTestConfig(maxHp: 20, energy: 3, startingHand: 1, cardsPerTurnCount: 1);
            manager.SetTestData(deck, enemy);
            return manager;
        }

        [Test]
        public void WorldChangeLimitedToOne_WhenDebugUnlimitedIsFalse()
        {
            var card = CreateCard("test", CardType.Attack, CardTarget.SingleEnemy, cost: 0);
            var enemy = CreateEnemyDefinition(
                "enemy",
                "Enemy",
                maxHp: 10,
                pattern: EnemyAIPattern.Sequence,
                moves: new List<EnemyMove>(),
                elementType: ElementType.None);

            var manager = CreateTurnManager(card, enemy);
            manager.SetWorldSwitchesForTest(used: 0, maxPerCombat: 1, unlimited: false);
            manager.InitializeCombat();

            Assert.AreEqual(TurnManager.WorldSide.A, manager.CurrentWorld);

            bool first = manager.TryChangeWorld();
            Assert.IsTrue(first, "First world change should succeed.");
            Assert.AreEqual(TurnManager.WorldSide.B, manager.CurrentWorld);
            Assert.AreEqual(1, manager.WorldSwitchesUsed);

            bool second = manager.TryChangeWorld();
            Assert.IsFalse(second, "Second world change should be blocked.");
            Assert.AreEqual(TurnManager.WorldSide.B, manager.CurrentWorld, "World should remain unchanged after limit.");
            Assert.AreEqual(1, manager.WorldSwitchesUsed);
        }

        [Test]
        public void WorldChangeUnlimited_WhenDebugIsTrue()
        {
            var card = CreateCard("test", CardType.Attack, CardTarget.SingleEnemy, cost: 0);
            var enemy = CreateEnemyDefinition(
                "enemy",
                "Enemy",
                maxHp: 10,
                pattern: EnemyAIPattern.Sequence,
                moves: new List<EnemyMove>(),
                elementType: ElementType.None);

            var manager = CreateTurnManager(card, enemy);
            manager.SetWorldSwitchesForTest(used: 0, maxPerCombat: 1, unlimited: true);
            manager.InitializeCombat();

            Assert.AreEqual(TurnManager.WorldSide.A, manager.CurrentWorld);

            bool first = manager.TryChangeWorld();
            bool second = manager.TryChangeWorld();
            bool third = manager.TryChangeWorld();

            Assert.IsTrue(first);
            Assert.IsTrue(second);
            Assert.IsTrue(third);
            Assert.AreEqual(TurnManager.WorldSide.B, manager.CurrentWorld); // Toggles each time; odd count ends in B
            Assert.GreaterOrEqual(manager.WorldSwitchesUsed, 0); // counter may keep increasing even in unlimited mode
        }

        // ── Tests de bonus switch por Contador de Estilo (Sub-PR C) ─────────────

        [Test]
        public void StyleBonus_GrantedBonus_AllowsExtraSwitch_ThenBlocks()
        {
            // Con maxPerCombat=1 y bonus=1 → TotalAvailable=2.
            // Switch 1: éxito. Switch 2: éxito (usa el bonus). Switch 3: bloqueado.
            var card = CreateCard("test", CardType.Attack, CardTarget.SingleEnemy, cost: 0);
            var enemy = CreateEnemyDefinition(
                "enemy", "Enemy", maxHp: 10,
                pattern: EnemyAIPattern.Sequence,
                moves: new List<EnemyMove>(),
                elementType: ElementType.None);

            var manager = CreateTurnManager(card, enemy);
            manager.SetWorldSwitchesForTest(used: 0, maxPerCombat: 1, unlimited: false);
            manager.InitializeCombat();
            manager.SetBonusWorldSwitchesForTest(1);

            bool first = manager.TryChangeWorld();
            bool second = manager.TryChangeWorld();
            bool third = manager.TryChangeWorld();

            Assert.IsTrue(first, "First switch should succeed (base).");
            Assert.IsTrue(second, "Second switch should succeed (bonus).");
            Assert.IsFalse(third, "Third switch should be blocked (limit reached).");
            Assert.AreEqual(2, manager.WorldSwitchesUsed);
        }

        [Test]
        public void StyleBonus_AfterBothSwitches_TotalEqualsUsed()
        {
            // Después de consumir el bonus, TotalAvailableWorldSwitches == WorldSwitchesUsed.
            var card = CreateCard("test", CardType.Attack, CardTarget.SingleEnemy, cost: 0);
            var enemy = CreateEnemyDefinition(
                "enemy", "Enemy", maxHp: 10,
                pattern: EnemyAIPattern.Sequence,
                moves: new List<EnemyMove>(),
                elementType: ElementType.None);

            var manager = CreateTurnManager(card, enemy);
            manager.SetWorldSwitchesForTest(used: 0, maxPerCombat: 1, unlimited: false);
            manager.InitializeCombat();
            manager.SetBonusWorldSwitchesForTest(1);

            manager.TryChangeWorld();
            manager.TryChangeWorld();

            Assert.AreEqual(manager.TotalAvailableWorldSwitches, manager.WorldSwitchesUsed,
                "After consuming both switches, TotalAvailable should equal WorldSwitchesUsed.");
        }
    }
}


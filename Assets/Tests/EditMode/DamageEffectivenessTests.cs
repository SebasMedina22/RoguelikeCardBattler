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
        public void PlayerActiveType_FollowsCurrentWorld()
        {
            var damage = CreateEffect(EffectType.Damage, 0, EffectTarget.SingleEnemy);
            var card = CreateCardWithElement(
                "filler",
                CardType.Attack,
                CardTarget.SingleEnemy,
                cost: 0,
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
            manager.SetPlayerTypesForTest(ElementType.Rojo, ElementType.Amarillo);
            manager.InitializeCombat();

            // Mundo A → tipo de A (Rojo).
            Assert.AreEqual(TurnManager.WorldSide.A, manager.CurrentWorld);
            Assert.AreEqual(ElementType.Rojo, manager.PlayerActiveType);

            // Mundo B → tipo de B (Amarillo).
            manager.SetCurrentWorldForTest(TurnManager.WorldSide.B);
            Assert.AreEqual(ElementType.Amarillo, manager.PlayerActiveType);

            // Volver a A → vuelve al tipo de A.
            manager.SetCurrentWorldForTest(TurnManager.WorldSide.A);
            Assert.AreEqual(ElementType.Rojo, manager.PlayerActiveType);
        }

        // ── Tests de efectividad Enemy → Player (DD-018, Sub-PR B) ──────────────

        [Test]
        public void EnemyDamage_SuperEffectiveAgainstPlayer_DealsIncreasedDamage()
        {
            // enemy=Amarillo, player=Rojo. Amarillo→Rojo es SuperEficaz.
            // Daño base 10 → round(10 * 1.5) = 15. HP: 30 - 15 = 15.
            var attackEffect = CreateEffect(EffectType.Damage, 10, EffectTarget.SingleEnemy);
            var attackMove = CreateEnemyMove("attack", "Attack", "Ataca",
                new List<EffectRef> { attackEffect }, weight: 1, intentType: EnemyIntentType.Attack);

            var enemy = CreateEnemyDefinition(
                "enemy_amarillo", "Enemy Amarillo", maxHp: 30,
                pattern: EnemyAIPattern.Sequence,
                moves: new List<EnemyMove> { attackMove },
                elementType: ElementType.Amarillo);

            var card = CreateCardWithElement("dummy", CardType.Skill, CardTarget.Self, cost: 99, ElementType.None);
            var manager = CreateTurnManager(card, enemy);
            manager.SetPlayerTypesForTest(ElementType.Rojo, ElementType.Amarillo);
            manager.InitializeCombat();

            int hpBefore = manager.PlayerHP;
            manager.EndPlayerTurn();

            Assert.AreEqual(hpBefore - 15, manager.PlayerHP); // round(10 * 1.5) = 15
        }

        [Test]
        public void EnemyDamage_LessEffectiveAgainstPlayer_DealsReducedDamage()
        {
            // enemy=Rojo, player=Amarillo. Rojo→Amarillo es PocoEficaz.
            // Daño base 10 → round(10 * 0.75) = round(7.5). Mathf.RoundToInt
            // usa "round half away from zero" → 8. HP: 30 - 8 = 22.
            var attackEffect = CreateEffect(EffectType.Damage, 10, EffectTarget.SingleEnemy);
            var attackMove = CreateEnemyMove("attack", "Attack", "Ataca",
                new List<EffectRef> { attackEffect }, weight: 1, intentType: EnemyIntentType.Attack);

            var enemy = CreateEnemyDefinition(
                "enemy_rojo", "Enemy Rojo", maxHp: 30,
                pattern: EnemyAIPattern.Sequence,
                moves: new List<EnemyMove> { attackMove },
                elementType: ElementType.Rojo);

            var card = CreateCardWithElement("dummy", CardType.Skill, CardTarget.Self, cost: 99, ElementType.None);
            var manager = CreateTurnManager(card, enemy);
            manager.SetPlayerTypesForTest(ElementType.Amarillo, ElementType.Rojo);
            manager.InitializeCombat();

            int hpBefore = manager.PlayerHP;
            manager.EndPlayerTurn();

            Assert.AreEqual(hpBefore - 8, manager.PlayerHP); // round(10 * 0.75) = 8
        }

        [Test]
        public void EnemyDamage_NeutralAgainstPlayer_DealsBaseDamage()
        {
            // enemy=Rojo, player=Morado. Rojo→Morado es Neutro.
            // Daño base 10 → 10. HP: 30 - 10 = 20.
            var attackEffect = CreateEffect(EffectType.Damage, 10, EffectTarget.SingleEnemy);
            var attackMove = CreateEnemyMove("attack", "Attack", "Ataca",
                new List<EffectRef> { attackEffect }, weight: 1, intentType: EnemyIntentType.Attack);

            var enemy = CreateEnemyDefinition(
                "enemy_rojo_neutro", "Enemy Rojo", maxHp: 30,
                pattern: EnemyAIPattern.Sequence,
                moves: new List<EnemyMove> { attackMove },
                elementType: ElementType.Rojo);

            var card = CreateCardWithElement("dummy", CardType.Skill, CardTarget.Self, cost: 99, ElementType.None);
            var manager = CreateTurnManager(card, enemy);
            manager.SetPlayerTypesForTest(ElementType.Morado, ElementType.Amarillo);
            manager.InitializeCombat();

            int hpBefore = manager.PlayerHP;
            manager.EndPlayerTurn();

            Assert.AreEqual(hpBefore - 10, manager.PlayerHP); // Neutro: sin modificador
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


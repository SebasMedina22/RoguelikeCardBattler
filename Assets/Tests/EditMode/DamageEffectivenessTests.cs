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
        public void SuperEffectiveHit_GrantsStyleCharge()
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

            Assert.AreEqual(0, manager.StyleCharges);

            var cardEntry = manager.PlayerHand[0];
            bool played = manager.PlayCard(cardEntry);

            Assert.IsTrue(played);
            Assert.AreEqual(1, manager.StyleCharges, "Super effective hit should grant one style charge.");
        }

        // ── Tests de Contador de Estilo (Sub-PR C) ──────────────────────────────

        [Test]
        public void StyleCounter_DecreasesOnEnemySuperEffectiveHit()
        {
            // enemy=Amarillo, player=Rojo. Amarillo→Rojo es SuperEficaz → resta carga.
            var attackEffect = CreateEffect(EffectType.Damage, 5, EffectTarget.SingleEnemy);
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
            manager.SetStyleChargesForTest(2);

            manager.EndPlayerTurn();

            Assert.AreEqual(1, manager.StyleCharges, "Enemy super effective hit should decrease style charges by 1.");
        }

        [Test]
        public void StyleCounter_NeverGoesBelowZero()
        {
            // enemy=Amarillo, player=Rojo. SuperEficaz con cargas en 0 → se queda en 0.
            var attackEffect = CreateEffect(EffectType.Damage, 5, EffectTarget.SingleEnemy);
            var attackMove = CreateEnemyMove("attack", "Attack", "Ataca",
                new List<EffectRef> { attackEffect }, weight: 1, intentType: EnemyIntentType.Attack);

            var enemy = CreateEnemyDefinition(
                "enemy_amarillo_zero", "Enemy Amarillo", maxHp: 30,
                pattern: EnemyAIPattern.Sequence,
                moves: new List<EnemyMove> { attackMove },
                elementType: ElementType.Amarillo);

            var card = CreateCardWithElement("dummy", CardType.Skill, CardTarget.Self, cost: 99, ElementType.None);
            var manager = CreateTurnManager(card, enemy);
            manager.SetPlayerTypesForTest(ElementType.Rojo, ElementType.Amarillo);
            manager.InitializeCombat();
            // StyleCharges ya está en 0 por default.

            manager.EndPlayerTurn();

            Assert.AreEqual(0, manager.StyleCharges, "Style charges should not go below zero.");
        }

        [Test]
        public void StyleCounter_At5ChargesGrantsBonusSwitch()
        {
            // player=Rojo, enemy=Azul. Rojo→Azul es SuperEficaz. Con 4 cargas + 1 hit → 5 → bonus switch.
            var damage = CreateEffect(EffectType.Damage, 10, EffectTarget.SingleEnemy);
            var card = CreateCardWithElement(
                "strike_rojo", CardType.Attack, CardTarget.SingleEnemy,
                cost: 0, elementType: ElementType.Rojo, damage);

            var enemy = CreateEnemyDefinition(
                "enemy_azul_bonus", "Enemy Azul", maxHp: 50,
                pattern: EnemyAIPattern.Sequence,
                moves: new List<EnemyMove>(),
                elementType: ElementType.Azul);

            var manager = CreateTurnManager(card, enemy);
            manager.SetPlayerTypesForTest(ElementType.Rojo, ElementType.Amarillo);
            manager.SetWorldSwitchesForTest(used: 0, maxPerCombat: 1, unlimited: false);
            manager.InitializeCombat();
            manager.SetStyleChargesForTest(4);

            var cardEntry = manager.PlayerHand[0];
            manager.PlayCard(cardEntry);

            Assert.AreEqual(2, manager.TotalAvailableWorldSwitches, "5 charges should grant 1 bonus world switch (total 2).");
            Assert.AreEqual(0, manager.StyleCharges, "Charges should reset to 0 after reaching 5.");
        }

        [Test]
        public void StyleCounter_BonusSwitchNotAccumulated()
        {
            // Llegar a 5 cargas dos veces no acumula a 3 switches (máx 2).
            var damage = CreateEffect(EffectType.Damage, 10, EffectTarget.SingleEnemy);
            var card = CreateCardWithElement(
                "strike_rojo_acc", CardType.Attack, CardTarget.SingleEnemy,
                cost: 0, elementType: ElementType.Rojo, damage);

            var enemy = CreateEnemyDefinition(
                "enemy_azul_acc", "Enemy Azul", maxHp: 200,
                pattern: EnemyAIPattern.Sequence,
                moves: new List<EnemyMove>(),
                elementType: ElementType.Azul);

            var manager = CreateTurnManager(card, enemy);
            manager.SetPlayerTypesForTest(ElementType.Rojo, ElementType.Amarillo);
            manager.SetWorldSwitchesForTest(used: 0, maxPerCombat: 1, unlimited: false);

            // Primer ciclo de 5 cargas: usar SetBonusWorldSwitchesForTest para simular
            // que ya se otorgó un bonus (el bonus ya está en 1, bloqueando acumulación).
            manager.InitializeCombat();
            manager.SetBonusWorldSwitchesForTest(1);
            manager.SetStyleChargesForTest(4);

            // Siguiente hit SuperEficaz llevaría a 5 cargas, pero bonus ya es 1 → no se acumula.
            var cardEntry = manager.PlayerHand[0];
            manager.PlayCard(cardEntry);

            Assert.AreEqual(2, manager.TotalAvailableWorldSwitches, "Bonus should stay at 1 (total 2), never accumulate to 2 (total 3).");
        }

        [Test]
        public void StyleCounter_DoesNotIncreaseOnNeutralHit()
        {
            // player=Rojo, enemy=Morado. Rojo→Morado es Neutro → no da cargas.
            var damage = CreateEffect(EffectType.Damage, 10, EffectTarget.SingleEnemy);
            var card = CreateCardWithElement(
                "strike_rojo_neutro", CardType.Attack, CardTarget.SingleEnemy,
                cost: 0, elementType: ElementType.Rojo, damage);

            var enemy = CreateEnemyDefinition(
                "enemy_morado", "Enemy Morado", maxHp: 20,
                pattern: EnemyAIPattern.Sequence,
                moves: new List<EnemyMove>(),
                elementType: ElementType.Morado);

            var manager = CreateTurnManager(card, enemy);
            manager.SetPlayerTypesForTest(ElementType.Rojo, ElementType.Amarillo);
            manager.InitializeCombat();

            var cardEntry = manager.PlayerHand[0];
            manager.PlayCard(cardEntry);

            Assert.AreEqual(0, manager.StyleCharges, "Neutral hit should not grant style charges.");
        }
    }
}


using System.Collections.Generic;
using NUnit.Framework;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;
using UnityEngine;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// M4 4c — enemigos transdimensionales (tipo activo conmuta con el mundo) y
    /// ancla (tipo fijo). Cubre defaults/derivada de EnemyDefinition, la resolución
    /// de TurnManager.EnemyElementType por mundo/ancla, y que esa resolución fluye
    /// al daño orgánico. NO re-asierta la tabla de efectividad (eso es de
    /// ElementEffectivenessTests) — asierta que el tipo activo usado cambió.
    /// </summary>
    public class EnemyTransdimTests : CombatTestBase
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

        // ── Caso 1: defaults (regresión / guard) ────────────────────────────────
        [Test]
        public void EnemyDefinition_FreshInstance_HasTransdimDefaults()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            Assert.AreEqual(ElementType.None, enemy.TypeWorldB, "typeWorldB debe defaultear a None.");
            Assert.IsFalse(enemy.IsAnchor, "isAnchor debe defaultear a false.");
            Assert.IsFalse(enemy.IsTransdimensional, "Sin typeWorldB ni anchor, no es transdim.");
            Object.DestroyImmediate(enemy);
        }

        // ── Caso 2: IsTransdimensional derivada ─────────────────────────────────
        [Test]
        public void IsTransdimensional_TrueOnlyWithTypeWorldBAndNotAnchor()
        {
            // typeWorldB != None && !anchor → true
            var transdim = CreateEnemyDefinition(
                "td", "Transdim", 10, EnemyAIPattern.Sequence, new List<EnemyMove>(),
                elementType: ElementType.Rojo, typeWorldB: ElementType.Azul, isAnchor: false);
            Assert.IsTrue(transdim.IsTransdimensional);

            // anchor (aun con typeWorldB seteado) → false
            var anchored = CreateEnemyDefinition(
                "an", "Anchor", 10, EnemyAIPattern.Sequence, new List<EnemyMove>(),
                elementType: ElementType.Rojo, typeWorldB: ElementType.Azul, isAnchor: true);
            Assert.IsFalse(anchored.IsTransdimensional, "Ancla nunca es transdim, aunque tenga typeWorldB.");

            // typeWorldB == None → false
            var single = CreateEnemyDefinition(
                "sg", "Single", 10, EnemyAIPattern.Sequence, new List<EnemyMove>(),
                elementType: ElementType.Rojo, typeWorldB: ElementType.None, isAnchor: false);
            Assert.IsFalse(single.IsTransdimensional, "Sin typeWorldB no es transdim.");
        }

        // ── Caso 3: resolución transdim sigue el mundo ──────────────────────────
        [Test]
        public void EnemyElementType_Transdim_FollowsCurrentWorld()
        {
            var card = CreateCardWithElement("filler", CardType.Skill, CardTarget.Self, cost: 99, ElementType.None);
            var enemy = CreateEnemyDefinition(
                "enemy_td", "Transdim", 30, EnemyAIPattern.Sequence, new List<EnemyMove>(),
                elementType: ElementType.Rojo, typeWorldB: ElementType.Azul, isAnchor: false);

            var manager = CreateTurnManager(card, enemy);
            manager.InitializeCombat();

            // Mundo A → tipo de A (Rojo).
            Assert.AreEqual(TurnManager.WorldSide.A, manager.CurrentWorld);
            Assert.AreEqual(ElementType.Rojo, manager.EnemyElementType);

            // Mundo B → tipo de B (Azul).
            manager.SetCurrentWorldForTest(TurnManager.WorldSide.B);
            Assert.AreEqual(ElementType.Azul, manager.EnemyElementType);

            // Volver a A → vuelve al tipo de A.
            manager.SetCurrentWorldForTest(TurnManager.WorldSide.A);
            Assert.AreEqual(ElementType.Rojo, manager.EnemyElementType);
        }

        // ── Caso 4: resolución ancla ignora el mundo ────────────────────────────
        [Test]
        public void EnemyElementType_Anchor_IgnoresWorld()
        {
            var card = CreateCardWithElement("filler", CardType.Skill, CardTarget.Self, cost: 99, ElementType.None);
            // Ancla con typeWorldB seteado por error → igual ignora el mundo (isAnchor precede).
            var enemy = CreateEnemyDefinition(
                "enemy_anchor", "Anchor", 30, EnemyAIPattern.Sequence, new List<EnemyMove>(),
                elementType: ElementType.Morado, typeWorldB: ElementType.Azul, isAnchor: true);

            var manager = CreateTurnManager(card, enemy);
            manager.InitializeCombat();

            Assert.AreEqual(ElementType.Morado, manager.EnemyElementType);
            manager.SetCurrentWorldForTest(TurnManager.WorldSide.B);
            Assert.AreEqual(ElementType.Morado, manager.EnemyElementType, "El ancla no cambia de tipo con el mundo.");
        }

        // ── Caso 5: tipo único (backward-compat) ────────────────────────────────
        [Test]
        public void EnemyElementType_SingleType_SameInBothWorlds()
        {
            var card = CreateCardWithElement("filler", CardType.Skill, CardTarget.Self, cost: 99, ElementType.None);
            var enemy = CreateEnemyDefinition(
                "enemy_single", "Single", 30, EnemyAIPattern.Sequence, new List<EnemyMove>(),
                elementType: ElementType.Amarillo, typeWorldB: ElementType.None, isAnchor: false);

            var manager = CreateTurnManager(card, enemy);
            manager.InitializeCombat();

            Assert.AreEqual(ElementType.Amarillo, manager.EnemyElementType);
            manager.SetCurrentWorldForTest(TurnManager.WorldSide.B);
            Assert.AreEqual(ElementType.Amarillo, manager.EnemyElementType, "Tipo único no cambia con el mundo.");
        }

        // ── Caso 6: combate orgánico (la resolución fluye al daño) ──────────────
        // Enemigo transdim Azul(A)/Amarillo(B). Carta Rojo 10 daño.
        //   Rojo→Azul     = SuperEficaz → round(10*1.5) = 15.
        //   Rojo→Amarillo = PocoEficaz  → round(10*0.75) = 8.
        // No re-asierta la tabla; asierta que el daño cambia porque el tipo activo
        // del enemigo conmutó con el mundo.
        [Test]
        public void EnemyElementType_Transdim_DamageEffectivenessChangesWithWorld()
        {
            // Mundo A: tipo activo del enemigo = Azul → SuperEficaz.
            var cardA = CreateCardWithElement(
                "strike_rojo_a", CardType.Attack, CardTarget.SingleEnemy, cost: 0, ElementType.Rojo,
                CreateEffect(EffectType.Damage, 10, EffectTarget.SingleEnemy));
            var enemyA = CreateEnemyDefinition(
                "enemy_td_a", "Transdim A", maxHp: 50, EnemyAIPattern.Sequence, new List<EnemyMove>(),
                elementType: ElementType.Azul, typeWorldB: ElementType.Amarillo, isAnchor: false);
            var managerA = CreateTurnManager(cardA, enemyA);
            managerA.InitializeCombat();
            Assert.AreEqual(TurnManager.WorldSide.A, managerA.CurrentWorld);
            managerA.PlayCard(managerA.PlayerHand[0]);
            Assert.AreEqual(50 - 15, managerA.EnemyHP,
                "Mundo A: tipo activo Azul → Rojo SuperEficaz → round(10*1.5)=15.");

            // Mundo B: tipo activo del enemigo = Amarillo → PocoEficaz.
            var cardB = CreateCardWithElement(
                "strike_rojo_b", CardType.Attack, CardTarget.SingleEnemy, cost: 0, ElementType.Rojo,
                CreateEffect(EffectType.Damage, 10, EffectTarget.SingleEnemy));
            var enemyB = CreateEnemyDefinition(
                "enemy_td_b", "Transdim B", maxHp: 50, EnemyAIPattern.Sequence, new List<EnemyMove>(),
                elementType: ElementType.Azul, typeWorldB: ElementType.Amarillo, isAnchor: false);
            var managerB = CreateTurnManager(cardB, enemyB);
            managerB.InitializeCombat();
            managerB.SetCurrentWorldForTest(TurnManager.WorldSide.B);
            managerB.PlayCard(managerB.PlayerHand[0]);
            Assert.AreEqual(50 - 8, managerB.EnemyHP,
                "Mundo B: tipo activo Amarillo → Rojo PocoEficaz → round(10*0.75)=8.");
        }
    }
}

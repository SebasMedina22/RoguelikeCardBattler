using System.Collections.Generic;
using NUnit.Framework;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// T9 (auditoría 2026-06, SUB-PR 2): una carta de tipo None aplica 90% del daño
    /// base (DD-002) contra CUALQUIER tipo de enemigo, fuera de la tabla de efectividad
    /// y sin otorgar cargas de Estilo.
    ///
    /// Nota sobre el 8º dispatch: las cartas neutras también disparan OnDamageDealt
    /// (Sub-PR 3B, TurnManager.ApplyPlayerToEnemyEffectiveness ~líneas 611-620). Ese
    /// dispatch depende de RunSession.RelicDispatcher, que se cablea en
    /// RunSession.Awake — y Awake NO corre al AddComponent en EditMode (los callbacks
    /// de MonoBehaviour solo corren en Play). Por eso esa rama se valida en Play /
    /// por lectura de código; acá se congela la regla de balance del 90%.
    /// </summary>
    public class NeutralCardDamageTests : CombatTestBase
    {
        private TurnManager CreateManager(CardDefinition card, EnemyDefinition enemy)
        {
            var deck = new List<CardDeckEntry> { CreateSingleCardEntry(card) };
            var go = CreateGameObject("TurnManager");
            var manager = AddComponent<TurnManager>(go);
            manager.SetTestConfig(maxHp: 30, energy: 3, startingHand: 1, cardsPerTurnCount: 1);
            manager.SetTestData(deck, enemy);
            return manager;
        }

        [TestCase(ElementType.Rojo)]
        [TestCase(ElementType.Azul)]
        [TestCase(ElementType.None)]
        public void NeutralCard_Applies90PercentDamage(ElementType enemyType)
        {
            // Carta None, daño base 10 → round(10 * 0.9) = 9, sin importar el tipo enemigo.
            CardDefinition card = CreateCardWithElement("neutral_strike", CardType.Attack, CardTarget.SingleEnemy,
                cost: 0, ElementType.None, CreateEffect(EffectType.Damage, 10, EffectTarget.SingleEnemy));
            EnemyDefinition enemy = CreateEnemyDefinition("enemy_" + enemyType, "Enemy", 50,
                EnemyAIPattern.Sequence, new List<EnemyMove>(), enemyType);

            TurnManager manager = CreateManager(card, enemy);
            manager.InitializeCombat();
            int hpBefore = manager.EnemyHP;

            manager.PlayCard(manager.PlayerHand[0]);

            Assert.AreEqual(hpBefore - 9, manager.EnemyHP,
                "Carta neutra (None) aplica 90% del daño base (DD-002) contra cualquier tipo.");
            Assert.AreEqual(0, manager.StyleCharges,
                "Las cartas neutras no pasan por la tabla de efectividad → no otorgan cargas de Estilo.");
        }
    }
}

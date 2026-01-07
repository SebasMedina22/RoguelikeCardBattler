using NUnit.Framework;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;
using UnityEngine;

namespace RoguelikeCardBattler.Tests.EditMode
{
    public class DefinitionTypeDefaultsTests
    {
        [Test]
        public void CardDefinition_DefaultsToNoneElement()
        {
            var card = ScriptableObject.CreateInstance<CardDefinition>();
            Assert.AreEqual(ElementType.None, card.ElementType);
            Object.DestroyImmediate(card);
        }

        [Test]
        public void EnemyDefinition_DefaultsToNoneElement()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            Assert.AreEqual(ElementType.None, enemy.ElementType);
            Object.DestroyImmediate(enemy);
        }
    }
}


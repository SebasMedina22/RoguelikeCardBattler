using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// Tests EditMode del contrato de datos del campo de arte de carta (C7).
    /// Lógica pura de SO, sin UI: default null, persistencia en SetDebugData, en el
    /// clon de upgrade, herencia dual por mundo y resolución vía CardDeckEntry.
    /// El render (CardHandView / NewRunController) se valida a mano en escena.
    /// </summary>
    public class CardArtTests
    {
        private static Sprite MakeSprite()
        {
            return Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        private static CardDefinition MakeCard(string id, Sprite art, bool withUpgrade = false)
        {
            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.SetDebugData(
                id,
                id,
                "",
                1,
                CardType.Attack,
                CardRarity.Common,
                CardTarget.SingleEnemy,
                new List<string>(),
                new List<EffectRef> { new EffectRef { effectType = EffectType.Damage, value = 6, target = EffectTarget.SingleEnemy } },
                ElementType.None,
                art);
            if (withUpgrade)
            {
                card.Upgrade.SetTestData(overrideCost: true, upgradedCost: 0, upgradedEffects: null, upgradedName: null, upgradedDescription: null);
            }
            return card;
        }

        [Test]
        public void NewCard_Art_DefaultsToNull()
        {
            var card = ScriptableObject.CreateInstance<CardDefinition>();
            Assert.IsNull(card.Art);
            Object.DestroyImmediate(card);
        }

        [Test]
        public void SetDebugData_SetsAndOmitsArt()
        {
            Sprite sprite = MakeSprite();

            CardDefinition withArt = MakeCard("withArt", sprite);
            Assert.AreSame(sprite, withArt.Art);

            // Caller viejo (sin el parámetro newArt) → Art queda null.
            CardDefinition noArt = MakeCard("noArt", null);
            Assert.IsNull(noArt.Art);

            Object.DestroyImmediate(withArt);
            Object.DestroyImmediate(noArt);
        }

        [Test]
        public void UpgradedClone_PreservesBaseArt()
        {
            Sprite sprite = MakeSprite();
            CardDefinition card = MakeCard("strike", sprite, withUpgrade: true);

            CardDefinition clone = card.CreateUpgradedClone();
            Assert.AreSame(sprite, clone.Art, "El clon de upgrade debe conservar el arte base (D3).");

            Object.DestroyImmediate(clone);
            Object.DestroyImmediate(card);
        }

        [Test]
        public void DualCard_InheritsArtPerWorld()
        {
            Sprite spriteA = MakeSprite();
            Sprite spriteB = MakeSprite();
            CardDefinition sideA = MakeCard("dualA", spriteA);
            CardDefinition sideB = MakeCard("dualB", spriteB);

            DualCardDefinition dual = ScriptableObject.CreateInstance<DualCardDefinition>();
            dual.InitRuntimeSides("dual1", "Dual", sideA, sideB);

            Assert.AreSame(spriteA, dual.GetSide(TurnManager.WorldSide.A).Art);
            Assert.AreSame(spriteB, dual.GetSide(TurnManager.WorldSide.B).Art);

            Object.DestroyImmediate(dual);
            Object.DestroyImmediate(sideA);
            Object.DestroyImmediate(sideB);
        }

        [Test]
        public void CardDeckEntry_ResolvesActiveArtPerWorld()
        {
            Sprite spriteA = MakeSprite();
            Sprite spriteB = MakeSprite();
            CardDefinition sideA = MakeCard("dualA", spriteA);
            CardDefinition sideB = MakeCard("dualB", spriteB);

            DualCardDefinition dual = ScriptableObject.CreateInstance<DualCardDefinition>();
            dual.InitRuntimeSides("dual1", "Dual", sideA, sideB);
            CardDeckEntry entry = CardDeckEntry.CreateDual(dual);

            Assert.AreSame(spriteA, entry.GetActiveCard(TurnManager.WorldSide.A).Art);
            Assert.AreSame(spriteB, entry.GetActiveCard(TurnManager.WorldSide.B).Art);

            Object.DestroyImmediate(dual);
            Object.DestroyImmediate(sideA);
            Object.DestroyImmediate(sideB);
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// 4a follow-up — afinidad en cartas ganadas/compradas/de-evento durante la run.
    /// Verifica que <see cref="RunState.AddCardToDeck(CardDeckEntry)"/> rutea por
    /// <see cref="AffinityResolver"/>: una carta afín adopta los tipos de mundo del
    /// jugador al entrar al mazo (no se queda como single sin tipo), mientras neutras y
    /// duales pasan sin cambios. Cubre el seam único que reward (RunFlowController) y
    /// Tienda (ShopNodeController) ya usan — y que reusarán los eventos de 4b.
    /// </summary>
    public class RunStateAffinityTests
    {
        // SOs runtime creados en estos tests (cuerpos a mano + las duales/lados que
        // produce el resolver). Se destruyen en TearDown para no dejar SOs huérfanos.
        private readonly List<Object> _sos = new List<Object>();

        [TearDown]
        public void Cleanup()
        {
            foreach (Object so in _sos)
            {
                if (so != null) Object.DestroyImmediate(so);
            }
            _sos.Clear();
        }

        private T Track<T>(T so) where T : Object
        {
            if (so != null) _sos.Add(so);
            return so;
        }

        private void TrackResolved(CardDeckEntry entry)
        {
            if (entry?.DualCard != null)
            {
                Track(entry.DualCard);
                Track(entry.DualCard.SideA);
                Track(entry.DualCard.SideB);
            }
        }

        private CardDefinition MakeStrike(string id, ElementType type, bool affinity)
        {
            var card = ScriptableObject.CreateInstance<CardDefinition>();
            card.SetDebugData(
                id, "Golpe", "Inflige 6 de daño.", 1, CardType.Attack, CardRarity.Common,
                CardTarget.SingleEnemy, new List<string>(),
                new List<EffectRef> { new EffectRef { effectType = EffectType.Damage, value = 6, target = EffectTarget.SingleEnemy } },
                type, null, affinity);
            return Track(card);
        }

        private RunState NewState(ElementType a, ElementType b)
        {
            var state = new RunState();
            state.PlayerWorldAType = a;
            state.PlayerWorldBType = b;
            return state;
        }

        [Test] // Una recompensa AFÍN entra al mazo como dual tipada por los mundos del jugador
        public void AddCardToDeck_AffineSingle_ResolvesToWorldTypedDual()
        {
            RunState state = NewState(ElementType.Amarillo, ElementType.Morado);
            state.AddCardToDeck(CardDeckEntry.CreateSingle(MakeStrike("reward_aff", ElementType.None, affinity: true)));

            Assert.AreEqual(1, state.Deck.Count);
            CardDeckEntry e = state.Deck[0];
            TrackResolved(e);
            Assert.IsNotNull(e.DualCard, "Una recompensa afín entra al mazo como dual resuelta.");
            Assert.IsNull(e.SingleCard);
            Assert.AreEqual(ElementType.Amarillo, e.DualCard.SideA.ElementType, "Lado A = tipo del Mundo A del jugador.");
            Assert.AreEqual(ElementType.Morado, e.DualCard.SideB.ElementType, "Lado B = tipo del Mundo B del jugador.");
        }

        [Test] // Una recompensa NEUTRA sigue siendo single None (no se convierte en dual)
        public void AddCardToDeck_NeutralSingle_StaysSingleNone()
        {
            RunState state = NewState(ElementType.Amarillo, ElementType.Morado);
            state.AddCardToDeck(CardDeckEntry.CreateSingle(MakeStrike("reward_neu", ElementType.None, affinity: false)));

            Assert.AreEqual(1, state.Deck.Count);
            CardDeckEntry e = state.Deck[0];
            Assert.IsNotNull(e.SingleCard);
            Assert.IsNull(e.DualCard);
            Assert.AreEqual(ElementType.None, e.SingleCard.ElementType);
        }

        [Test] // Una dual YA tipada pasa sin cambios (clon) — no se re-resuelve
        public void AddCardToDeck_TypedDual_PassesUnchanged()
        {
            CardDefinition sideA = MakeStrike("dual_a", ElementType.Rojo, affinity: false);
            CardDefinition sideB = MakeStrike("dual_b", ElementType.Negro, affinity: false);
            var dual = Track(ScriptableObject.CreateInstance<DualCardDefinition>());
            dual.InitRuntimeSides("dual_test", "Golpe", sideA, sideB);

            RunState state = NewState(ElementType.Amarillo, ElementType.Morado);
            state.AddCardToDeck(CardDeckEntry.CreateDual(dual));

            Assert.AreEqual(1, state.Deck.Count);
            CardDeckEntry e = state.Deck[0];
            TrackResolved(e);
            Assert.IsNotNull(e.DualCard, "Una dual entra como dual.");
            Assert.AreEqual(ElementType.Rojo, e.DualCard.SideA.ElementType, "Una dual ya tipada NO se re-resuelve a los mundos del jugador.");
            Assert.AreEqual(ElementType.Negro, e.DualCard.SideB.ElementType);
        }

        [Test] // El clon es independiente del SO autorado (no aliasa la lista de config)
        public void AddCardToDeck_ClonesEntry_NoAliasing()
        {
            RunState state = NewState(ElementType.Amarillo, ElementType.Morado);
            CardDeckEntry authored = CardDeckEntry.CreateSingle(MakeStrike("alias_neu", ElementType.None, affinity: false));
            state.AddCardToDeck(authored);

            Assert.AreEqual(1, state.Deck.Count);
            Assert.AreNotSame(authored, state.Deck[0], "La entry del mazo es un clon, no la referencia autorada.");
        }
    }
}

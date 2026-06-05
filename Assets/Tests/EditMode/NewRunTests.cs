using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Run;
using RoguelikeCardBattler.Run.NewRun;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// Tests EditMode de NewRunScene (Sub-PR 3E). Validan los helpers static puros
    /// de <see cref="StarterDraft"/> (filtrado/determinismo del draft, composición
    /// dual, regla de tipos distintos, escritura de selección) y la inyección de la
    /// carta drafteada en el mazo vía <c>RunState.PendingStarterCard</c> +
    /// <c>InitializeDeck</c>. Patrón: RunState directo sin MonoBehaviour, igual que
    /// ShopTests (todo el flujo de NewRunScene vive fuera de combate).
    /// </summary>
    public class NewRunTests
    {
        // ────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────

        private static CardDefinition MakeFace(string id, string name, ElementType element)
        {
            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.SetDebugData(
                id, name, "", 1,
                CardType.Attack, CardRarity.Common, CardTarget.SingleEnemy,
                new List<string>(), new List<EffectRef>(), element);
            return card;
        }

        private static NewRunConfig MakeConfig(List<CardDefinition> faces, int optionsPerWorld = 3)
        {
            NewRunConfig config = ScriptableObject.CreateInstance<NewRunConfig>();
            config.SetDebugData(faces, new List<ElementType>
            {
                ElementType.Rojo, ElementType.Amarillo, ElementType.Azul,
                ElementType.Morado, ElementType.Negro, ElementType.Blanco
            }, optionsPerWorld);
            return config;
        }

        // Pool con >=3 caras por tipo para Rojo / Azul / Amarillo (suficiente para
        // los tests de filtrado y determinismo).
        private static List<CardDefinition> RichPool()
        {
            List<CardDefinition> faces = new List<CardDefinition>();
            ElementType[] types = { ElementType.Rojo, ElementType.Azul, ElementType.Amarillo };
            foreach (ElementType t in types)
            {
                for (int i = 0; i < 4; i++)
                {
                    faces.Add(MakeFace($"{t}_{i}", $"{t} {i}", t));
                }
            }
            return faces;
        }

        // ────────────────────────────────────────
        // 1. Filtrado por tipo
        // ────────────────────────────────────────

        [Test]
        public void BuildDraftOptions_FiltersEachColumnByType()
        {
            NewRunConfig config = MakeConfig(RichPool(), optionsPerWorld: 3);

            StarterDraft.DraftOptions opts =
                StarterDraft.BuildDraftOptions(config, ElementType.Rojo, ElementType.Azul, seed: 5);

            Assert.AreEqual(3, opts.WorldAOptions.Count);
            Assert.AreEqual(3, opts.WorldBOptions.Count);
            foreach (CardDefinition f in opts.WorldAOptions)
                Assert.AreEqual(ElementType.Rojo, f.ElementType, "Columna A debe ser sólo del tipo A.");
            foreach (CardDefinition f in opts.WorldBOptions)
                Assert.AreEqual(ElementType.Azul, f.ElementType, "Columna B debe ser sólo del tipo B.");
        }

        // ────────────────────────────────────────
        // 2. Determinismo por seed
        // ────────────────────────────────────────

        [Test]
        public void BuildDraftOptions_DeterministicBySeed()
        {
            NewRunConfig config = MakeConfig(RichPool(), optionsPerWorld: 3);

            StarterDraft.DraftOptions first =
                StarterDraft.BuildDraftOptions(config, ElementType.Rojo, ElementType.Azul, seed: 42);
            StarterDraft.DraftOptions second =
                StarterDraft.BuildDraftOptions(config, ElementType.Rojo, ElementType.Azul, seed: 42);

            CollectionAssert.AreEqual(IdsOf(first.WorldAOptions), IdsOf(second.WorldAOptions),
                "Misma seed → misma columna A.");
            CollectionAssert.AreEqual(IdsOf(first.WorldBOptions), IdsOf(second.WorldBOptions),
                "Misma seed → misma columna B.");
        }

        private static List<string> IdsOf(List<CardDefinition> faces)
        {
            List<string> ids = new List<string>();
            foreach (CardDefinition f in faces) ids.Add(f.Id);
            return ids;
        }

        // ────────────────────────────────────────
        // 3. Composición dual
        // ────────────────────────────────────────

        [Test]
        public void ComposeDualCard_SidesMapToWorlds()
        {
            CardDefinition faceA = MakeFace("a", "Cara A", ElementType.Rojo);
            CardDefinition faceB = MakeFace("b", "Cara B", ElementType.Azul);

            DualCardDefinition dual = StarterDraft.ComposeDualCard(faceA, faceB);

            Assert.AreSame(faceA, dual.GetSide(TurnManager.WorldSide.A));
            Assert.AreSame(faceB, dual.GetSide(TurnManager.WorldSide.B));
        }

        // ────────────────────────────────────────
        // 4. La carta compuesta entra al mazo
        // ────────────────────────────────────────

        [Test]
        public void PendingStarterCard_InjectedIntoDeckOnInitialize()
        {
            CardDefinition faceA = MakeFace("a", "Cara A", ElementType.Rojo);
            CardDefinition faceB = MakeFace("b", "Cara B", ElementType.Azul);
            DualCardDefinition dual = StarterDraft.ComposeDualCard(faceA, faceB);

            RunState state = new RunState();
            StarterDraft.ApplySelectionToState(state, ElementType.Rojo, ElementType.Azul, dual);

            List<CardDeckEntry> starter = new List<CardDeckEntry>
            {
                CardDeckEntry.CreateSingle(MakeFace("s1", "Starter 1", ElementType.None)),
                CardDeckEntry.CreateSingle(MakeFace("s2", "Starter 2", ElementType.None)),
            };

            state.InitializeDeck(starter);

            Assert.AreEqual(starter.Count + 1, state.Deck.Count, "El mazo = starter base + carta drafteada.");
            CardDeckEntry last = state.Deck[state.Deck.Count - 1];
            Assert.IsNotNull(last.DualCard, "La carta inyectada debe ser la dual drafteada.");
            Assert.AreSame(faceA, last.DualCard.GetSide(TurnManager.WorldSide.A));
            Assert.AreSame(faceB, last.DualCard.GetSide(TurnManager.WorldSide.B));
        }

        // ────────────────────────────────────────
        // 5. ApplySelection escribe los tipos
        // ────────────────────────────────────────

        [Test]
        public void ApplySelection_WritesChosenTypes()
        {
            CardDefinition faceA = MakeFace("a", "Cara A", ElementType.Morado);
            CardDefinition faceB = MakeFace("b", "Cara B", ElementType.Negro);
            DualCardDefinition dual = StarterDraft.ComposeDualCard(faceA, faceB);

            RunState state = new RunState();
            StarterDraft.ApplySelectionToState(state, ElementType.Morado, ElementType.Negro, dual);

            Assert.AreEqual(ElementType.Morado, state.PlayerWorldAType);
            Assert.AreEqual(ElementType.Negro, state.PlayerWorldBType);
            Assert.IsNotNull(state.PendingStarterCard);
            Assert.IsNotNull(state.PendingStarterCard.DualCard);
        }

        // ────────────────────────────────────────
        // 6. Restricción: tipos distintos
        // ────────────────────────────────────────

        [Test]
        public void TypesValid_RequiresTwoDistinctNonNoneTypes()
        {
            Assert.IsTrue(StarterDraft.TypesValid(ElementType.Rojo, ElementType.Azul));
            Assert.IsFalse(StarterDraft.TypesValid(ElementType.Rojo, ElementType.Rojo), "B no puede igualar a A.");
            Assert.IsFalse(StarterDraft.TypesValid(ElementType.None, ElementType.Azul), "A debe estar elegido.");
            Assert.IsFalse(StarterDraft.TypesValid(ElementType.Rojo, ElementType.None), "B debe estar elegido.");
        }

        // ────────────────────────────────────────
        // 7. Sin estado sucio si no se confirma
        // ────────────────────────────────────────

        [Test]
        public void NoDirtyState_WhenSelectionNotApplied()
        {
            // El controller NO toca RunState hasta ApplySelection. Un RunState fresco
            // sobre el que sólo se construyó/navegó la pantalla conserva los defaults.
            RunState state = new RunState();

            Assert.AreEqual(ElementType.Rojo, state.PlayerWorldAType, "Tipo A default intacto.");
            Assert.AreEqual(ElementType.Amarillo, state.PlayerWorldBType, "Tipo B default intacto.");
            Assert.IsNull(state.PendingStarterCard, "Sin confirmar → no hay carta pendiente.");
        }

        // ────────────────────────────────────────
        // 8. Guard de pool insuficiente
        // ────────────────────────────────────────

        [Test]
        public void BuildDraftOptions_InsufficientPool_ReturnsAvailableWithoutCrash()
        {
            // Rojo tiene 1 sola cara pero se piden 3 → devuelve la disponible (1)
            // sin crashear (la política "no silent caps" loggea un warning).
            List<CardDefinition> faces = new List<CardDefinition>
            {
                MakeFace("r1", "Rojo único", ElementType.Rojo),
                MakeFace("z1", "Azul 1", ElementType.Azul),
                MakeFace("z2", "Azul 2", ElementType.Azul),
                MakeFace("z3", "Azul 3", ElementType.Azul),
            };
            NewRunConfig config = MakeConfig(faces, optionsPerWorld: 3);

            StarterDraft.DraftOptions opts =
                StarterDraft.BuildDraftOptions(config, ElementType.Rojo, ElementType.Azul, seed: 1);

            Assert.AreEqual(1, opts.WorldAOptions.Count, "Columna corta devuelve sólo lo disponible.");
            Assert.AreEqual(3, opts.WorldBOptions.Count, "La otra columna no se ve afectada.");
        }
    }
}

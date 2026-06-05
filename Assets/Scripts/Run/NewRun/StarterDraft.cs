using System.Collections.Generic;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;

namespace RoguelikeCardBattler.Run.NewRun
{
    // Nota: este archivo vive en RoguelikeCardBattler.Run.NewRun pero referencia
    // RunState (RoguelikeCardBattler.Run) en ApplySelectionToState — mismo assembly.

    /// <summary>
    /// Lógica pura del draft de la carta especial dual (Sub-PR 3E). Espejo de
    /// <c>ShopNodeController.BuildStock</c>: helpers static y testeables sin UI.
    /// El controller (NewRunController) sólo orquesta UI y llama aquí.
    ///
    /// Modelo del draft = "Componer" (decisión cerrada #1): se ofrecen N caras
    /// filtradas por el tipo del Mundo A + N por el del Mundo B; el jugador elige
    /// una de cada columna y <see cref="ComposeDualCard"/> arma el dual en runtime.
    /// </summary>
    public static class StarterDraft
    {
        /// <summary>
        /// Opciones de draft para una elección de tipos: las caras filtradas por
        /// el tipo del Mundo A y las del Mundo B.
        /// </summary>
        public struct DraftOptions
        {
            public List<CardDefinition> WorldAOptions;
            public List<CardDefinition> WorldBOptions;
        }

        /// <summary>
        /// Arma las opciones de draft de forma determinista por <paramref name="seed"/>:
        /// filtra <c>config.DraftFaces</c> por <paramref name="typeA"/> (columna A)
        /// y por <paramref name="typeB"/> (columna B), baraja cada lista con
        /// Fisher-Yates y RNG por seed, y toma <c>config.OptionsPerWorld</c> de cada
        /// una. Misma seed → mismas opciones.
        ///
        /// Política "no silent caps": si un tipo tiene menos caras que
        /// OptionsPerWorld, devuelve las disponibles y loggea el faltante en vez de
        /// recortar en silencio.
        /// </summary>
        public static DraftOptions BuildDraftOptions(NewRunConfig config, ElementType typeA, ElementType typeB, int seed)
        {
            DraftOptions options = new DraftOptions
            {
                WorldAOptions = new List<CardDefinition>(),
                WorldBOptions = new List<CardDefinition>()
            };
            if (config == null) return options;

            System.Random rng = new System.Random(seed);
            int perWorld = config.OptionsPerWorld;

            options.WorldAOptions = PickFacesForType(config, typeA, perWorld, rng, "A");
            options.WorldBOptions = PickFacesForType(config, typeB, perWorld, rng, "B");
            return options;
        }

        /// <summary>
        /// Compone la carta dual a partir de la cara elegida del Mundo A y la del
        /// Mundo B, en runtime (sin SerializedObject) vía
        /// <c>DualCardDefinition.InitRuntimeSides</c> — mismo mecanismo que usa la
        /// mejora de la Hoguera. <c>GetSide(WorldSide.A)</c> devuelve
        /// <paramref name="sideA"/> y <c>GetSide(WorldSide.B)</c>, <paramref name="sideB"/>.
        /// </summary>
        public static DualCardDefinition ComposeDualCard(CardDefinition sideA, CardDefinition sideB)
        {
            DualCardDefinition dual = ScriptableObject.CreateInstance<DualCardDefinition>();

            string idA = sideA != null ? sideA.Id : "?";
            string idB = sideB != null ? sideB.Id : "?";
            string nameA = sideA != null ? sideA.CardName : "?";
            string nameB = sideB != null ? sideB.CardName : "?";
            string displayName = nameA == nameB ? nameA : $"{nameA} / {nameB}";

            dual.InitRuntimeSides($"starter_{idA}_{idB}", displayName, sideA, sideB);
            return dual;
        }

        /// <summary>
        /// Regla de tipos válidos (decisión cerrada #3): dos tipos elegidos, no
        /// None, y distintos entre sí (el tipo de B no puede igualar al de A). La
        /// usa el controller para habilitar "Continuar"; pura para testearla sin UI.
        /// </summary>
        public static bool TypesValid(ElementType typeA, ElementType typeB)
        {
            return typeA != ElementType.None
                && typeB != ElementType.None
                && typeA != typeB;
        }

        /// <summary>
        /// Escribe la selección confirmada en el RunState: los 2 tipos por mundo y
        /// la carta drafteada como <c>PendingStarterCard</c> (la consume
        /// InitializeDeck). Pura sobre el estado que recibe — sin RunSession — para
        /// testearla sin instanciar el controller. La llama
        /// <c>NewRunController.ApplySelection</c> con el RunState vivo.
        /// </summary>
        public static void ApplySelectionToState(RunState state, ElementType typeA, ElementType typeB, DualCardDefinition dual)
        {
            if (state == null) return;
            state.PlayerWorldAType = typeA;
            state.PlayerWorldBType = typeB;
            state.PendingStarterCard = dual != null ? CardDeckEntry.CreateDual(dual) : null;
        }

        // ──────────────────────────────────────────────
        // Internos
        // ──────────────────────────────────────────────

        private static List<CardDefinition> PickFacesForType(
            NewRunConfig config, ElementType type, int perWorld, System.Random rng, string columnLabel)
        {
            List<CardDefinition> candidates = new List<CardDefinition>();
            foreach (CardDefinition face in config.DraftFaces)
            {
                if (face != null && face.ElementType == type)
                {
                    candidates.Add(face);
                }
            }

            Shuffle(candidates, rng);

            int take = Mathf.Min(perWorld, candidates.Count);
            if (candidates.Count < perWorld)
            {
                // No silent caps: el pool placeholder debe cubrir ≥perWorld por tipo.
                // Si falta, lo decimos en vez de ofrecer una columna corta callados.
                Debug.LogWarning(
                    $"[StarterDraft] Mundo {columnLabel}: el tipo {type} sólo tiene " +
                    $"{candidates.Count} cara(s) en el pool, se pidieron {perWorld}. " +
                    "Revisa NewRunConfig (Roguelike > Setup New Run Config).");
            }

            List<CardDefinition> result = new List<CardDefinition>(take);
            for (int i = 0; i < take; i++)
            {
                result.Add(candidates[i]);
            }
            return result;
        }

        // Fisher-Yates con RNG inyectado: misma seed → mismo orden (determinismo).
        // Idéntico al de ShopNodeController.
        private static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}

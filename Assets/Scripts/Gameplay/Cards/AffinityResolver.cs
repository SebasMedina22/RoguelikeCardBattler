using System.Collections.Generic;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Combat;

namespace RoguelikeCardBattler.Gameplay.Cards
{
    /// <summary>
    /// Resuelve la afinidad de tipo del mazo inicial (4a, DD-022 opción A). Helper
    /// <b>static puro</b> (espejo de <see cref="Run.NewRun.StarterDraft"/>): sin UI,
    /// sin estado, testeable directo.
    ///
    /// Una carta afín (su <see cref="CardDefinition.Affinity"/> == true) no tiene
    /// tipo fijo: en runtime debe usar el tipo elegido para el Mundo A cuando el
    /// jugador está en A, y el del Mundo B cuando está en B. Eso es exactamente lo
    /// que ya hace una carta DUAL al conmutar de lado por mundo. Por eso la afinidad
    /// se implementa representando cada carta afín como una <see cref="DualCardDefinition"/>
    /// runtime cuyos dos lados son el mismo cuerpo tipado: lado A con
    /// <paramref name="typeA"/>, lado B con <paramref name="typeB"/>. Así TurnManager
    /// (protegido) NO necesita cambios: ya lee <c>GetActiveCard(CurrentWorld).ElementType</c>.
    ///
    /// Las cartas neutras (Affinity=false, tipo None) y las ya-duales pasan sin tocar.
    /// </summary>
    public static class AffinityResolver
    {
        /// <summary>
        /// Resuelve UNA entrada de mazo. Si es una carta single con afinidad, la
        /// convierte en una dual runtime tipada por mundo. Si es neutra (single sin
        /// afinidad) o ya es dual, devuelve una copia sin cambios (Clone evita
        /// aliasar la lista del SO de config). Entrada null/ inválida → null.
        /// </summary>
        public static CardDeckEntry Resolve(CardDeckEntry authored, ElementType typeA, ElementType typeB)
        {
            if (authored == null || !authored.IsValid) return null;

            CardDefinition single = authored.SingleCard;
            if (single != null && single.Affinity)
            {
                // Cuerpo tipado por mundo. CreateAffinityVariant preserva el payload
                // de upgrade (a diferencia de CreateUpgradedClone) → la dual afín
                // resuelta sigue siendo mejorable en la Hoguera.
                DualCardDefinition dual = ScriptableObject.CreateInstance<DualCardDefinition>();
                dual.InitRuntimeSides(
                    $"affine_{single.Id}",
                    single.CardName,
                    single.CreateAffinityVariant(typeA),   // lado A
                    single.CreateAffinityVariant(typeB));  // lado B
                return CardDeckEntry.CreateDual(dual);
            }

            // Neutra o ya-dual: sin cambios.
            return authored.Clone();
        }

        /// <summary>
        /// Resuelve la lista entera mapeando <see cref="Resolve"/> sobre cada entrada
        /// válida (saltea null/ inválidas). El orden se preserva.
        /// </summary>
        public static List<CardDeckEntry> ResolveDeck(IReadOnlyList<CardDeckEntry> deck, ElementType typeA, ElementType typeB)
        {
            var result = new List<CardDeckEntry>();
            if (deck == null) return result;

            foreach (CardDeckEntry entry in deck)
            {
                if (entry == null || !entry.IsValid) continue;
                CardDeckEntry resolved = Resolve(entry, typeA, typeB);
                if (resolved != null) result.Add(resolved);
            }

            return result;
        }
    }
}

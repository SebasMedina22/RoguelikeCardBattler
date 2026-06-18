using System.Collections.Generic;
using RoguelikeCardBattler.Gameplay.Cards;

namespace RoguelikeCardBattler.Editor
{
    /// <summary>
    /// Helpers de autorado de cartas compartidos por los menús editor
    /// (<see cref="CardUpgradeSetup"/>, <see cref="StarterDeckSetup"/>). Centraliza
    /// el clonado de efectos para que el balance de upgrades NO derive en silencio
    /// entre menús (antes existían dos copias byte-idénticas de este código).
    /// </summary>
    public static class EditorCardAuthoring
    {
        /// <summary>
        /// Devuelve una copia de los efectos de <paramref name="card"/> sumando
        /// <paramref name="delta"/> al primer efecto cuyo tipo coincide. Si no hay
        /// efecto del tipo pedido, devuelve la lista clonada sin cambios (la lista
        /// resultante sigue siendo != vacía → CardUpgradeDef.HasUpgrade la detecta).
        /// </summary>
        public static List<EffectRef> CloneEffectsBoostingFirst(CardDefinition card, EffectType type, int delta)
        {
            var result = new List<EffectRef>();
            bool boosted = false;
            foreach (EffectRef src in card.Effects)
            {
                EffectRef copy = new EffectRef
                {
                    effectType = src.effectType,
                    value = src.value,
                    target = src.target,
                    statusType = src.statusType
                };
                if (!boosted && copy.effectType == type)
                {
                    copy.value += delta;
                    boosted = true;
                }
                result.Add(copy);
            }
            return result;
        }

        /// <summary>Valor del primer efecto del tipo pedido, o <paramref name="fallback"/> si no hay.</summary>
        public static int FirstEffectValue(IReadOnlyList<EffectRef> list, EffectType type, int fallback)
        {
            foreach (EffectRef e in list)
            {
                if (e.effectType == type) return e.value;
            }
            return fallback;
        }
    }
}

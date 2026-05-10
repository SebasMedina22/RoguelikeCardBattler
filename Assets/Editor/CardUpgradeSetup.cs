using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;

namespace RoguelikeCardBattler.Editor
{
    /// <summary>
    /// Pobla datos de mejora (CardUpgradeDef) en los 6 CardDefinition simples
    /// del mazo inicial (Strike/Defend/BattleFocus × ambos lados de cada dual).
    /// Reglas placeholder de Sub-PR 3C:
    ///   - Strike (ambos lados): +3 al primer efecto Damage.
    ///   - Defend (ambos lados): +5 al primer efecto Block.
    ///   - BattleFocus (ambos lados): coste = max(0, baseCost - 1) → gratis.
    ///
    /// Sobreescribe siempre el campo Upgrade — re-ejecutable sin limpieza
    /// manual. Para tweaks personalizados de upgrades, NO correr este menú.
    /// </summary>
    public static class CardUpgradeSetup
    {
        private const string FolderCards = "Assets/ScriptableObjects/Cards";

        [MenuItem("Roguelike/Setup Placeholder Card Upgrades")]
        public static void Setup()
        {
            int updated = 0, missing = 0;

            // Strike (sideA + sideB): +3 damage
            updated += ApplyDamageBoost("StrikeBasic", 3, ref missing);
            updated += ApplyDamageBoost("StrikeSideB", 3, ref missing);

            // Defend (sideA + sideB): +5 block
            updated += ApplyBlockBoost("DefendBasic", 5, ref missing);
            updated += ApplyBlockBoost("DefendSideB", 5, ref missing);

            // BattleFocus (sideA + sideB): -1 cost (mínimo 0)
            updated += ApplyCostReduction("BattleFocus", 1, ref missing);
            updated += ApplyCostReduction("BattleFocusSideB", 1, ref missing);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CardUpgradeSetup] updated={updated} missing={missing}");
        }

        // ────────────────────────────────────────
        // Helpers de mutación
        // ────────────────────────────────────────

        // Sobreescribe siempre: dev tool de placeholders, no preserva tweaks
        // manuales. Para tweaks manuales, no correr este menú.

        private static int ApplyDamageBoost(string assetName, int delta, ref int missing)
        {
            CardDefinition card = LoadCard(assetName);
            if (card == null) { missing++; return 0; }

            List<EffectRef> upgraded = CloneEffectsAddingTo(card, EffectType.Damage, delta);
            int newValue = FindFirstEffectValue(upgraded, EffectType.Damage, fallback: 0);
            string newDesc = $"Deal {newValue} damage to an enemy.";
            card.Upgrade.SetTestData(false, 0, upgraded, null, newDesc);
            EditorUtility.SetDirty(card);
            return 1;
        }

        private static int ApplyBlockBoost(string assetName, int delta, ref int missing)
        {
            CardDefinition card = LoadCard(assetName);
            if (card == null) { missing++; return 0; }

            List<EffectRef> upgraded = CloneEffectsAddingTo(card, EffectType.Block, delta);
            int newValue = FindFirstEffectValue(upgraded, EffectType.Block, fallback: 0);
            string newDesc = $"Gain {newValue} Block.";
            card.Upgrade.SetTestData(false, 0, upgraded, null, newDesc);
            EditorUtility.SetDirty(card);
            return 1;
        }

        private static int ApplyCostReduction(string assetName, int delta, ref int missing)
        {
            CardDefinition card = LoadCard(assetName);
            if (card == null) { missing++; return 0; }

            int newCost = Mathf.Max(0, card.Cost - delta);
            // BattleFocus: la descripción no incluye número de coste, así que la
            // dejamos igual al base (null = se conserva description original).
            card.Upgrade.SetTestData(true, newCost, null, null, null);
            EditorUtility.SetDirty(card);
            return 1;
        }

        private static int FindFirstEffectValue(List<EffectRef> list, EffectType type, int fallback)
        {
            foreach (EffectRef e in list)
            {
                if (e.effectType == type) return e.value;
            }
            return fallback;
        }

        /// <summary>
        /// Devuelve una nueva lista de EffectRef copiando los del card y sumando
        /// <paramref name="delta"/> al primer efecto cuyo type coincide. Si no
        /// hay efecto del tipo pedido, devuelve la lista clonada sin cambios
        /// (CardUpgradeDef seguirá detectando HasUpgrade porque la lista es != vacía).
        /// </summary>
        private static List<EffectRef> CloneEffectsAddingTo(CardDefinition card, EffectType type, int delta)
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

        private static CardDefinition LoadCard(string assetName)
        {
            string[] guids = AssetDatabase.FindAssets($"{assetName} t:CardDefinition", new[] { FolderCards });
            if (guids.Length == 0)
            {
                Debug.LogWarning($"[CardUpgradeSetup] CardDefinition '{assetName}' no encontrado en {FolderCards}.");
                return null;
            }
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
        }
    }
}

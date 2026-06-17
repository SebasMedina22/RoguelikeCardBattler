#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Run.NewRun;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// M4 bloque 4a — Guard de cobertura de mejoras (DD-023). Garantía PERMANENTE:
    /// TODA CardDefinition del proyecto tiene mejora autorada, así la Hoguera no
    /// vuelve a "verse rota" en silencio (un mazo con cartas no mejorables). Test
    /// editor-only (usa AssetDatabase): el archivo entero va bajo #if UNITY_EDITOR
    /// por higiene — EditModeTests.asmdef corre en plataforma Editor.
    /// </summary>
    public class CardUpgradeCoverageTests
    {
        [Test] // 9 — Guard global: ningún CardDefinition queda sin upgrade
        public void EveryCardDefinition_HasUpgradeAuthored()
        {
            string[] guids = AssetDatabase.FindAssets("t:CardDefinition");
            var missing = new List<string>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardDefinition card = AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
                if (card == null) continue;
                if (card.Upgrade == null || !card.Upgrade.HasUpgrade)
                {
                    missing.Add($"{card.name}  ({path})");
                }
            }

            Assert.IsEmpty(missing,
                "Estos CardDefinition no tienen mejora autorada (DD-023). Corré los menús " +
                "'Roguelike > Setup Starter Deck (4a)' y 'Roguelike > Setup Placeholder Card Upgrades':\n  " +
                string.Join("\n  ", missing));
        }

        [Test] // 10 — La dual compuesta de 2 caras es mejorable (caras con upgrade poblado)
        public void ComposedDualFromFaces_IsUpgradeable()
        {
            CardDefinition faceA = LoadFace("Face_Rojo_1");
            CardDefinition faceB = LoadFace("Face_Azul_1");
            Assert.IsNotNull(faceA, "Falta Face_Rojo_1 (corré 'Setup New Run Config').");
            Assert.IsNotNull(faceB, "Falta Face_Azul_1 (corré 'Setup New Run Config').");

            DualCardDefinition dual = StarterDraft.ComposeDualCard(faceA, faceB);
            CardDeckEntry entry = CardDeckEntry.CreateDual(dual);

            Assert.IsTrue(entry.CanUpgrade(),
                "La dual drafteada (2 caras) debe ser mejorable: ambos lados tienen upgrade " +
                "autorado por 'Setup Starter Deck (4a)'.");
        }

        private static CardDefinition LoadFace(string assetName)
        {
            string[] guids = AssetDatabase.FindAssets(
                $"{assetName} t:CardDefinition", new[] { "Assets/ScriptableObjects/Cards/NewRunFaces" });
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<CardDefinition>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
#endif

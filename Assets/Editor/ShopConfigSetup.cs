using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Run.Shop;

namespace RoguelikeCardBattler.Editor
{
    /// <summary>
    /// Crea (o reutiliza) el asset ShopConfig.asset de la Tienda (Sub-PR 3D) y
    /// puebla sus pools placeholder con las cartas y Retazos existentes en el
    /// proyecto. Idempotente: re-ejecutable sin limpieza manual — sólo reemplaza
    /// los pools, deja precios/slots/sprites intactos.
    ///
    /// Pools placeholder: las cartas son los 6 lados de cartas iniciales (único
    /// contenido de cartas hoy) y los Retazos son los de las categorías
    /// "comprables" en tienda. El balance/curado real se hará post-3D.
    /// </summary>
    public static class ShopConfigSetup
    {
        private const string ConfigFolder = "Assets/ScriptableObjects/Configs";
        private const string ConfigPath = ConfigFolder + "/ShopConfig.asset";
        private const string CardsFolder = "Assets/ScriptableObjects/Cards";
        private const string RelicsFolder = "Assets/ScriptableObjects/Relics";

        [MenuItem("Roguelike/Setup Shop Config")]
        public static void Setup()
        {
            ShopConfig config = AssetDatabase.LoadAssetAtPath<ShopConfig>(ConfigPath);
            if (config == null)
            {
                if (!AssetDatabase.IsValidFolder(ConfigFolder))
                {
                    AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Configs");
                }
                config = ScriptableObject.CreateInstance<ShopConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
                Debug.Log($"[ShopConfigSetup] Asset creado en {ConfigPath}.");
            }

            List<CardDeckEntry> cards = LoadCardPool();
            List<RelicDefinition> relics = LoadRelicPool();
            config.EditorPopulatePools(cards, relics);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ShopConfigSetup] cards={cards.Count} relics={relics.Count}");
        }

        private static List<CardDeckEntry> LoadCardPool()
        {
            var result = new List<CardDeckEntry>();
            string[] guids = AssetDatabase.FindAssets("t:CardDefinition", new[] { CardsFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardDefinition card = AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
                if (card != null)
                {
                    result.Add(CardDeckEntry.CreateSingle(card));
                }
            }
            return result;
        }

        private static List<RelicDefinition> LoadRelicPool()
        {
            var result = new List<RelicDefinition>();
            string[] guids = AssetDatabase.FindAssets("t:RelicDefinition", new[] { RelicsFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RelicDefinition relic = AssetDatabase.LoadAssetAtPath<RelicDefinition>(path);
                if (relic != null)
                {
                    result.Add(relic);
                }
            }
            return result;
        }
    }
}

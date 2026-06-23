using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Editor
{
    /// <summary>
    /// M4 4c — genera (o reutiliza) 2 EnemyDefinitions de prueba para validar el
    /// tipo activo por mundo en BattleScene: <c>TransdimTestEnemy</c> (transdim,
    /// Rojo en Mundo A / Azul en Mundo B) y <c>AnchorTestEnemy</c> (ancla, Morado
    /// fijo). Idempotente y NO toca los 5 SOs de enemigos existentes. Molde espejo
    /// de <c>EventConfigSetup</c>.
    /// </summary>
    public static class EnemyConfigSetup
    {
        private const string EnemiesFolder = "Assets/ScriptableObjects/Enemies";

        [MenuItem("Roguelike/Setup Enemy Test Data (4c)")]
        public static void Setup()
        {
            EnsureFolder("Assets/ScriptableObjects", "Enemies", EnemiesFolder);

            EnemyDefinition transdim = CreateOrUpdateEnemy(
                "TransdimTestEnemy",
                id: "enemy_transdim_test",
                name: "Transdim de prueba",
                maxHp: 40,
                elementType: ElementType.Rojo,   // Mundo A
                typeWorldB: ElementType.Azul,     // Mundo B
                isAnchor: false);

            EnemyDefinition anchor = CreateOrUpdateEnemy(
                "AnchorTestEnemy",
                id: "enemy_anchor_test",
                name: "Ancla de prueba",
                maxHp: 40,
                elementType: ElementType.Morado,  // tipo fijo
                typeWorldB: ElementType.None,
                isAnchor: true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[EnemyConfigSetup] Listos: {transdim.name} (transdim Rojo/Azul) " +
                      $"y {anchor.name} (ancla Morado). Carpeta: {EnemiesFolder}.");
        }

        private static EnemyDefinition CreateOrUpdateEnemy(
            string assetName, string id, string name, int maxHp,
            ElementType elementType, ElementType typeWorldB, bool isAnchor)
        {
            string path = $"{EnemiesFolder}/{assetName}.asset";
            EnemyDefinition def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<EnemyDefinition>();
                AssetDatabase.CreateAsset(def, path);
                Debug.Log($"[EnemyConfigSetup] Enemy creado: {path}");
            }

            def.SetDebugData(
                id,
                name,
                maxHp,
                0,
                EnemyAIPattern.Sequence,
                new List<string>(),
                BuildMoves(),
                1f,
                null,
                elementType,
                typeWorldB,
                isAnchor);
            EditorUtility.SetDirty(def);
            return def;
        }

        // Un único ataque para que el enemigo golpee al jugador en el E2E (sirve
        // para ver el cambio de efectividad enemigo→jugador al conmutar de mundo).
        private static List<EnemyMove> BuildMoves()
        {
            var attack = new EnemyMove();
            attack.SetDebugData(
                "mv_attack",
                "Golpe",
                "Un golpe directo.",
                new List<EffectRef>
                {
                    new EffectRef
                    {
                        effectType = EffectType.Damage,
                        value = 8,
                        target = EffectTarget.SingleEnemy,
                    },
                },
                newWeight: 1,
                newSequenceIndex: 0,
                newIntentType: EnemyIntentType.Attack);
            return new List<EnemyMove> { attack };
        }

        private static void EnsureFolder(string parent, string newFolder, string fullPath)
        {
            if (!AssetDatabase.IsValidFolder(fullPath))
                AssetDatabase.CreateFolder(parent, newFolder);
        }
    }
}

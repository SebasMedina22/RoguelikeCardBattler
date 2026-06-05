using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Run.NewRun;

namespace RoguelikeCardBattler.Editor
{
    /// <summary>
    /// Crea (o reutiliza) el asset NewRunConfig.asset (Sub-PR 3E) y puebla su pool
    /// de caras placeholder para el draft. Espejo de <c>ShopConfigSetup</c>:
    /// idempotente — re-ejecutable sin limpieza manual, sólo reemplaza el pool de
    /// caras y deja tipos/optionsPerWorld/confirmClip intactos.
    ///
    /// A diferencia de la Tienda (que reusa cartas existentes), el draft necesita
    /// COBERTURA garantizada: ≥3 caras por cada uno de los 6 tipos para que
    /// cualquier elección rinda 3 opciones por columna. Como hoy no existe ese
    /// contenido, el menú genera caras placeholder tipadas (Attack/Skill) en
    /// <c>Assets/ScriptableObjects/Cards/NewRunFaces/</c>. Las reutiliza si ya
    /// existen (no duplica).
    /// </summary>
    public static class NewRunConfigSetup
    {
        private const string ConfigFolder = "Assets/ScriptableObjects/Configs";
        private const string ConfigPath = ConfigFolder + "/NewRunConfig.asset";
        private const string FacesParent = "Assets/ScriptableObjects/Cards";
        private const string FacesFolder = FacesParent + "/NewRunFaces";
        private const int FacesPerType = 3;

        private static readonly ElementType[] DraftTypes =
        {
            ElementType.Rojo,
            ElementType.Amarillo,
            ElementType.Azul,
            ElementType.Morado,
            ElementType.Negro,
            ElementType.Blanco
        };

        [MenuItem("Roguelike/Setup New Run Config")]
        public static void Setup()
        {
            EnsureFolder(ConfigFolder, "Assets/ScriptableObjects", "Configs");
            EnsureFolder(FacesFolder, FacesParent, "NewRunFaces");

            NewRunConfig config = AssetDatabase.LoadAssetAtPath<NewRunConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<NewRunConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
                Debug.Log($"[NewRunConfigSetup] Asset creado en {ConfigPath}.");
            }

            List<CardDefinition> faces = BuildPlaceholderFaces();
            config.EditorPopulateFaces(faces);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[NewRunConfigSetup] faces={faces.Count} ({FacesPerType} por tipo × {DraftTypes.Length} tipos).");
        }

        /// <summary>
        /// Genera (o reutiliza) FacesPerType caras por tipo. Idempotente: cada cara
        /// se identifica por path; si ya existe se carga en vez de recrearse.
        /// </summary>
        private static List<CardDefinition> BuildPlaceholderFaces()
        {
            List<CardDefinition> faces = new List<CardDefinition>();

            foreach (ElementType type in DraftTypes)
            {
                for (int i = 0; i < FacesPerType; i++)
                {
                    string assetPath = $"{FacesFolder}/Face_{type}_{i + 1}.asset";
                    CardDefinition face = AssetDatabase.LoadAssetAtPath<CardDefinition>(assetPath);
                    if (face == null)
                    {
                        face = ScriptableObject.CreateInstance<CardDefinition>();
                        ConfigurePlaceholderFace(face, type, i);
                        AssetDatabase.CreateAsset(face, assetPath);
                    }
                    faces.Add(face);
                }
            }

            return faces;
        }

        /// <summary>
        /// Da a la cara placeholder datos coherentes con su tipo y variante:
        /// variantes 0/2 = ataque (Damage), variante 1 = defensa (Block). Valores
        /// modestos: es contenido de relleno hasta el curado real del draft.
        /// </summary>
        private static void ConfigurePlaceholderFace(CardDefinition face, ElementType type, int variant)
        {
            bool isDefense = variant == 1;
            int value = isDefense ? 5 : 6 + variant; // 6, 5, 8 por las 3 variantes

            List<EffectRef> effects = new List<EffectRef>
            {
                isDefense
                    ? new EffectRef { effectType = EffectType.Block, value = value, target = EffectTarget.Self }
                    : new EffectRef { effectType = EffectType.Damage, value = value, target = EffectTarget.SingleEnemy }
            };

            string roleName = isDefense ? "Guardia" : "Golpe";
            string cardName = $"{roleName} {type}";
            string description = isDefense
                ? $"Bloquea {value}."
                : $"Inflige {value} de daño.";

            face.SetDebugData(
                $"face_{type}_{variant + 1}".ToLowerInvariant(),
                cardName,
                description,
                1,
                isDefense ? CardType.Skill : CardType.Attack,
                CardRarity.Uncommon,
                isDefense ? CardTarget.Self : CardTarget.SingleEnemy,
                new List<string>(),
                effects,
                type);
        }

        private static void EnsureFolder(string fullPath, string parent, string newFolderName)
        {
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parent, newFolderName);
            }
        }
    }
}

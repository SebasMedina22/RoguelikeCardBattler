using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Editor
{
    /// <summary>
    /// M5 Sub-PR A — autoría del boss del Acto 1 "Costura Maldita / UNIT-RB7":
    /// un <see cref="EnemyDefinition"/> transdimensional (dos tipos, uno por mundo)
    /// con IA <c>PhaseBased</c>. Mundo A (Costura Maldita) = Blanco; Mundo B
    /// (UNIT-RB7) = Azul, contra el jugador de REFERENCIA (defaults del TurnManager:
    /// Mundo A Rojo / Mundo B Amarillo), elegidos por la matriz de
    /// <see cref="ElementEffectiveness"/> en CÓDIGO (DC2 — la tabla §3 del doc está
    /// desfasada y NO es la fuente de verdad). La regla §6/DD-004 es ASIMÉTRICA
    /// (1 tipo SuperEficaz contra el jugador + 1 que sea debilidad del jugador), y
    /// se cumple por el eje JUGADOR→BOSS, NO porque el boss domine ambos mundos:
    ///   - Mundo A: el boss (Blanco) pega SuperEficaz a Rojo, PERO Rojo también pega
    ///     SuperEficaz a Blanco → Mundo A es EXPLOTABLE (el jugador pelea de vuelta).
    ///   - Mundo B: el boss (Azul) pega SuperEficaz a Amarillo y el jugador solo
    ///     devuelve PocoEficaz → Mundo B FAVORECE al boss (la "debilidad" de §6).
    ///
    /// Moves etiquetados por HP% para el selector PhaseBased: fase 1 en [51,100],
    /// fase 2 (obligatoria al 50%, DC3) en [0,50] con números endurecidos. El boss
    /// reusa la maquinaria transdim de 4c (typeWorldB + getter EnemyElementType +
    /// ficha de dos tipos) tal cual — no la modifica.
    ///
    /// Idempotente; molde espejo de <c>EnemyConfigSetup</c> (4c). No toca SOs
    /// existentes ni la config de run. NADA de setup manual en el inspector.
    ///
    /// FUERA de scope de A (es Sub-PR B/C): Desfase Dimensional, debuffs
    /// Sangrado/Virus, contador de cartas y cambio de mundo forzado por IA. Por eso
    /// los moves de A son ataque/defensa genéricos por fase.
    /// </summary>
    public static class BossConfigSetup
    {
        private const string EnemiesFolder = "Assets/ScriptableObjects/Enemies";

        // Rangos de fase. El selector PhaseBased de TurnManager filtra cada move por
        // [MinHpPercent, MaxHpPercent] contra el HP% actual del boss. El 50% exacto
        // cae en fase 2 (≤ 50), cumpliendo "Fase 2 obligatoria al 50%" (DC3).
        private const int Phase1MinHp = 51;
        private const int Phase1MaxHp = 100;
        private const int Phase2MinHp = 0;
        private const int Phase2MaxHp = 50;

        [MenuItem("Roguelike/Setup Boss Act 1 (M5 A)")]
        public static void Setup()
        {
            EnsureFolder("Assets/ScriptableObjects", "Enemies", EnemiesFolder);

            EnemyDefinition boss = CreateOrUpdateBoss(
                assetName: "BossAct1_CosturaMaldita",
                id: "boss_act1_costura_maldita",
                name: "Costura Maldita",
                maxHp: 140,                       // D4 default tuneable (~2x HP de jugador)
                elementType: ElementType.Blanco,  // Mundo A — SuperEficaz vs Rojo (jugador ref.)
                typeWorldB: ElementType.Azul);    // Mundo B — SuperEficaz vs Amarillo (jugador ref.)

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BossConfigSetup] Boss listo: {boss.name} (transdim Blanco/Azul, " +
                      $"PhaseBased, {boss.Moves.Count} moves). Carpeta: {EnemiesFolder}. " +
                      "Para el eyeball: asignalo como DefaultEnemy del RunCombatConfig " +
                      "(o al SpecificEnemy del nodo de boss) y entrá a BattleScene.");
        }

        private static EnemyDefinition CreateOrUpdateBoss(
            string assetName, string id, string name, int maxHp,
            ElementType elementType, ElementType typeWorldB)
        {
            string path = $"{EnemiesFolder}/{assetName}.asset";
            EnemyDefinition def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<EnemyDefinition>();
                AssetDatabase.CreateAsset(def, path);
                Debug.Log($"[BossConfigSetup] Boss creado: {path}");
            }

            def.SetDebugData(
                id,
                name,
                maxHp,
                0,
                EnemyAIPattern.PhaseBased,
                new List<string> { "boss" },
                BuildMoves(),
                1f,
                null,
                elementType,
                typeWorldB,
                false);   // isAnchor — el boss es transdim, NO ancla (DC1/DC2)
            EditorUtility.SetDirty(def);
            return def;
        }

        // Kit de ataque/defensa por fase (DC3). Fase 2 endurece los números. Sin
        // Desfase ni debuffs (eso es B/C). Etiquetado por HP% → el selector
        // PhaseBased de TurnManager elige solo entre los moves de la fase actual.
        private static List<EnemyMove> BuildMoves()
        {
            return new List<EnemyMove>
            {
                BuildAttack("mv_p1_attack", "Puntada", "Una puntada que perfora.", 10,
                    Phase1MinHp, Phase1MaxHp),
                BuildBlock("mv_p1_defend", "Tejido Protector", "Refuerza su trama.", 8,
                    Phase1MinHp, Phase1MaxHp),
                BuildAttack("mv_p2_attack", "Puntada Frenética", "Cose con furia.", 14,
                    Phase2MinHp, Phase2MaxHp),
                BuildBlock("mv_p2_defend", "Tejido Reforzado", "Trama impenetrable.", 12,
                    Phase2MinHp, Phase2MaxHp),
            };
        }

        private static EnemyMove BuildAttack(
            string id, string name, string description, int damage, int minHp, int maxHp)
        {
            var move = new EnemyMove();
            move.SetDebugData(
                id,
                name,
                description,
                new List<EffectRef>
                {
                    new EffectRef
                    {
                        effectType = EffectType.Damage,
                        value = damage,
                        target = EffectTarget.SingleEnemy,
                    },
                },
                newWeight: 1,
                newSequenceIndex: -1,
                newIntentType: EnemyIntentType.Attack,
                newMinHpPercent: minHp,
                newMaxHpPercent: maxHp);
            return move;
        }

        private static EnemyMove BuildBlock(
            string id, string name, string description, int block, int minHp, int maxHp)
        {
            var move = new EnemyMove();
            move.SetDebugData(
                id,
                name,
                description,
                new List<EffectRef>
                {
                    new EffectRef
                    {
                        effectType = EffectType.Block,
                        value = block,
                        target = EffectTarget.Self,
                    },
                },
                newWeight: 1,
                newSequenceIndex: -1,
                newIntentType: EnemyIntentType.Defend,
                newMinHpPercent: minHp,
                newMaxHpPercent: maxHp);
            return move;
        }

        private static void EnsureFolder(string parent, string newFolder, string fullPath)
        {
            if (!AssetDatabase.IsValidFolder(fullPath))
                AssetDatabase.CreateFolder(parent, newFolder);
        }
    }
}

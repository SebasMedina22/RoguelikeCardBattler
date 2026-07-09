using System.Collections.Generic;
using System.Linq;
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

        // Arte del boss (idle por mundo). El avatar A ya suele estar asignado desde M5
        // Sub-PR A; el swap por mundo (C4) agrega el de Mundo B (personaje_unit_rb7).
        private const string AvatarAPath =
            "Assets/Art/Sprites/Enemies/CosturaMaldita/A/Idle/personaje_costura_maldita.png";
        private const string AvatarBPath =
            "Assets/Art/Sprites/Enemies/CosturaMaldita/B/Idle/personaje_unit_rb7.png";

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

            AssignAvatars(boss);   // C4 — cara por mundo (A ya asignada; agrega la de B)

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BossConfigSetup] Boss listo: {boss.name} (transdim Blanco/Azul, " +
                      $"PhaseBased, {boss.Moves.Count} moves, avatar A+B). Carpeta: {EnemiesFolder}. " +
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

        // Asigna los avatares por path (SerializedObject), idempotente (C4 — swap por mundo):
        //   - avatarWorldB (Mundo B): (re)asigna siempre el sub-sprite del personaje.
        //   - avatar (Mundo A): solo si está null → no pisa la referencia ya existente
        //     (M5 Sub-PR A lo dejó apuntando al sub-sprite del PNG Multiple, y funciona).
        // Espejo del patrón EventConfigSetup.FindBackground + SerializedObject. NADA manual.
        private static void AssignAvatars(EnemyDefinition boss)
        {
            var so = new SerializedObject(boss);

            SerializedProperty avatarB = so.FindProperty("avatarWorldB");
            Sprite spriteB = LoadCharacterSprite(AvatarBPath);
            if (avatarB != null && spriteB != null)
            {
                avatarB.objectReferenceValue = spriteB;
            }
            else if (spriteB == null)
            {
                Debug.LogWarning($"[BossConfigSetup] No se resolvió el avatar de Mundo B en {AvatarBPath}.");
            }

            SerializedProperty avatarA = so.FindProperty("avatar");
            if (avatarA != null && avatarA.objectReferenceValue == null)
            {
                // A no asignado (p.ej. asset recién creado) → resolverlo por path.
                avatarA.objectReferenceValue = LoadCharacterSprite(AvatarAPath);
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(boss);
        }

        // Resuelve el sprite del PERSONAJE dentro del PNG. El arte entra como Sprite Multiple
        // (auto-slice) y el personaje es el sub-sprite de mayor área; se ignoran las "motas"
        // que el slicer detecta como sub-sprites espurios (p.ej. personaje_unit_rb7_1 de 16x18).
        // Residual R1 del spec resuelto por la ALTERNATIVA: referenciar el sub-sprite recortado
        // (mismo criterio que el avatar A), NO forzar Single — el canvas completo dejaría al
        // personaje con padding y más chico/descentrado que la cara A. Si el PNG fuera Single,
        // LoadAllAssetsAtPath devuelve un único sprite y se usa ese (fallback grácil).
        private static Sprite LoadCharacterSprite(string pngPath)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(pngPath).OfType<Sprite>().ToList();
            if (sprites.Count == 0)
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            }
            Sprite best = sprites[0];
            foreach (var s in sprites)
            {
                if (s.rect.width * s.rect.height > best.rect.width * best.rect.height)
                {
                    best = s;
                }
            }
            return best;
        }

        private static void EnsureFolder(string parent, string newFolder, string fullPath)
        {
            if (!AssetDatabase.IsValidFolder(fullPath))
                AssetDatabase.CreateFolder(parent, newFolder);
        }
    }
}

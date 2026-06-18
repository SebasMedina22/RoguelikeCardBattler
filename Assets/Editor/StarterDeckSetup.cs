using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Editor
{
    /// <summary>
    /// Autorado del mazo inicial del Acto 1 (M4 bloque 4a). Menú idempotente que:
    ///   1. Crea/asegura los 4 SOs de cuerpo del starter (Strike/Defend × afín/neutra)
    ///      con su mejora (Strike 6→9, Defend 5→10).
    ///   2. Autora la mejora en las 18 caras de NewRunFaces/ (la dual drafteada se
    ///      vuelve mejorable en la Hoguera — sus dos lados pasan a tener upgrade).
    ///   3. Reescribe RunCombatConfig_Act1.starterDeck a la composición GDD §5:
    ///      3 Strike_Affine + 2 Strike_Neutral + 2 Defend_Affine + 2 Defend_Neutral
    ///      (9 entradas single; la 10ª es la dual drafteada, inyectada por
    ///      InitializeDeck vía PendingStarterCard).
    ///
    /// Re-ejecutable sin limpieza manual: sobreescribe datos de los SOs existentes.
    /// Espejo del patrón *Setup.cs (NewRunConfigSetup / ShopConfigSetup / CardUpgradeSetup).
    /// Los 6 SOs viejos del starter (StrikeBasic/DefendBasic/BattleFocus×lados) NO se
    /// tocan — quedan intactos para el rewardPool y ya traen su upgrade commiteado.
    /// </summary>
    public static class StarterDeckSetup
    {
        private const string FolderCards = "Assets/ScriptableObjects/Cards";
        private const string FacesFolder = FolderCards + "/NewRunFaces";
        private const string ConfigPath = "Assets/ScriptableObjects/Run/RunCombatConfig_Act1.asset";

        // Valores de balance espejados de los cuerpos básicos actuales (StrikeBasic/
        // DefendBasic) y de los placeholders de upgrade ya probados en CardUpgradeSetup.
        private const int StrikeBaseDamage = 6;
        private const int StrikeUpgradeDelta = 3;   // 6 → 9
        private const int DefendBaseBlock = 5;
        private const int DefendUpgradeDelta = 5;   // 5 → 10

        [MenuItem("Roguelike/Setup Starter Deck (4a)")]
        public static void Setup()
        {
            // 1. Cuerpos del starter (afín = adopta tipo del mundo; neutra = None 90%).
            CardDefinition strikeAffine = EnsureStrikeBody("Strike_Affine", affinity: true);
            CardDefinition strikeNeutral = EnsureStrikeBody("Strike_Neutral", affinity: false);
            CardDefinition defendAffine = EnsureDefendBody("Defend_Affine", affinity: true);
            CardDefinition defendNeutral = EnsureDefendBody("Defend_Neutral", affinity: false);

            // 2. Mejoras en las 18 caras del draft.
            int facesUpgraded = AuthorFaceUpgrades();

            // 3. Recomposición del starter deck (9 entradas single).
            bool deckWritten = RecomposeStarterDeck(strikeAffine, strikeNeutral, defendAffine, defendNeutral);

            // 4. Reward pool afín (4a follow-up): las recompensas de combate adoptan los
            //    tipos de mundo del jugador al ganarlas, en vez de los duales Rojo/Negro fijos.
            bool rewardWritten = RecomposeRewardPool(strikeAffine, defendAffine, strikeNeutral);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[StarterDeckSetup] bodies=4 facesUpgraded={facesUpgraded} " +
                      $"starterDeck={(deckWritten ? "9 entradas" : "NO escrito (config faltante)")} " +
                      $"rewardPool={(rewardWritten ? "3 afines/neutra" : "NO escrito (config faltante)")}.");
        }

        // ──────────────────────────────────────────────
        // 1. Cuerpos del starter
        // ──────────────────────────────────────────────

        // Texto en español, espejo del vocabulario de las caras del draft
        // (NewRunConfigSetup: "Golpe"/"Guardia", "Inflige N de daño."/"Bloquea N.")
        // para que el mazo inicial — cuerpos + la dual drafteada — lea consistente
        // en la misma mano. (El número/identidad es placeholder hasta el curado real.)
        private static CardDefinition EnsureStrikeBody(string assetName, bool affinity)
        {
            return EnsureBody(
                assetName,
                id: assetName.ToLowerInvariant(),
                cardName: "Golpe",
                description: $"Inflige {StrikeBaseDamage} de daño.",
                type: CardType.Attack,
                cardTarget: CardTarget.SingleEnemy,
                effectType: EffectType.Damage,
                effectTarget: EffectTarget.SingleEnemy,
                baseValue: StrikeBaseDamage,
                upgradeDelta: StrikeUpgradeDelta,
                upgradedDescription: $"Inflige {StrikeBaseDamage + StrikeUpgradeDelta} de daño.",
                affinity: affinity);
        }

        private static CardDefinition EnsureDefendBody(string assetName, bool affinity)
        {
            return EnsureBody(
                assetName,
                id: assetName.ToLowerInvariant(),
                cardName: "Guardia",
                description: $"Bloquea {DefendBaseBlock}.",
                type: CardType.Skill,
                cardTarget: CardTarget.Self,
                effectType: EffectType.Block,
                effectTarget: EffectTarget.Self,
                baseValue: DefendBaseBlock,
                upgradeDelta: DefendUpgradeDelta,
                upgradedDescription: $"Bloquea {DefendBaseBlock + DefendUpgradeDelta}.",
                affinity: affinity);
        }

        /// <summary>
        /// Crea (si falta) y puebla un SO de cuerpo + su mejora. elementType siempre
        /// None: la carta afín adopta el tipo del mundo en runtime (vía AffinityResolver),
        /// y la neutra ES None por definición. Idempotente — re-puebla si ya existe.
        /// </summary>
        private static CardDefinition EnsureBody(
            string assetName, string id, string cardName, string description,
            CardType type, CardTarget cardTarget, EffectType effectType, EffectTarget effectTarget,
            int baseValue, int upgradeDelta, string upgradedDescription, bool affinity)
        {
            string path = $"{FolderCards}/{assetName}.asset";
            CardDefinition card = AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
            if (card == null)
            {
                card = ScriptableObject.CreateInstance<CardDefinition>();
                AssetDatabase.CreateAsset(card, path);
            }

            var effects = new List<EffectRef>
            {
                new EffectRef { effectType = effectType, value = baseValue, target = effectTarget }
            };
            card.SetDebugData(
                id, cardName, description, 1, type, CardRarity.Common, cardTarget,
                new List<string>(), effects, ElementType.None, null, affinity);

            var upgradedEffects = new List<EffectRef>
            {
                new EffectRef { effectType = effectType, value = baseValue + upgradeDelta, target = effectTarget }
            };
            card.Upgrade.SetTestData(false, 0, upgradedEffects, null, upgradedDescription);

            EditorUtility.SetDirty(card);
            return card;
        }

        // ──────────────────────────────────────────────
        // 2. Mejoras en las 18 caras del draft
        // ──────────────────────────────────────────────

        private static int AuthorFaceUpgrades()
        {
            if (!AssetDatabase.IsValidFolder(FacesFolder))
            {
                Debug.LogWarning($"[StarterDeckSetup] No existe {FacesFolder}. " +
                                 "Corré 'Roguelike > Setup New Run Config' primero para generar las caras.");
                return 0;
            }

            string[] guids = AssetDatabase.FindAssets("t:CardDefinition", new[] { FacesFolder });
            int count = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardDefinition face = AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
                if (face == null) continue;
                AuthorFaceUpgrade(face);
                count++;
            }
            return count;
        }

        /// <summary>
        /// Autora la mejora de UNA cara, escalando su efecto primario: Damage +3
        /// (Golpe) o Block +5 (Guardia), espejando el balance del starter. La
        /// descripción mejorada respeta el estilo en español de las caras
        /// (NewRunConfigSetup) para que el HUD muestre el número nuevo.
        /// </summary>
        private static void AuthorFaceUpgrade(CardDefinition face)
        {
            EffectType primary = EffectType.Damage;
            int baseValue = 0;
            bool isBlock = false;
            foreach (EffectRef e in face.Effects)
            {
                if (e.effectType == EffectType.Block) { primary = EffectType.Block; baseValue = e.value; isBlock = true; break; }
                if (e.effectType == EffectType.Damage) { primary = EffectType.Damage; baseValue = e.value; break; }
            }

            int delta = isBlock ? DefendUpgradeDelta : StrikeUpgradeDelta;
            int newValue = baseValue + delta;
            List<EffectRef> upgradedEffects = EditorCardAuthoring.CloneEffectsBoostingFirst(face, primary, delta);
            string desc = isBlock ? $"Bloquea {newValue}." : $"Inflige {newValue} de daño.";

            face.Upgrade.SetTestData(false, 0, upgradedEffects, null, desc);
            EditorUtility.SetDirty(face);
        }

        // ──────────────────────────────────────────────
        // 3. Recomposición del starter deck
        // ──────────────────────────────────────────────

        private static bool RecomposeStarterDeck(
            CardDefinition strikeAffine, CardDefinition strikeNeutral,
            CardDefinition defendAffine, CardDefinition defendNeutral)
        {
            RunCombatConfig config = AssetDatabase.LoadAssetAtPath<RunCombatConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogWarning($"[StarterDeckSetup] No se encontró {ConfigPath}; starter deck no recompuesto.");
                return false;
            }

            var deck = new List<CardDeckEntry>();
            AddCopies(deck, strikeAffine, 3);   // 3 Strike afín
            AddCopies(deck, strikeNeutral, 2);  // 2 Strike neutra
            AddCopies(deck, defendAffine, 2);   // 2 Defend afín
            AddCopies(deck, defendNeutral, 2);  // 2 Defend neutra  → 9 entradas

            config.EditorPopulateStarterDeck(deck);
            EditorUtility.SetDirty(config);
            return true;
        }

        private static void AddCopies(List<CardDeckEntry> deck, CardDefinition card, int count)
        {
            for (int i = 0; i < count; i++)
            {
                deck.Add(CardDeckEntry.CreateSingle(card));
            }
        }

        // ──────────────────────────────────────────────
        // 4. Reward pool afín (4a follow-up)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Reescribe RunCombatConfig_Act1.rewardPool a cartas AFINES single (+1 neutra):
        /// Strike afín, Defend afín, Strike neutra. Al ganarlas, RunState.AddCardToDeck
        /// las rutea por AffinityResolver → las afines adoptan los tipos elegidos por el
        /// jugador (Mundo A/B), la neutra entra como None. Reusa los mismos cuerpos del
        /// starter (no crea SOs nuevos). GetRewardOptions toma las primeras ChoicesCount.
        /// </summary>
        private static bool RecomposeRewardPool(
            CardDefinition strikeAffine, CardDefinition defendAffine, CardDefinition strikeNeutral)
        {
            RunCombatConfig config = AssetDatabase.LoadAssetAtPath<RunCombatConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogWarning($"[StarterDeckSetup] No se encontró {ConfigPath}; reward pool no recompuesto.");
                return false;
            }

            var pool = new List<CardDeckEntry>
            {
                CardDeckEntry.CreateSingle(strikeAffine),
                CardDeckEntry.CreateSingle(defendAffine),
                CardDeckEntry.CreateSingle(strikeNeutral),
            };

            config.EditorPopulateRewardPool(pool);
            EditorUtility.SetDirty(config);
            return true;
        }
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Run.Events;

namespace RoguelikeCardBattler.Editor
{
    /// <summary>
    /// Crea (o reutiliza) el EventPoolConfig.asset (4b-1) y un set de
    /// EventDefinitions simples placeholder, poblando sus decisiones con cartas y
    /// Retazos existentes en el proyecto. Idempotente: re-ejecutable sin limpieza
    /// manual (carga-o-crea cada asset por ruta y reescribe su contenido). Patrón
    /// espejo de <c>ShopConfigSetup</c>.
    ///
    /// Los 3 eventos placeholder cubren todas las consecuencias de 4b-1
    /// (GiveCard / RemoveCard / GiveGold / LoseGold / ModifyHP / GiveRelic) para
    /// permitir la validación end-to-end en RunScene.
    /// </summary>
    public static class EventConfigSetup
    {
        private const string ConfigFolder = "Assets/ScriptableObjects/Configs";
        private const string ConfigPath = ConfigFolder + "/EventPoolConfig.asset";
        private const string EventsFolder = "Assets/ScriptableObjects/Events";
        private const string CardsFolder = "Assets/ScriptableObjects/Cards";
        private const string RelicsFolder = "Assets/ScriptableObjects/Relics";
        private const string EventArtFolder = "Assets/Art/Events";

        [MenuItem("Roguelike/Setup Event Config")]
        public static void Setup()
        {
            EnsureFolder("Assets/ScriptableObjects", "Configs", ConfigFolder);
            EnsureFolder("Assets/ScriptableObjects", "Events", EventsFolder);

            // Payloads desde assets existentes. Los placeholders degradan con gracia
            // si falta algún asset (la consecuencia con payload null es no-op).
            CardDefinition giveCard = FindCard("BattleFocus") ?? FindAnyCard();
            CardDefinition removeCard = FindCard("Strike_Neutral") ?? FindCard("Defend_Neutral") ?? FindAnyCard();
            RelicDefinition relic = FindAnyRelic();

            List<EventDefinition> pool = new List<EventDefinition>
            {
                BuildMerchantEvent(giveCard),
                BuildAltarEvent(relic),
                BuildChestEvent(removeCard),
            };

            EventPoolConfig config = AssetDatabase.LoadAssetAtPath<EventPoolConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<EventPoolConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
                Debug.Log($"[EventConfigSetup] Asset creado en {ConfigPath}.");
            }
            config.EditorPopulateEvents(pool);
            EditorUtility.SetDirty(config);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[EventConfigSetup] events={pool.Count} (giveCard={(giveCard != null ? giveCard.name : "null")}, " +
                      $"removeCard={(removeCard != null ? removeCard.name : "null")}, relic={(relic != null ? relic.name : "null")})");
        }

        // ──────────────────────────────────────────────
        // Eventos placeholder
        // ──────────────────────────────────────────────

        private static EventDefinition BuildMerchantEvent(CardDefinition giveCard)
        {
            var choices = new List<EventChoice>
            {
                new EventChoice(
                    "Comprar una carta",
                    "Guardas la carta en tu mazo.",
                    new List<EventConsequence>
                    {
                        new EventConsequence(ConsequenceType.LoseGold, 20),
                        new EventConsequence(ConsequenceType.GiveCard, card: giveCard),
                    },
                    minGoldRequired: 20),
                new EventChoice(
                    "Pedir indicaciones",
                    "El mercader agradece tu compañía y te da 15 de oro.",
                    new List<EventConsequence>
                    {
                        new EventConsequence(ConsequenceType.GiveGold, 15),
                    }),
                new EventChoice(
                    "Seguir tu camino",
                    "Continúas tu viaje.",
                    new List<EventConsequence>()),
            };
            return CreateOrUpdateEvent("evt_merchant", "El mercader errante",
                "Un mercader ambulante extiende su manta de trueques frente a ti.", choices);
        }

        private static EventDefinition BuildAltarEvent(RelicDefinition relic)
        {
            var choices = new List<EventChoice>
            {
                new EventChoice(
                    "Rezar ante el altar",
                    "Una calidez recorre tu cuerpo (+12 HP).",
                    new List<EventConsequence>
                    {
                        new EventConsequence(ConsequenceType.ModifyHP, 12),
                    }),
                new EventChoice(
                    "Profanar el altar",
                    "Arrancas el Retazo incrustado, pero una astilla te hiere (-8 HP).",
                    new List<EventConsequence>
                    {
                        new EventConsequence(ConsequenceType.GiveRelic, relic: relic),
                        new EventConsequence(ConsequenceType.ModifyHP, -8),
                    }),
                new EventChoice(
                    "Ignorarlo",
                    "Te alejas del altar en silencio.",
                    new List<EventConsequence>()),
            };
            return CreateOrUpdateEvent("evt_altar", "Altar misterioso",
                "Un altar de cartón craquelado emite un brillo tenue.", choices);
        }

        private static EventDefinition BuildChestEvent(CardDefinition removeCard)
        {
            var choices = new List<EventChoice>
            {
                new EventChoice(
                    "Abrir el cofre",
                    "El cofre contenía 30 de oro.",
                    new List<EventConsequence>
                    {
                        new EventConsequence(ConsequenceType.GiveGold, 30),
                    }),
                new EventChoice(
                    "Quemar una carta gastada",
                    "Echas una carta vieja a la hoguera del campamento.",
                    new List<EventConsequence>
                    {
                        new EventConsequence(ConsequenceType.RemoveCard, card: removeCard),
                    }),
                new EventChoice(
                    "Dejarlo cerrado",
                    "Dejas el cofre intacto.",
                    new List<EventConsequence>()),
            };
            return CreateOrUpdateEvent("evt_chest", "Cofre del trotamundos",
                "Encuentras un cofre polvoriento junto al sendero.", choices);
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────

        private static EventDefinition CreateOrUpdateEvent(string id, string title, string body, List<EventChoice> choices)
        {
            string path = $"{EventsFolder}/{id}.asset";
            EventDefinition def = AssetDatabase.LoadAssetAtPath<EventDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<EventDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            // Fondo por-evento si existe el PNG (Assets/Art/Events/fondo_<id>.png).
            // Null-safe: sin arte, el panel usa el color de fallback.
            Sprite background = FindBackground(id);
            def.SetDebugData(id, title, body, choices, background);
            EditorUtility.SetDirty(def);
            return def;
        }

        private static Sprite FindBackground(string id)
        {
            string path = $"{EventArtFolder}/fondo_{id}.png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string parent, string newFolder, string fullPath)
        {
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parent, newFolder);
            }
        }

        private static CardDefinition FindCard(string assetName)
        {
            string[] guids = AssetDatabase.FindAssets($"{assetName} t:CardDefinition", new[] { CardsFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == assetName)
                {
                    return AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
                }
            }
            return null;
        }

        private static CardDefinition FindAnyCard()
        {
            string[] guids = AssetDatabase.FindAssets("t:CardDefinition", new[] { CardsFolder });
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<CardDefinition>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static RelicDefinition FindAnyRelic()
        {
            string[] guids = AssetDatabase.FindAssets("t:RelicDefinition", new[] { RelicsFolder });
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<RelicDefinition>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}

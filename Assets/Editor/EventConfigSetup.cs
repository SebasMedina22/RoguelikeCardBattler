using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Gameplay.Relics.Effects;
using RoguelikeCardBattler.Run.Events;

namespace RoguelikeCardBattler.Editor
{
    /// <summary>
    /// Crea (o reutiliza) el EventPoolConfig.asset y todos los EventDefinitions del
    /// acto: 3 simples (4b-1) + 3 multidimensionales (4b-2). Idempotente. Patrón
    /// espejo de <c>ShopConfigSetup</c>.
    ///
    /// Los 2 Retazos MCguffin se crean en <c>Assets/ScriptableObjects/Relics/Quest/</c>
    /// (subcarpeta dedicada, NO entran a ningún drop pool).
    /// </summary>
    public static class EventConfigSetup
    {
        private const string ConfigFolder = "Assets/ScriptableObjects/Configs";
        private const string ConfigPath = ConfigFolder + "/EventPoolConfig.asset";
        private const string EventsFolder = "Assets/ScriptableObjects/Events";
        private const string CardsFolder = "Assets/ScriptableObjects/Cards";
        private const string RelicsFolder = "Assets/ScriptableObjects/Relics";
        private const string QuestRelicsFolder = "Assets/ScriptableObjects/Relics/Quest";
        private const string EventArtFolder = "Assets/Art/Events";

        [MenuItem("Roguelike/Setup Event Config")]
        public static void Setup()
        {
            EnsureFolder("Assets/ScriptableObjects", "Configs", ConfigFolder);
            EnsureFolder("Assets/ScriptableObjects", "Events", EventsFolder);
            EnsureFolder(RelicsFolder, "Quest", QuestRelicsFolder);

            // Payloads para eventos simples
            CardDefinition giveCard = FindCard("BattleFocus") ?? FindAnyCard();
            CardDefinition removeCard = FindCard("Strike_Neutral") ?? FindCard("Defend_Neutral") ?? FindAnyCard();
            RelicDefinition relic = FindAnyRelic();

            // Retazos MCguffin (4b-2) — creados antes que los eventos que los referencian
            RelicDefinition mcguffinA = CreateOrUpdateMcguffinRelic(
                "R-MCG-A_CalizDelMensajero",
                "Cáliz del mensajero",
                "+2 oro al ganar cada combate.",
                "Lacrado con cera de otra época. Pesa como una promesa.",
                RelicHook.OnCombatEnd,
                new RelicEndGoldEffect { Amount = 2 });

            RelicDefinition mcguffinB = CreateOrUpdateMcguffinRelic(
                "R-MCG-B_DiscoDeLaResistencia",
                "Disco de la resistencia",
                "+1 escudo al jugar una carta de escudo.",
                "Late con datos que alguien murió por proteger.",
                RelicHook.OnCardPlayed,
                new RelicCardPlayedBlockEffect { Amount = 1 });

            // Eventos simples (4b-1)
            List<EventDefinition> pool = new List<EventDefinition>
            {
                BuildMerchantEvent(giveCard),
                BuildAltarEvent(relic),
                BuildChestEvent(removeCard),
                // Eventos multidimensionales (4b-2)
                BuildForgeEvent(giveCard),
                BuildRelicEvent(relic),
                BuildQuestEvent(mcguffinA, mcguffinB),
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

            Debug.Log($"[EventConfigSetup] pool={pool.Count} (3 simples + 3 multidim). " +
                      $"MCguffin A={mcguffinA?.name}, B={mcguffinB?.name}.");
        }

        // ──────────────────────────────────────────────
        // Eventos simples (4b-1)
        // ──────────────────────────────────────────────

        private static EventDefinition BuildMerchantEvent(CardDefinition giveCard)
        {
            var choices = new List<EventChoice>
            {
                new EventChoice("Comprar una carta", "Guardas la carta en tu mazo.",
                    new List<EventConsequence>
                    {
                        new EventConsequence(ConsequenceType.LoseGold, 20),
                        new EventConsequence(ConsequenceType.GiveCard, card: giveCard),
                    }, minGoldRequired: 20),
                new EventChoice("Pedir indicaciones", "El mercader agradece tu compañía y te da 15 de oro.",
                    new List<EventConsequence> { new EventConsequence(ConsequenceType.GiveGold, 15) }),
                new EventChoice("Seguir tu camino", "Continúas tu viaje.", new List<EventConsequence>()),
            };
            return CreateOrUpdateEvent("evt_merchant", "El mercader errante",
                "Un mercader ambulante extiende su manta de trueques frente a ti.", choices);
        }

        private static EventDefinition BuildAltarEvent(RelicDefinition relic)
        {
            var choices = new List<EventChoice>
            {
                new EventChoice("Rezar ante el altar", "Una calidez recorre tu cuerpo (+12 HP).",
                    new List<EventConsequence> { new EventConsequence(ConsequenceType.ModifyHP, 12) }),
                new EventChoice("Profanar el altar",
                    "Arrancas el Retazo incrustado, pero una astilla te hiere (-8 HP).",
                    new List<EventConsequence>
                    {
                        new EventConsequence(ConsequenceType.GiveRelic, relic: relic),
                        new EventConsequence(ConsequenceType.ModifyHP, -8),
                    }),
                new EventChoice("Ignorarlo", "Te alejas del altar en silencio.", new List<EventConsequence>()),
            };
            return CreateOrUpdateEvent("evt_altar", "Altar misterioso",
                "Un altar de cartón craquelado emite un brillo tenue.", choices);
        }

        private static EventDefinition BuildChestEvent(CardDefinition removeCard)
        {
            var choices = new List<EventChoice>
            {
                new EventChoice("Abrir el cofre", "El cofre contenía 30 de oro.",
                    new List<EventConsequence> { new EventConsequence(ConsequenceType.GiveGold, 30) }),
                new EventChoice("Quemar una carta gastada",
                    "Echas una carta vieja a la hoguera del campamento.",
                    new List<EventConsequence> { new EventConsequence(ConsequenceType.RemoveCard, card: removeCard) }),
                new EventChoice("Dejarlo cerrado", "Dejas el cofre intacto.", new List<EventConsequence>()),
            };
            return CreateOrUpdateEvent("evt_chest", "Cofre del trotamundos",
                "Encuentras un cofre polvoriento junto al sendero.", choices);
        }

        // ──────────────────────────────────────────────
        // Eventos multidimensionales (4b-2)
        // ──────────────────────────────────────────────

        private static EventDefinition BuildForgeEvent(CardDefinition giveCard)
        {
            var variantA = BuildForgeVariantA(giveCard);
            var variantB = BuildForgeVariantB(giveCard);
            return CreateOrUpdateMultidimEvent(
                "evt_md_forge", "El artesano y su yunque",
                "Un artesano trabaja en silencio.", variantA, variantB);
        }

        private static EventVariant BuildForgeVariantA(CardDefinition giveCard)
        {
            var v = new EventVariant();
            v.SetData(
                "Un herrero ermitaño aviva las brasas de su fragua. " +
                "'Dame algo de oro y tu acero saldrá más afilado', murmura sin mirarte.",
                new List<EventChoice>
                {
                    new EventChoice("Pagar al herrero",
                        "El herrero te entrega una hoja recién templada.",
                        new List<EventConsequence>
                        {
                            new EventConsequence(ConsequenceType.LoseGold, 25),
                            new EventConsequence(ConsequenceType.GiveCard, card: giveCard),
                        }, minGoldRequired: 25),
                    new EventChoice("Curar tus heridas",
                        "El calor de la fragua reconforta tu cuerpo (+10 HP).",
                        new List<EventConsequence> { new EventConsequence(ConsequenceType.ModifyHP, 10) }),
                    new EventChoice("Dejar la fragua",
                        "Te alejas del calor de las brasas.",
                        new List<EventConsequence>()),
                });
            return v;
        }

        private static EventVariant BuildForgeVariantB(CardDefinition giveCard)
        {
            var v = new EventVariant();
            v.SetData(
                "Un técnico de chatarra calibra su soldadora de plasma. " +
                "'Unos créditos y recalibro tu equipo', zumba su vocoder.",
                new List<EventChoice>
                {
                    new EventChoice("Pagar al técnico",
                        "El técnico imprime un módulo y lo acopla a tu equipo.",
                        new List<EventConsequence>
                        {
                            new EventConsequence(ConsequenceType.LoseGold, 25),
                            new EventConsequence(ConsequenceType.GiveCard, card: giveCard),
                        }, minGoldRequired: 25),
                    new EventChoice("Pedir un parche",
                        "El nanogel sella tus heridas (+14 HP).",
                        new List<EventConsequence> { new EventConsequence(ConsequenceType.ModifyHP, 14) }),
                    new EventChoice("Salir del taller",
                        "Las compuertas se cierran tras de ti.",
                        new List<EventConsequence>()),
                });
            return v;
        }

        private static EventDefinition BuildRelicEvent(RelicDefinition relic)
        {
            var variantA = BuildRelicVariantA(relic);
            var variantB = BuildRelicVariantB(relic);
            return CreateOrUpdateMultidimEvent(
                "evt_md_relic", "El relicario sellado",
                "Algo antiguo espera ser abierto.", variantA, variantB);
        }

        private static EventVariant BuildRelicVariantA(RelicDefinition relic)
        {
            var v = new EventVariant();
            v.SetData(
                "Un relicario de hierro descansa sobre un altar agrietado. " +
                "Un susurro promete poder a quien se atreva a abrirlo.",
                new List<EventChoice>
                {
                    new EventChoice("Romper el sello",
                        "Arrancas la reliquia; las púas del relicario te hieren (-8 HP).",
                        new List<EventConsequence>
                        {
                            new EventConsequence(ConsequenceType.GiveRelic, relic: relic),
                            new EventConsequence(ConsequenceType.ModifyHP, -8),
                        }),
                    new EventChoice("Llevarte las ofrendas",
                        "Recoges 30 de oro de entre las ofrendas.",
                        new List<EventConsequence> { new EventConsequence(ConsequenceType.GiveGold, 30) }),
                    new EventChoice("No arriesgarte",
                        "Retrocedes sin tocar el altar.",
                        new List<EventConsequence>()),
                });
            return v;
        }

        private static EventVariant BuildRelicVariantB(RelicDefinition relic)
        {
            var v = new EventVariant();
            v.SetData(
                "Una cápsula de contención parpadea en rojo. " +
                "Una voz sintética ofrece su carga a quien acepte el riesgo.",
                new List<EventChoice>
                {
                    new EventChoice("Forzar la cápsula",
                        "Extraes el núcleo; una descarga te recorre (-6 HP).",
                        new List<EventConsequence>
                        {
                            new EventConsequence(ConsequenceType.GiveRelic, relic: relic),
                            new EventConsequence(ConsequenceType.ModifyHP, -6),
                        }),
                    new EventChoice("Vaciar la reserva",
                        "Transfieres 35 créditos de la reserva.",
                        new List<EventConsequence> { new EventConsequence(ConsequenceType.GiveGold, 35) }),
                    new EventChoice("No arriesgarte",
                        "Te apartas de la cápsula parpadeante.",
                        new List<EventConsequence>()),
                });
            return v;
        }

        private static EventDefinition BuildQuestEvent(RelicDefinition mcguffinA, RelicDefinition mcguffinB)
        {
            var variantA = BuildQuestVariantA(mcguffinA);
            var variantB = BuildQuestVariantB(mcguffinB);
            return CreateOrUpdateMultidimEvent(
                "evt_md_quest_courier", "Un encargo en el camino",
                "Alguien necesita tu ayuda para llevar algo al otro lado del mapa.", variantA, variantB);
        }

        private static EventVariant BuildQuestVariantA(RelicDefinition mcguffinA)
        {
            var questData = new QuestData { CarriedRelic = mcguffinA, FinalRewardGold = 75 };
            var v = new EventVariant();
            v.SetData(
                "Un hombre moribundo, con las ropas raídas de la legión de magos, te aferra el brazo. " +
                "Te ruega llevar un objeto místico hasta un punto lejano del mapa; si lo logras, promete recompensarte.",
                new List<EventChoice>
                {
                    new EventChoice("Aceptar el encargo",
                        "El mago te confía un cáliz lacrado. Un punto del mapa queda resaltado.",
                        new List<EventConsequence> { new EventConsequence(ConsequenceType.StartQuest, quest: questData) }),
                    new EventChoice("Robar al moribundo",
                        "Le arrebatas la bolsa y huyes. El objeto se hace añicos. +100 de oro.",
                        new List<EventConsequence> { new EventConsequence(ConsequenceType.GiveGold, 100) }),
                });
            return v;
        }

        private static EventVariant BuildQuestVariantB(RelicDefinition mcguffinB)
        {
            var questData = new QuestData { CarriedRelic = mcguffinB, FinalRewardGold = 75 };
            var v = new EventVariant();
            v.SetData(
                "Un robot con las piezas rotas chisporrotea en el suelo. Dice pertenecer a una facción " +
                "revolucionaria y te pide llevar un disco duro con información vital hasta un punto lejano del mapa.",
                new List<EventChoice>
                {
                    new EventChoice("Aceptar el encargo",
                        "El robot te transfiere un disco sellado. Un punto del mapa queda resaltado.",
                        new List<EventConsequence> { new EventConsequence(ConsequenceType.StartQuest, quest: questData) }),
                    new EventChoice("Robar al robot",
                        "Le arrancas el disco y sus créditos. El disco se desintegra. +100 de oro.",
                        new List<EventConsequence> { new EventConsequence(ConsequenceType.GiveGold, 100) }),
                });
            return v;
        }

        // ──────────────────────────────────────────────
        // MCguffin relics
        // ──────────────────────────────────────────────

        private static RelicDefinition CreateOrUpdateMcguffinRelic(
            string assetName, string displayName, string description,
            string flavorText, RelicHook hook, IRelicEffect effect)
        {
            string path = $"{QuestRelicsFolder}/{assetName}.asset";
            RelicDefinition def = AssetDatabase.LoadAssetAtPath<RelicDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<RelicDefinition>();
                AssetDatabase.CreateAsset(def, path);
                Debug.Log($"[EventConfigSetup] MCguffin relic creado: {path}");
            }
            def.DisplayName = displayName;
            def.Description = description;
            def.FlavorText = flavorText;
            def.Category = RelicCategory.World;
            def.Hooks = new[] { hook };
            def.Effect = effect;
            EditorUtility.SetDirty(def);
            return def;
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────

        private static EventDefinition CreateOrUpdateEvent(
            string id, string title, string body, List<EventChoice> choices)
        {
            string path = $"{EventsFolder}/{id}.asset";
            EventDefinition def = AssetDatabase.LoadAssetAtPath<EventDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<EventDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            Sprite background = FindBackground(id);
            def.SetDebugData(id, title, body, choices, background);
            EditorUtility.SetDirty(def);
            return def;
        }

        private static EventDefinition CreateOrUpdateMultidimEvent(
            string id, string title, string bodyFallback,
            EventVariant variantA, EventVariant variantB)
        {
            string path = $"{EventsFolder}/{id}.asset";
            EventDefinition def = AssetDatabase.LoadAssetAtPath<EventDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<EventDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            Sprite background = FindBackground(id);
            def.SetDebugData(id, title, bodyFallback, null, background,
                multidim: true, newWorldA: variantA, newWorldB: variantB);
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
                AssetDatabase.CreateFolder(parent, newFolder);
        }

        private static CardDefinition FindCard(string assetName)
        {
            string[] guids = AssetDatabase.FindAssets($"{assetName} t:CardDefinition", new[] { CardsFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == assetName)
                    return AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
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
            // Solo en la carpeta principal de Relics (excluye Quest/) para no capturar MCguffins.
            string[] guids = AssetDatabase.FindAssets("t:RelicDefinition", new[] { RelicsFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("/Quest/"))
                    return AssetDatabase.LoadAssetAtPath<RelicDefinition>(path);
            }
            return null;
        }
    }
}

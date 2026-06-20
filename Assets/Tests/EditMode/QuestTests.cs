using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RoguelikeCardBattler.Run;
using RoguelikeCardBattler.Run.Events;
using RoguelikeCardBattler.Run.Quests;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Gameplay.Relics.Effects;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// Casos 8-16 del spec M4 4b-2 (QuestTests).
    /// </summary>
    public class QuestTests : CombatTestBase
    {
        // ──────────────────────────────────────────────
        // Helpers de construcción de mapas (topologías del proyecto)
        // ──────────────────────────────────────────────

        // Topo A: 0→(1,2)→3→(4,5)→6→7
        private static ActMap BuildTopoA()
        {
            var map = new ActMap(0);
            for (int i = 0; i < 8; i++)
                map.Nodes.Add(new MapNode(i, i == 7 ? NodeType.Boss : NodeType.Event));
            map.GetNode(0).Connections.AddRange(new[] { 1, 2 });
            map.GetNode(1).Connections.Add(3);
            map.GetNode(2).Connections.Add(3);
            map.GetNode(3).Connections.AddRange(new[] { 4, 5 });
            map.GetNode(4).Connections.Add(6);
            map.GetNode(5).Connections.Add(6);
            map.GetNode(6).Connections.Add(7);
            return map;
        }

        // Topo B: 0→1→(2,3)→4→(5,6)→7
        private static ActMap BuildTopoB()
        {
            var map = new ActMap(0);
            for (int i = 0; i < 8; i++)
                map.Nodes.Add(new MapNode(i, i == 7 ? NodeType.Boss : NodeType.Event));
            map.GetNode(0).Connections.Add(1);
            map.GetNode(1).Connections.AddRange(new[] { 2, 3 });
            map.GetNode(2).Connections.Add(4);
            map.GetNode(3).Connections.Add(4);
            map.GetNode(4).Connections.AddRange(new[] { 5, 6 });
            map.GetNode(5).Connections.Add(7);
            map.GetNode(6).Connections.Add(7);
            return map;
        }

        // Topo C: 0→(1,2,3), 1→4, 2→4, 3→5, 4→6, 5→6, 6→7
        private static ActMap BuildTopoC()
        {
            var map = new ActMap(0);
            for (int i = 0; i < 8; i++)
                map.Nodes.Add(new MapNode(i, i == 7 ? NodeType.Boss : NodeType.Event));
            map.GetNode(0).Connections.AddRange(new[] { 1, 2, 3 });
            map.GetNode(1).Connections.Add(4);
            map.GetNode(2).Connections.Add(4);
            map.GetNode(3).Connections.Add(5);
            map.GetNode(4).Connections.Add(6);
            map.GetNode(5).Connections.Add(6);
            map.GetNode(6).Connections.Add(7);
            return map;
        }

        private static bool IsForwardReachable(ActMap map, int from, int target)
        {
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                if (cur == target) return true;
                if (!visited.Add(cur)) continue;
                var node = map.GetNode(cur);
                if (node == null) continue;
                foreach (int conn in node.Connections) queue.Enqueue(conn);
            }
            return false;
        }

        private TurnManager CreateManager()
        {
            var enemy = CreateEnemyDefinition("dummy", "Dummy", 30,
                EnemyAIPattern.Sequence, new List<EnemyMove>(), ElementType.None);
            var card = CreateCard("card0", CardType.Attack, CardTarget.SingleEnemy, 1,
                new EffectRef { effectType = EffectType.Damage, value = 5, target = EffectTarget.SingleEnemy });
            var deck = new List<CardDeckEntry> { CreateSingleCardEntry(card) };

            GameObject go = CreateGameObject("TurnManager");
            TurnManager manager = AddComponent<TurnManager>(go);
            manager.SetTestConfig(30, 3, 1, 1);
            manager.ConfigureCombat(deck, enemy,
                playerCurrentHpOverride: 30,
                playerMaxHpOverride: 30,
                initializeImmediately: true);
            return manager;
        }

        // ──────────────────────────────────────────────
        // Caso 8: ResolveVariant devuelve choices distintas para A vs B
        // ──────────────────────────────────────────────

        [Test]
        public void Test08_ResolveVariant_ReturnsDifferentChoicesForAAndB()
        {
            var choiceA = new EventChoice("ChoiceA", "ResultA", new List<EventConsequence>());
            var choiceB = new EventChoice("ChoiceB", "ResultB", new List<EventConsequence>());

            var varA = new EventVariant();
            varA.SetData("Texto mundo A", new List<EventChoice> { choiceA });
            var varB = new EventVariant();
            varB.SetData("Texto mundo B", new List<EventChoice> { choiceB });

            EventDefinition def = ScriptableObject.CreateInstance<EventDefinition>();
            def.SetDebugData("test_md", "Test", "body", null,
                multidim: true, newWorldA: varA, newWorldB: varB);

            var choicesA = EventResolver.ResolveVariant(def, 0);
            var choicesB = EventResolver.ResolveVariant(def, 1);

            Assert.IsNotNull(choicesA, "Choices A no deben ser null");
            Assert.IsNotNull(choicesB, "Choices B no deben ser null");
            Assert.AreEqual("ChoiceA", choicesA[0].Label, "Mundo A debe tener ChoiceA");
            Assert.AreEqual("ChoiceB", choicesB[0].Label, "Mundo B debe tener ChoiceB");
            Assert.AreNotEqual(choicesA[0].Label, choicesB[0].Label, "A y B deben ser distintos");

            Object.DestroyImmediate(def);
        }

        // ──────────────────────────────────────────────
        // Caso 9: SelectDestination — alcanzable, != questNodeId, ∉ completedNodes
        //         para todas las topologías × nodos intermedios 1..6
        // ──────────────────────────────────────────────

        [Test]
        public void Test09_SelectDestination_AlwaysReachableAndValid_AllTopologies()
        {
            var maps = new[] { BuildTopoA(), BuildTopoB(), BuildTopoC() };
            string[] topoNames = { "TopoA", "TopoB", "TopoC" };

            for (int m = 0; m < maps.Length; m++)
            {
                ActMap map = maps[m];
                for (int questNode = 1; questNode <= 6; questNode++)
                {
                    var completed = new HashSet<int>();
                    int dest = QuestDestinationResolver.SelectDestination(map, questNode, completed);

                    Assert.IsTrue(IsForwardReachable(map, questNode, dest),
                        $"{topoNames[m]}: destino {dest} no alcanzable desde quest={questNode}");
                    Assert.AreNotEqual(questNode, dest,
                        $"{topoNames[m]}: destino == questNodeId={questNode}");
                    Assert.IsFalse(completed.Contains(dest),
                        $"{topoNames[m]}: destino {dest} está en completedNodes");
                }
            }
        }

        // ──────────────────────────────────────────────
        // Caso 10: CompleteQuestIfDestination(destino) → otorga gold, Active=false, true
        // ──────────────────────────────────────────────

        [Test]
        public void Test10_CompleteQuestIfDestination_Awards_Gold_And_Deactivates()
        {
            RunState state = new RunState();
            state.StartQuest(new QuestState
            {
                Active = true,
                DestinationNodeId = 5,
                FinalRewardGold = 75
            });
            int goldBefore = state.Gold;

            bool result = state.CompleteQuestIfDestination(5);

            Assert.IsTrue(result, "Debe devolver true al completar el quest");
            Assert.AreEqual(goldBefore + 75, state.Gold, "Debe otorgar FinalRewardGold");
            Assert.IsFalse(state.ActiveQuest.Active, "Quest debe quedar inactivo");
        }

        // ──────────────────────────────────────────────
        // Caso 11: CompleteQuestIfDestination(otroNodo) → false, sin gold, quest activo
        // ──────────────────────────────────────────────

        [Test]
        public void Test11_CompleteQuestIfDestination_WrongNode_DoesNothing()
        {
            RunState state = new RunState();
            state.StartQuest(new QuestState
            {
                Active = true,
                DestinationNodeId = 5,
                FinalRewardGold = 75
            });
            int goldBefore = state.Gold;

            bool result = state.CompleteQuestIfDestination(3);

            Assert.IsFalse(result, "Debe devolver false para nodo incorrecto");
            Assert.AreEqual(goldBefore, state.Gold, "No debe dar oro");
            Assert.IsTrue(state.ActiveQuest.Active, "Quest debe seguir activo");
        }

        // ──────────────────────────────────────────────
        // Caso 12: Quest "Robar" → +100 oro, sin quest activo, Retazo NO entregado
        // ──────────────────────────────────────────────

        [Test]
        public void Test12_RobChoice_GivesGold_NoQuest_NoRelic()
        {
            RunState state = new RunState();
            state.Gold = 0;

            // La choice "Robar" solo tiene GiveGold — sin StartQuest ni GiveRelic
            var robConsequence = new EventConsequence(ConsequenceType.GiveGold, 100);
            EventConsequence.Apply(state, null, robConsequence);

            Assert.AreEqual(100, state.Gold, "Debe dar +100 oro");
            Assert.IsFalse(state.ActiveQuest.Active, "No debe activar quest");
            Assert.AreEqual(0, state.Relics.Count, "No debe dar Retazo");
        }

        // ──────────────────────────────────────────────
        // Caso 13: RunState.Reset() limpia ActiveQuest
        // ──────────────────────────────────────────────

        [Test]
        public void Test13_Reset_ClearsActiveQuest()
        {
            RunState state = new RunState();
            state.StartQuest(new QuestState
            {
                Active = true,
                DestinationNodeId = 4,
                FinalRewardGold = 75
            });
            Assert.IsTrue(state.ActiveQuest.Active, "Setup: quest debe estar activo");

            state.Reset(null);

            Assert.IsFalse(state.ActiveQuest.Active, "Reset debe desactivar el quest");
            Assert.AreEqual(-1, state.ActiveQuest.DestinationNodeId, "DestinationNodeId debe ser -1");
        }

        // ──────────────────────────────────────────────
        // Caso 14: RelicCardPlayedBlockEffect — carta Block da bloqueo; no-Block no-op
        // ──────────────────────────────────────────────

        [Test]
        public void Test14_RelicCardPlayedBlockEffect_BlockCard_GrantsBlock_NonBlock_NoOp()
        {
            TurnManager manager = CreateManager();

            RelicDefinition relicDef = ScriptableObject.CreateInstance<RelicDefinition>();
            relicDef.DisplayName = "Test MCguffin B";
            relicDef.Hooks = new[] { RelicHook.OnCardPlayed };
            relicDef.Effect = new RelicCardPlayedBlockEffect { Amount = 1 };

            RunState rs = new RunState();
            rs.Relics.Add(new RelicInstance(relicDef, 0));
            RelicHookDispatcher dispatcher = new RelicHookDispatcher(rs);

            // Carta con EffectType.Block
            CardDefinition blockCard = ScriptableObject.CreateInstance<CardDefinition>();
            blockCard.SetDebugData("test_block", "Block", "desc", 1,
                CardType.Defense, CardRarity.Common, CardTarget.Self, null,
                new List<EffectRef> { new EffectRef { effectType = EffectType.Block, value = 5 } });

            // Carta sin EffectType.Block
            CardDefinition attackCard = ScriptableObject.CreateInstance<CardDefinition>();
            attackCard.SetDebugData("test_attack", "Attack", "desc", 1,
                CardType.Attack, CardRarity.Common, CardTarget.SingleEnemy, null,
                new List<EffectRef> { new EffectRef { effectType = EffectType.Damage, value = 5 } });

            int blockBefore = manager.Player.CurrentBlock;

            // Disparar OnCardPlayed con carta Block
            var hookBlock = new CardPlayedHookData(rs, manager, dispatcher,
                blockCard, TurnManager.WorldSide.A, 1);
            dispatcher.Dispatch(RelicHook.OnCardPlayed, hookBlock);

            Assert.AreEqual(blockBefore + 1, manager.Player.CurrentBlock,
                "Carta Block debe dar +1 bloqueo");

            int blockAfterBlock = manager.Player.CurrentBlock;

            // Disparar OnCardPlayed con carta no-Block: sin cambio
            var hookAttack = new CardPlayedHookData(rs, manager, dispatcher,
                attackCard, TurnManager.WorldSide.A, 1);
            dispatcher.Dispatch(RelicHook.OnCardPlayed, hookAttack);

            Assert.AreEqual(blockAfterBlock, manager.Player.CurrentBlock,
                "Carta no-Block NO debe cambiar el bloqueo");

            Object.DestroyImmediate(relicDef);
            Object.DestroyImmediate(blockCard);
            Object.DestroyImmediate(attackCard);
        }

        // ──────────────────────────────────────────────
        // Caso 15: Fallback al Boss cuando el event está en penúltima capa
        // ──────────────────────────────────────────────

        [Test]
        public void Test15_SelectDestination_Fallback_ToBoss_WhenPenultimateLayer()
        {
            // Topo A: nodo 6 tiene solo nodo 7 (Boss) como forward
            ActMap mapA = BuildTopoA();
            int destA = QuestDestinationResolver.SelectDestination(mapA, 6, new HashSet<int>());
            Assert.AreEqual(7, destA, "TopoA nodo 6: destino debe ser Boss (7)");
            Assert.AreEqual(NodeType.Boss, mapA.GetNode(destA).Type, "Destino debe ser Boss");

            // Topo B: nodos 5 y 6 tienen solo el Boss como forward
            ActMap mapB = BuildTopoB();
            Assert.AreEqual(7, QuestDestinationResolver.SelectDestination(mapB, 5, new HashSet<int>()),
                "TopoB nodo 5: destino debe ser Boss");
            Assert.AreEqual(7, QuestDestinationResolver.SelectDestination(mapB, 6, new HashSet<int>()),
                "TopoB nodo 6: destino debe ser Boss");

            // Topo C: nodo 6 tiene solo el Boss como forward
            ActMap mapC = BuildTopoC();
            Assert.AreEqual(7, QuestDestinationResolver.SelectDestination(mapC, 6, new HashSet<int>()),
                "TopoC nodo 6: destino debe ser Boss");
        }

        // ──────────────────────────────────────────────
        // Caso 16: Determinismo — misma (map, questNodeId) → mismo destino en llamadas repetidas
        // ──────────────────────────────────────────────

        [Test]
        public void Test16_SelectDestination_Deterministic_SameInputSameResult()
        {
            ActMap map = BuildTopoA();
            var completed = new HashSet<int>();

            int dest1 = QuestDestinationResolver.SelectDestination(map, 1, completed);
            int dest2 = QuestDestinationResolver.SelectDestination(map, 1, completed);
            int dest3 = QuestDestinationResolver.SelectDestination(map, 1, completed);

            Assert.AreEqual(dest1, dest2, "Llamada 1 vs 2: mismo destino");
            Assert.AreEqual(dest1, dest3, "Llamada 1 vs 3: mismo destino");
        }
    }
}

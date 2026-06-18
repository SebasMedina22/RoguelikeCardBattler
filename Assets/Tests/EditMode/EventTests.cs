using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Run;
using RoguelikeCardBattler.Run.Events;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// Tests EditMode del motor de eventos (Sub-PR 4b-1). Validan los helpers
    /// static puros (EventConsequence.Apply, EventResolver.SelectEvent,
    /// EventNodeController.IsChoiceAvailable) y RunMapGenerator.AssignEvents.
    /// Patrón: RunState directo sin TurnManager (los eventos corren fuera de combate).
    /// </summary>
    public class EventTests
    {
        // ────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────

        private static CardDefinition MakeCard(string id, string name, ElementType element = ElementType.None)
        {
            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.SetDebugData(
                id, name, "", 1,
                CardType.Attack, CardRarity.Common, CardTarget.SingleEnemy,
                new List<string>(), new List<EffectRef>(), element);
            return card;
        }

        private static RelicDefinition MakeRelic(string name)
        {
            RelicDefinition def = ScriptableObject.CreateInstance<RelicDefinition>();
            def.DisplayName = name;
            def.Hooks = new RelicHook[0];
            return def;
        }

        private static EventDefinition MakeEvent(string id)
        {
            EventDefinition def = ScriptableObject.CreateInstance<EventDefinition>();
            def.SetDebugData(id, id, "body", new List<EventChoice>());
            return def;
        }

        private static EventPoolConfig MakePool(params EventDefinition[] events)
        {
            EventPoolConfig pool = ScriptableObject.CreateInstance<EventPoolConfig>();
            pool.SetDebugData(new List<EventDefinition>(events));
            return pool;
        }

        // ────────────────────────────────────────
        // Caso 1 — oro
        // ────────────────────────────────────────

        [Test]
        public void Apply_GiveGold_AddsAndLoseGold_ClampsAtZero()
        {
            RunState rs = new RunState { Gold = 10 };

            EventConsequence.Apply(rs, null, new EventConsequence(ConsequenceType.GiveGold, 25));
            Assert.AreEqual(35, rs.Gold, "GiveGold debe sumar oro.");

            EventConsequence.Apply(rs, null, new EventConsequence(ConsequenceType.LoseGold, 100));
            Assert.AreEqual(0, rs.Gold, "LoseGold no debe dejar el oro por debajo de 0.");
        }

        // ────────────────────────────────────────
        // Caso 2 — HP
        // ────────────────────────────────────────

        [Test]
        public void Apply_ModifyHP_RespectsMaxCapAndZeroFloor()
        {
            RunState rs = new RunState { PlayerMaxHP = 60, PlayerCurrentHP = 55 };

            EventConsequence.Apply(rs, null, new EventConsequence(ConsequenceType.ModifyHP, 20));
            Assert.AreEqual(60, rs.PlayerCurrentHP, "El HP positivo no debe superar PlayerMaxHP.");

            EventConsequence.Apply(rs, null, new EventConsequence(ConsequenceType.ModifyHP, -200));
            Assert.AreEqual(0, rs.PlayerCurrentHP, "El HP negativo no debe bajar de 0.");
        }

        // ────────────────────────────────────────
        // Caso 3 — cartas
        // ────────────────────────────────────────

        [Test]
        public void Apply_GiveCard_AddsCloneToDeck()
        {
            RunState rs = new RunState();
            CardDefinition card = MakeCard("c1", "Carta", ElementType.None);

            EventConsequence.Apply(rs, null, new EventConsequence(ConsequenceType.GiveCard, card: card));

            Assert.AreEqual(1, rs.Deck.Count, "GiveCard debe agregar una carta al mazo.");
        }

        [Test]
        public void Apply_RemoveCard_RemovesByReference()
        {
            RunState rs = new RunState();
            CardDefinition card = MakeCard("c1", "Carta", ElementType.None);
            rs.Deck.Add(CardDeckEntry.CreateSingle(card));
            // Carta presente pero distinta: no debe eliminarse.
            CardDefinition other = MakeCard("c2", "Otra", ElementType.None);
            rs.Deck.Add(CardDeckEntry.CreateSingle(other));

            EventConsequence.Apply(rs, null, new EventConsequence(ConsequenceType.RemoveCard, card: card));

            Assert.AreEqual(1, rs.Deck.Count, "RemoveCard debe quitar exactamente la entrada que referencia esa carta.");
            Assert.AreSame(other, rs.Deck[0].SingleCard, "La carta no objetivo debe permanecer.");
        }

        // ────────────────────────────────────────
        // Caso 4 — Retazos con AcquisitionOrder
        // ────────────────────────────────────────

        [Test]
        public void Apply_GiveRelic_AddsWithAcquisitionOrder()
        {
            RunState rs = new RunState();
            RelicDefinition first = MakeRelic("Primero");
            RelicDefinition second = MakeRelic("Segundo");

            EventConsequence.Apply(rs, null, new EventConsequence(ConsequenceType.GiveRelic, relic: first));
            EventConsequence.Apply(rs, null, new EventConsequence(ConsequenceType.GiveRelic, relic: second));

            Assert.AreEqual(2, rs.Relics.Count);
            Assert.AreSame(first, rs.Relics[0].Definition);
            Assert.AreEqual(0, rs.Relics[0].AcquisitionOrder);
            Assert.AreSame(second, rs.Relics[1].Definition);
            Assert.AreEqual(1, rs.Relics[1].AcquisitionOrder);
        }

        // ────────────────────────────────────────
        // Caso 5 — SelectEvent determinista por seed
        // ────────────────────────────────────────

        [Test]
        public void SelectEvent_DeterministicBySeedAndNode()
        {
            EventPoolConfig pool = MakePool(
                MakeEvent("a"), MakeEvent("b"), MakeEvent("c"),
                MakeEvent("d"), MakeEvent("e"), MakeEvent("f"));

            for (int nodeId = 0; nodeId <= 6; nodeId++)
            {
                EventDefinition first = EventResolver.SelectEvent(pool, nodeId, 42);
                EventDefinition second = EventResolver.SelectEvent(pool, nodeId, 42);
                Assert.AreSame(first, second, $"Misma seed/nodo ({nodeId}) debe dar el mismo evento.");
            }
        }

        [Test]
        public void SelectEvent_DivergesBySeed()
        {
            EventPoolConfig pool = MakePool(
                MakeEvent("a"), MakeEvent("b"), MakeEvent("c"),
                MakeEvent("d"), MakeEvent("e"), MakeEvent("f"));

            bool anyDifferent = false;
            for (int nodeId = 0; nodeId <= 6; nodeId++)
            {
                EventDefinition s1 = EventResolver.SelectEvent(pool, nodeId, 100);
                EventDefinition s2 = EventResolver.SelectEvent(pool, nodeId, 200);
                if (!ReferenceEquals(s1, s2)) anyDifferent = true;
            }
            Assert.IsTrue(anyDifferent, "Dos seeds distintas deben divergir en al menos un nodo.");
        }

        [Test]
        public void SelectEvent_NullOrEmptyPool_ReturnsNull()
        {
            Assert.IsNull(EventResolver.SelectEvent(null, 0, 1));
            Assert.IsNull(EventResolver.SelectEvent(MakePool(), 0, 1));
        }

        // ────────────────────────────────────────
        // Caso 6 — MinGoldRequired
        // ────────────────────────────────────────

        [Test]
        public void IsChoiceAvailable_GatesByGold()
        {
            EventChoice gated = new EventChoice("Comprar", "ok", new List<EventConsequence>(), minGoldRequired: 50);
            EventChoice free = new EventChoice("Mirar", "ok", new List<EventConsequence>());

            Assert.IsFalse(EventNodeController.IsChoiceAvailable(new RunState { Gold = 30 }, gated),
                "Con oro insuficiente la decisión no debe estar disponible.");
            Assert.IsTrue(EventNodeController.IsChoiceAvailable(new RunState { Gold = 50 }, gated),
                "Con oro suficiente (=requisito) la decisión debe estar disponible.");
            Assert.IsTrue(EventNodeController.IsChoiceAvailable(new RunState { Gold = 0 }, free),
                "Una decisión sin requisito siempre está disponible.");
        }

        // ────────────────────────────────────────
        // Caso 7 — AssignEvents
        // ────────────────────────────────────────

        [Test]
        public void AssignEvents_OnlyEventNodes_Deterministic()
        {
            EventPoolConfig pool = MakePool(
                MakeEvent("a"), MakeEvent("b"), MakeEvent("c"), MakeEvent("d"));

            ActMap map = new ActMap(0);
            map.Nodes.Add(new MapNode(0, NodeType.Combat));
            map.Nodes.Add(new MapNode(1, NodeType.Event));
            map.Nodes.Add(new MapNode(2, NodeType.Shop));
            map.Nodes.Add(new MapNode(3, NodeType.Event));
            map.Nodes.Add(new MapNode(4, NodeType.Boss));

            RunMapGenerator.AssignEvents(map, pool, seed: 7);

            Assert.IsNull(map.GetNode(0).AssignedEvent, "Un nodo Combat no debe recibir evento.");
            Assert.IsNull(map.GetNode(2).AssignedEvent, "Un nodo Shop no debe recibir evento.");
            Assert.IsNull(map.GetNode(4).AssignedEvent, "Un nodo Boss no debe recibir evento.");
            Assert.IsNotNull(map.GetNode(1).AssignedEvent, "El nodo Event 1 debe recibir un evento.");
            Assert.IsNotNull(map.GetNode(3).AssignedEvent, "El nodo Event 3 debe recibir un evento.");

            // Determinismo: re-generar con la misma seed asigna los mismos eventos.
            EventDefinition before1 = map.GetNode(1).AssignedEvent;
            EventDefinition before3 = map.GetNode(3).AssignedEvent;
            RunMapGenerator.AssignEvents(map, pool, seed: 7);
            Assert.AreSame(before1, map.GetNode(1).AssignedEvent);
            Assert.AreSame(before3, map.GetNode(3).AssignedEvent);
        }
    }
}

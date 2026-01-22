using System.Collections.Generic;
using RoguelikeCardBattler.Gameplay.Cards;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Estado en memoria de la run. No persiste a disco.
    /// </summary>
    public class RunState
    {
        public enum NodeOutcome
        {
            None,
            Victory,
            Defeat
        }

        public int CurrentPositionNodeId { get; set; } = -1;
        public int CurrentNodeId { get; set; } = -1;
        public int Gold { get; set; } = 0;
        public HashSet<int> CompletedNodes { get; } = new HashSet<int>();
        public HashSet<int> AvailableNodes { get; } = new HashSet<int>();
        public List<CardDeckEntry> Deck { get; } = new List<CardDeckEntry>();
        public int PlayerMaxHP { get; set; }
        public int PlayerCurrentHP { get; set; }
        public bool HasPlayerHPInitialized { get; set; }
        public bool PendingReturnFromBattle { get; set; }
        public NodeOutcome LastNodeOutcome { get; set; } = NodeOutcome.None;
        public bool RunFailed { get; set; }
        public bool ActoCompleted { get; set; } = false;

        public void EnsureInitialized(ActMap map)
        {
            if (map == null)
            {
                return;
            }

            if (AvailableNodes.Count > 0 || CompletedNodes.Count > 0)
            {
                return;
            }

            AvailableNodes.Clear();
            CompletedNodes.Clear();
            AvailableNodes.Add(map.StartNodeId);
            CurrentPositionNodeId = map.StartNodeId;
        }

        public void Reset(ActMap map)
        {
            CompletedNodes.Clear();
            AvailableNodes.Clear();
            CurrentNodeId = -1;
            CurrentPositionNodeId = -1;
            Gold = 0;
            Deck.Clear();
            PlayerMaxHP = 0;
            PlayerCurrentHP = 0;
            HasPlayerHPInitialized = false;
            PendingReturnFromBattle = false;
            LastNodeOutcome = NodeOutcome.None;
            RunFailed = false;
            ActoCompleted = false;
            EnsureInitialized(map);
        }

        public bool IsNodeAvailable(int nodeId) => AvailableNodes.Contains(nodeId);
        public bool IsNodeCompleted(int nodeId) => CompletedNodes.Contains(nodeId);

        public void InitializeDeck(IReadOnlyList<CardDeckEntry> starterDeck)
        {
            if (starterDeck == null || starterDeck.Count == 0 || Deck.Count > 0)
            {
                return;
            }

            foreach (CardDeckEntry entry in starterDeck)
            {
                if (entry == null || !entry.IsValid)
                {
                    continue;
                }

                Deck.Add(entry.Clone());
            }
        }

        /// <summary>
        /// Inicializa los valores de HP del jugador una sola vez por run.
        /// Esto permite persistir HP entre combates sin depender de escenas.
        /// </summary>
        public void EnsurePlayerHpInitialized(int defaultMaxHp)
        {
            if (HasPlayerHPInitialized || defaultMaxHp <= 0)
            {
                return;
            }

            PlayerMaxHP = defaultMaxHp;
            PlayerCurrentHP = defaultMaxHp;
            HasPlayerHPInitialized = true;
        }

        public void AddCardToDeck(CardDeckEntry entry)
        {
            if (entry == null || !entry.IsValid)
            {
                return;
            }

            Deck.Add(entry.Clone());
        }

        public void AddCardToDeck(CardDefinition card)
        {
            if (card == null)
            {
                return;
            }

            AddCardToDeck(CardDeckEntry.CreateSingle(card));
        }

        public List<CardDeckEntry> GetDeckSnapshot()
        {
            return new List<CardDeckEntry>(Deck);
        }
    }
}

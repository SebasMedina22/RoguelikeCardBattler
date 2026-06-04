using System.Collections.Generic;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Relics;

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

        // Cuántas tiendas completó el jugador en esta run. Lo incrementa
        // RunFlowController al salir de una Tienda; la Tienda lo lee para escalar
        // el precio del servicio "eliminar carta" (Sub-PR 3D). Reset en Reset().
        public int ShopsCompleted { get; set; } = 0;

        // Los 2 tipos elegidos al inicio del run (uno por mundo). Sub-PR A los
        // declara con defaults Rojo/Amarillo; futuras pantallas de selección
        // (M3) los sobreescribirán. TurnManager los lee en ConfigureCombat
        // para derivar PlayerActiveType según el mundo activo.
        public ElementType PlayerWorldAType { get; set; } = ElementType.Rojo;
        public ElementType PlayerWorldBType { get; set; } = ElementType.Amarillo;

        // Inventario de Retazos del run. RelicHookDispatcher (en RunSession) los lee
        // por hook al disparar eventos. El orden de la lista define AcquisitionOrder
        // de cada instance (asc = adquirido primero = corre primero en cadena).
        public List<RelicInstance> Relics { get; } = new List<RelicInstance>();

        /// <summary>
        /// Helper para sumar un Retazo al run respetando la convención de
        /// AcquisitionOrder (orden de la lista). Lo usan drops post-victoria
        /// (BattleFlowController) y la Tienda (3D).
        /// </summary>
        public void AddRelic(RelicDefinition definition)
        {
            if (definition == null) return;
            Relics.Add(new RelicInstance(definition, Relics.Count));
        }

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
            ShopsCompleted = 0;
            PlayerWorldAType = ElementType.Rojo;
            PlayerWorldBType = ElementType.Amarillo;
            Relics.Clear();
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

        /// <summary>
        /// Quita una entrada concreta del mazo (por referencia). Devuelve true si
        /// estaba y se eliminó, false si no estaba. Lo usa el servicio "eliminar
        /// carta" de la Tienda (Sub-PR 3D).
        /// </summary>
        public bool RemoveCardFromDeck(CardDeckEntry entry)
        {
            if (entry == null) return false;
            return Deck.Remove(entry);
        }
    }
}

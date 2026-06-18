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
        // declara con defaults Rojo/Amarillo; NewRunScene (Sub-PR 3E) los
        // sobreescribe vía NewRunController.ApplySelection al confirmar.
        // TurnManager los lee en ConfigureCombat para derivar PlayerActiveType
        // según el mundo activo.
        public ElementType PlayerWorldAType { get; set; } = ElementType.Rojo;
        public ElementType PlayerWorldBType { get; set; } = ElementType.Amarillo;

        // Carta especial dual drafteada en NewRunScene (Sub-PR 3E). Runtime-only,
        // no serializada: la escribe NewRunController.ApplySelection al confirmar
        // y la consume InitializeDeck para inyectarla como la "10ª carta" del mazo
        // inicial (GDD §5). Se transporta aparte del Deck porque InitializeDeck
        // sólo puebla cuando Deck.Count == 0 (poblarlo antes bloquearía el starter
        // base). Reset a null en Reset().
        public CardDeckEntry PendingStarterCard { get; set; } = null;

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
            PendingStarterCard = null;
            Relics.Clear();
            EnsureInitialized(map);
        }

        /// <summary>
        /// Restaura el estado a "combate jugable de nuevo" tras una derrota (botón
        /// Reintentar). Limpia los flags de outcome y devuelve el HP a full —
        /// agnóstico al número base (usa PlayerMaxHP, hoy 60). NO toca
        /// HasPlayerHPInitialized (queda true; PlayerCurrentHP = PlayerMaxHP pasa el
        /// clamp de InitializeCombat sin caer a 1 HP) ni CurrentNodeId (retry = mismo
        /// nodo). Ver spec fix_combat_end_hp_sync (H3).
        /// </summary>
        public void PrepareForRetry()
        {
            RunFailed = false;
            PendingReturnFromBattle = false;
            LastNodeOutcome = NodeOutcome.None;
            if (PlayerMaxHP > 0) PlayerCurrentHP = PlayerMaxHP;
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

            // Carta drafteada en NewRunScene (Sub-PR 3E): se inyecta clonada como
            // la "10ª carta" del mazo inicial (GDD §5). Va aquí, tras el starter
            // base, porque el transporte vía PendingStarterCard evita el guard
            // Deck.Count == 0 que bloquearía cargar el starter si se poblara antes.
            if (PendingStarterCard != null && PendingStarterCard.IsValid)
            {
                Deck.Add(PendingStarterCard.Clone());
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

            // 4a follow-up: toda carta ganada/comprada/de-evento durante la run pasa por
            // la MISMA resolución de afinidad que el mazo inicial (RunSession.ConfigureCombat).
            // Una carta afín adopta los tipos de mundo elegidos por el jugador (A/B); las
            // neutras y las ya-duales se clonan sin cambios. Sin esto, una recompensa afín
            // (single, ElementType.None) entraría al mazo sin tipo de mundo y nunca
            // explotaría la afinidad — se veía como una carta de tipos ajenos a los elegidos.
            // AffinityResolver.Resolve ya clona internamente → no duplicar Clone aquí.
            CardDeckEntry resolved = AffinityResolver.Resolve(entry, PlayerWorldAType, PlayerWorldBType);
            if (resolved != null)
            {
                Deck.Add(resolved);
            }
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

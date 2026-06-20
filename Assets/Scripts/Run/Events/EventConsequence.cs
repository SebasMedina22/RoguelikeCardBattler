using System;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Relics;

namespace RoguelikeCardBattler.Run.Events
{
    /// <summary>
    /// Tipo de consecuencia de una decisión de evento. Data-driven por enum (no
    /// subclases por consecuencia, decisión cerrada del spec). La lógica de
    /// aplicación vive en <see cref="EventConsequence.Apply"/> (static puro,
    /// testeable sin UI).
    ///
    /// StartQuest va AL FINAL del enum (4b-2) para no renumerar los valores ya
    /// serializados de 4b-1.
    /// </summary>
    public enum ConsequenceType
    {
        GiveCard,
        RemoveCard,
        GiveGold,
        LoseGold,
        ModifyHP,
        GiveRelic,
        StartQuest     // 4b-2 — al final del enum, no renumera los 6 anteriores
    }

    /// <summary>
    /// Payload del tipo <see cref="ConsequenceType.StartQuest"/>: el Retazo entregado
    /// al aceptar (pasivo durante el trayecto) y la recompensa final en oro. El nodo
    /// destino NO se autora aquí — se resuelve en runtime por BFS forward al aceptar
    /// (ver <c>QuestDestinationResolver</c>).
    /// </summary>
    [Serializable]
    public class QuestData
    {
        [Tooltip("Retazo entregado al aceptar el quest (pasivo durante el trayecto).")]
        [SerializeField] public RelicDefinition CarriedRelic;
        [Tooltip("Oro otorgado al llegar al nodo destino.")]
        [SerializeField] public int FinalRewardGold;
    }

    /// <summary>
    /// Una consecuencia atómica de una decisión de evento. Datos puros
    /// serializables (espejo conceptual de <c>ShopItem</c>): el autor de un
    /// <see cref="EventDefinition"/> arma la lista y <see cref="Apply"/> la ejecuta
    /// sobre el <see cref="RunState"/>. El payload usado depende de <see cref="Type"/>.
    /// </summary>
    [Serializable]
    public class EventConsequence
    {
        [SerializeField] private ConsequenceType type;
        [Tooltip("Oro / HP delta. GiveGold/LoseGold usan la magnitud; ModifyHP respeta el signo.")]
        [SerializeField] private int amount;
        [Tooltip("Carta para GiveCard / RemoveCard.")]
        [SerializeField] private CardDefinition card;
        [Tooltip("Retazo para GiveRelic.")]
        [SerializeField] private RelicDefinition relic;
        [Tooltip("Payload para StartQuest (4b-2).")]
        [SerializeReference] private QuestData quest;

        public ConsequenceType Type => type;
        public int Amount => amount;
        public CardDefinition Card => card;
        public RelicDefinition Relic => relic;
        public QuestData Quest => quest;

        public EventConsequence() { }

        public EventConsequence(
            ConsequenceType type,
            int amount = 0,
            CardDefinition card = null,
            RelicDefinition relic = null,
            QuestData quest = null)
        {
            this.type = type;
            this.amount = amount;
            this.card = card;
            this.relic = relic;
            this.quest = quest;
        }

        /// <summary>
        /// Aplica la consecuencia sobre el RunState. Static y sin UI (espejo de
        /// <c>CampfireNodeController.ApplyRest</c> / <c>ShopNodeController.TryPurchase</c>)
        /// para que los tests EditMode la validen sin instanciar el panel. Reusa los
        /// helpers de mutación de RunState (AddCardToDeck / RemoveCardFromDeck /
        /// AddRelic). Clamps: oro ≥ 0, HP en [0, PlayerMaxHP].
        ///
        /// <paramref name="dispatcher"/> queda en la firma por contrato del spec
        /// (4b-2 lo usa para StartQuest); en 4b-1 ninguna consecuencia dispara hooks.
        /// </summary>
        public static void Apply(RunState state, RelicHookDispatcher dispatcher, EventConsequence c)
        {
            if (state == null || c == null) return;

            switch (c.type)
            {
                case ConsequenceType.GiveGold:
                    state.Gold = Mathf.Max(0, state.Gold + Mathf.Abs(c.amount));
                    break;

                case ConsequenceType.LoseGold:
                    state.Gold = Mathf.Max(0, state.Gold - Mathf.Abs(c.amount));
                    break;

                case ConsequenceType.ModifyHP:
                    // El signo del Amount decide curación (+) o daño (-). El cap
                    // superior es PlayerMaxHP; nunca baja de 0 (no mata por evento).
                    state.PlayerCurrentHP = Mathf.Clamp(state.PlayerCurrentHP + c.amount, 0, state.PlayerMaxHP);
                    break;

                case ConsequenceType.GiveCard:
                    // AddCardToDeck clona internamente y rutea por AffinityResolver
                    // (las afines adoptan los tipos del jugador; neutras/duales pasan).
                    if (c.card != null) state.AddCardToDeck(c.card);
                    break;

                case ConsequenceType.RemoveCard:
                    RemoveMatchingCard(state, c.card);
                    break;

                case ConsequenceType.GiveRelic:
                    if (c.relic != null) state.AddRelic(c.relic);
                    break;

                case ConsequenceType.StartQuest:
                    // StartQuest NO se resuelve aquí: necesita el mapa (BFS forward)
                    // que la firma static no tiene. Lo maneja EventNodeController al
                    // detectar este tipo en la choice (spec §Wiring de StartQuest).
                    break;
            }
        }

        /// <summary>
        /// Quita del mazo la primera entrada que referencia <paramref name="card"/>
        /// (por referencia de CardDefinition). Funciona para cartas que comparten el
        /// asset original (las neutras del starter/recompensa lo conservan a través
        /// de <c>CardDeckEntry.Clone</c>); las afines resueltas a dual runtime tienen
        /// instancias propias y no matchean por diseño.
        /// </summary>
        private static void RemoveMatchingCard(RunState state, CardDefinition card)
        {
            if (card == null) return;
            CardDeckEntry match = null;
            foreach (CardDeckEntry entry in state.Deck)
            {
                if (entry == null) continue;
                if (entry.SingleCard == card)
                {
                    match = entry;
                    break;
                }
                if (entry.DualCard != null && (entry.DualCard.SideA == card || entry.DualCard.SideB == card))
                {
                    match = entry;
                    break;
                }
            }
            if (match != null) state.RemoveCardFromDeck(match);
        }
    }
}

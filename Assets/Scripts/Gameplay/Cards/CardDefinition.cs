using System.Collections.Generic;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Combat;

namespace RoguelikeCardBattler.Gameplay.Cards
{
    /// <summary>
    /// ScriptableObject de carta simple: coste, efectos, tipo elemental y metadatos.
    /// </summary>
    [CreateAssetMenu(menuName = "Cards/Card Definition", fileName = "CardDefinition")]
    public class CardDefinition : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string cardName = "New Card";
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private int cost = 1;
        [SerializeField] private CardType type = CardType.Attack;
        [SerializeField] private CardRarity rarity = CardRarity.Common;
        [SerializeField] private CardTarget target = CardTarget.SingleEnemy;
        [SerializeField] private List<string> tags = new List<string>();
        [SerializeField] private List<EffectRef> effects = new List<EffectRef>();
        [SerializeField] private ElementType elementType = ElementType.None;

        public string Id => id;
        public string CardName => cardName;
        public string Description => description;
        public int Cost => cost;
        public CardType Type => type;
        public CardRarity Rarity => rarity;
        public CardTarget Target => target;
        public IReadOnlyList<string> Tags => tags;
        public IReadOnlyList<EffectRef> Effects => effects;
        public ElementType ElementType => elementType;

        public void SetDebugData(
            string newId,
            string newName,
            string newDescription,
            int newCost,
            CardType newType,
            CardRarity newRarity,
            CardTarget newTarget,
            List<string> newTags,
            List<EffectRef> newEffects,
            ElementType newElementType = ElementType.None)
        {
            id = newId;
            cardName = newName;
            description = newDescription;
            cost = newCost;
            type = newType;
            rarity = newRarity;
            target = newTarget;
            tags = newTags;
            effects = newEffects;
            elementType = newElementType;
        }
    }
}


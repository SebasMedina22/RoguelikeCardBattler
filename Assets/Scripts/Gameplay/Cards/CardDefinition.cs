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
        [SerializeField] private Sprite art = null;   // ilustración de la carta (C7). null = fallback a texto.
        [SerializeField] private CardUpgradeDef _upgrade = new CardUpgradeDef();

        public CardUpgradeDef Upgrade => _upgrade;

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
        public Sprite Art => art;

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
            ElementType newElementType = ElementType.None,
            Sprite newArt = null)
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
            art = newArt;
        }

        /// <summary>
        /// Crea una copia runtime de esta carta aplicando los overrides definidos
        /// en <see cref="Upgrade"/>. Se usa al ejecutar la mejora en la Hoguera
        /// (sub-PR 3C) — TurnManager no necesita cambios porque la carta clonada
        /// conserva la API de CardDefinition.
        /// </summary>
        public CardDefinition CreateUpgradedClone()
        {
            CardDefinition clone = ScriptableObject.CreateInstance<CardDefinition>();

            string newName = !string.IsNullOrEmpty(_upgrade.UpgradedName)
                ? _upgrade.UpgradedName
                : cardName + "+";

            string newDescription = !string.IsNullOrEmpty(_upgrade.UpgradedDescription)
                ? _upgrade.UpgradedDescription
                : description;

            int newCost = _upgrade.OverrideCost ? _upgrade.UpgradedCost : cost;

            List<EffectRef> newEffects = _upgrade.UpgradedEffects != null && _upgrade.UpgradedEffects.Count > 0
                ? new List<EffectRef>(_upgrade.UpgradedEffects)
                : new List<EffectRef>(effects);

            clone.SetDebugData(
                id + "_upgraded",
                newName,
                newDescription,
                newCost,
                type,
                rarity,
                target,
                new List<string>(tags),
                newEffects,
                elementType,
                art);   // el clon de upgrade conserva la ilustración base (D3)

            return clone;
        }
    }
}


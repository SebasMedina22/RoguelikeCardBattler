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
        // Afinidad de tipo (4a, DD-022 opción A): si true, la carta NO tiene tipo
        // fijo — adopta en runtime el tipo del mundo activo. AffinityResolver la
        // resuelve a una dual runtime (lado A = tipo Mundo A, lado B = tipo Mundo B)
        // antes de que llegue al mazo. Su elementType autorado en el SO es
        // irrelevante (se autora None). affinity=false → usa elementType fijo
        // (incluido None = carta neutra 90%).
        [SerializeField] private bool affinity = false;
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
        public bool Affinity => affinity;
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
            Sprite newArt = null,
            bool newAffinity = false)
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
            affinity = newAffinity;
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
                art,        // el clon de upgrade conserva la ilustración base (D3)
                affinity);  // y el flag de afinidad (caso defensivo: en el flujo normal
                            // el afín ya es dual antes de mejorarse, ver CreateAffinityVariant)

            return clone;
        }

        /// <summary>
        /// Crea una variante runtime de esta carta tipada para un mundo concreto
        /// (4a, afinidad DD-022). Idéntica al cuerpo (effects, cost, name,
        /// description, type, rarity, target, tags, art) salvo que su
        /// <see cref="ElementType"/> pasa a ser <paramref name="worldType"/> y su
        /// <see cref="Affinity"/> queda en false (ya resuelta a un tipo).
        ///
        /// A diferencia de <see cref="CreateUpgradedClone"/> (que NO copia el
        /// payload de upgrade), PRESERVA <c>_upgrade</c> compartiendo la referencia
        /// para que la dual afín resuelta siga siendo mejorable en la Hoguera.
        /// El <see cref="CardUpgradeDef"/> es read-only en runtime, así que
        /// compartir la referencia es seguro. NO toca el SO de origen.
        /// </summary>
        public CardDefinition CreateAffinityVariant(ElementType worldType)
        {
            CardDefinition clone = ScriptableObject.CreateInstance<CardDefinition>();

            clone.SetDebugData(
                id,
                cardName,
                description,
                cost,
                type,
                rarity,
                target,
                new List<string>(tags),
                new List<EffectRef>(effects),
                worldType,
                art,
                false);   // affinity=false: la variante ya está resuelta a worldType

            clone._upgrade = _upgrade;   // comparte el payload de upgrade (read-only)
            return clone;
        }
    }
}


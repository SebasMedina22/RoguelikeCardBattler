using System.Collections.Generic;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Relics;

namespace RoguelikeCardBattler.Run.Shop
{
    /// <summary>
    /// Configuración del nodo Tienda. Espejo de <c>CampfireConfig</c>: mantiene
    /// los sprites del parallax, los precios y los pools de stock fuera del código
    /// para que el arte y el balance se ajusten sin recompilar. Si los sprites son
    /// null el controller usa colores de fallback (la feature debe funcionar antes
    /// de tener arte real).
    ///
    /// Decisión de diseño (Sub-PR 3D): los pools son DEDICADOS de la Tienda
    /// (opción B), NO se reusan los de RunCombatConfig.RewardPool/EliteRelicDropPool.
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike/Shop Config", fileName = "ShopConfig")]
    public class ShopConfig : ScriptableObject
    {
        [Header("Parallax (3 capas: pared / estanterías / mostrador)")]
        [SerializeField] private Sprite wallSprite;
        [SerializeField] private Sprite shelfSprite;
        [SerializeField] private Sprite counterSprite;

        [Header("Tamaño de stock")]
        [SerializeField, Min(0)] private int cardSlots = 3;
        [SerializeField, Min(0)] private int relicSlots = 2;

        // Balance ajustado 2026-06-04: la economía da ~10 oro/nodo, así que los
        // precios originales (50/75/100) eran inalcanzables. Reducidos ~50-60%.
        [Header("Precios placeholder (oro)")]
        [SerializeField, Min(0)] private int priceCommon = 20;
        [SerializeField, Min(0)] private int priceUncommon = 35;
        [SerializeField, Min(0)] private int priceRare = 55;
        [SerializeField, Min(0)] private int priceLegendary = 80;
        [SerializeField, Min(0)] private int relicPrice = 40;

        [Header("Servicio: eliminar carta del mazo (precio escalante)")]
        [SerializeField, Min(0), Tooltip("Precio en la 1ª tienda visitada.")]
        private int removeCardBasePrice = 30;
        [SerializeField, Min(0), Tooltip("Incremento lineal por cada tienda ya completada.")]
        private int removeCardPriceStep = 5;

        [Header("Pools de stock (dedicados de la tienda)")]
        [SerializeField] private List<CardDeckEntry> cardPool = new List<CardDeckEntry>();
        [SerializeField] private List<RelicDefinition> relicPool = new List<RelicDefinition>();

        public Sprite WallSprite => wallSprite;
        public Sprite ShelfSprite => shelfSprite;
        public Sprite CounterSprite => counterSprite;

        public int CardSlots => Mathf.Max(0, cardSlots);
        public int RelicSlots => Mathf.Max(0, relicSlots);

        public int RelicPrice => Mathf.Max(0, relicPrice);

        public IReadOnlyList<CardDeckEntry> CardPool => cardPool;
        public IReadOnlyList<RelicDefinition> RelicPool => relicPool;

        /// <summary>
        /// Precio del servicio "eliminar carta" según cuántas tiendas se hayan
        /// completado antes de esta. Lineal: base + step × tiendas previas
        /// (1ª 75, 2ª 80, 3ª 85 con los defaults).
        /// </summary>
        public int RemoveCardPriceFor(int shopsCompletedBefore)
        {
            return Mathf.Max(0, removeCardBasePrice + removeCardPriceStep * Mathf.Max(0, shopsCompletedBefore));
        }

        /// <summary>
        /// Precio de una carta según su rareza. Placeholder de balance cerrado en
        /// el spec de 3D.
        /// </summary>
        public int PriceForRarity(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return Mathf.Max(0, priceCommon);
                case CardRarity.Uncommon: return Mathf.Max(0, priceUncommon);
                case CardRarity.Rare: return Mathf.Max(0, priceRare);
                case CardRarity.Legendary: return Mathf.Max(0, priceLegendary);
                default: return Mathf.Max(0, priceCommon);
            }
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        /// <summary>
        /// Setter de datos para tests EditMode (mismo patrón que CardDefinition.
        /// SetDebugData). Permite armar un ShopConfig sin asset en disco.
        /// </summary>
        public void SetDebugData(
            List<CardDeckEntry> cards,
            List<RelicDefinition> relics,
            int cardSlotsValue,
            int relicSlotsValue,
            int common,
            int uncommon,
            int rare,
            int relicPriceValue,
            int removeBase,
            int removeStep)
        {
            cardPool = cards ?? new List<CardDeckEntry>();
            relicPool = relics ?? new List<RelicDefinition>();
            cardSlots = cardSlotsValue;
            relicSlots = relicSlotsValue;
            priceCommon = common;
            priceUncommon = uncommon;
            priceRare = rare;
            relicPrice = relicPriceValue;
            removeCardBasePrice = removeBase;
            removeCardPriceStep = removeStep;
        }
#endif

#if UNITY_EDITOR
        /// <summary>
        /// Puebla SÓLO los pools (cartas y Retazos) desde el editor tooling
        /// (ShopConfigSetup). Deja precios/slots/sprites intactos para no pisar
        /// ajustes de balance manuales. Idempotente: reemplaza el contenido de
        /// los pools en cada corrida.
        /// </summary>
        public void EditorPopulatePools(List<CardDeckEntry> cards, List<RelicDefinition> relics)
        {
            cardPool = cards ?? new List<CardDeckEntry>();
            relicPool = relics ?? new List<RelicDefinition>();
        }
#endif
    }
}

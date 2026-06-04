using System;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Relics;

namespace RoguelikeCardBattler.Run.Shop
{
    /// <summary>
    /// Tipo de ítem que ofrece la Tienda. La lógica de compra (ShopNodeController.
    /// TryPurchase) ramifica por este Kind para aplicar el efecto correcto sobre
    /// el RunState.
    /// </summary>
    public enum ShopItemKind
    {
        Card,
        Relic,
        RemoveCard
    }

    /// <summary>
    /// Un ítem del stock de la Tienda. Data class pura: ShopNodeController.BuildStock
    /// arma la lista, el dispatcher de Retazos puede agregar ítems extra vía
    /// OnShopStockBuilt, y la UI dibuja un botón por ítem. El efecto de compra se
    /// aplica en TryPurchase según <see cref="Kind"/> usando los payloads.
    /// </summary>
    public class ShopItem
    {
        public ShopItemKind Kind { get; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public bool Purchased { get; set; }

        // Payloads tipados según Kind. Card → CardPayload (clon al comprar),
        // Relic → RelicPayload. RemoveCard no usa payload: la carta a eliminar la
        // elige el jugador en el sub-panel selector.
        public CardDeckEntry CardPayload { get; }
        public RelicDefinition RelicPayload { get; }

        // Gancho cosmético/extensible que se dispara tras una compra exitosa.
        // Los Retazos que agregan ítems vía hook pueden engancharse aquí.
        public Action OnPurchase { get; set; }

        public ShopItem(
            ShopItemKind kind,
            string title,
            string description,
            int price,
            CardDeckEntry cardPayload = null,
            RelicDefinition relicPayload = null)
        {
            Kind = kind;
            Title = title;
            Description = description;
            Price = price;
            CardPayload = cardPayload;
            RelicPayload = relicPayload;
        }
    }
}

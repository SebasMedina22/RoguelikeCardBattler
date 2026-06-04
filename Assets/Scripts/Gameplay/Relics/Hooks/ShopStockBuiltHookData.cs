using System.Collections.Generic;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Run;
using RoguelikeCardBattler.Run.Shop;

namespace RoguelikeCardBattler.Gameplay.Relics.Hooks
{
    /// <summary>
    /// Disparado por <c>ShopNodeController</c> después de construir el stock base
    /// (cartas, Retazos, servicio eliminar carta) y antes de dibujar el panel.
    /// Los Retazos suscritos pueden agregar/modificar ítems en la lista mutable
    /// (ej: un descuento, o un ítem exclusivo). Espejo de
    /// <c>CampfireOptionsBuiltHookData</c>.
    /// TurnManager es null: estamos fuera de combate (los Grant* del context
    /// hacen no-op por guard).
    /// </summary>
    public class ShopStockBuiltHookData : RelicHookContext
    {
        public List<ShopItem> Stock { get; }

        public ShopStockBuiltHookData(
            RunState runState,
            RelicHookDispatcher dispatcher,
            List<ShopItem> stock)
            : base(runState, null, dispatcher)
        {
            Stock = stock;
        }
    }
}

using RoguelikeCardBattler.Gameplay.Combat;

namespace RoguelikeCardBattler.Gameplay.Cards
{
    /// <summary>
    /// Hogar canónico de los helpers de presentación de cartas en texto (token de
    /// tipo coloreado, carta representante de una entry, criterio de label dual).
    /// Esta lógica vivía duplicada como `private static` en ShopNodeController y
    /// CampfireNodeController (BuildCardSelectLabel / CardToken / RepresentativeCard);
    /// el visor de mazo la extrae acá para NO crear una tercera copia que arrastre
    /// el drift (decisión del spec del visor, §Reuso).
    ///
    /// Migrar ShopNodeController/CampfireNodeController a consumir estos métodos es
    /// follow-up FUERA de scope del visor (anotado en `_roadmap.md` Future work):
    /// tocaría sus tests y no aporta al visor. Por ahora este es el único consumidor.
    /// </summary>
    public static class CardDisplay
    {
        /// <summary>
        /// Carta representante de una entry, usada para orden/coste/tipo. Single →
        /// la propia carta; dual → SideA con fallback a SideB (tolera SideA null).
        /// Devuelve null si la entry no tiene ninguna carta resoluble.
        /// </summary>
        public static CardDefinition RepresentativeCard(CardDeckEntry entry)
        {
            if (entry == null) return null;
            if (entry.SingleCard != null) return entry.SingleCard;
            if (entry.DualCard != null) return entry.DualCard.SideA ?? entry.DualCard.SideB;
            return null;
        }

        /// <summary>
        /// Token rich-text "[Tipo] Nombre" de un lado (color del tipo vía
        /// ElementTypeColors.TypePrefix). Tipo None ⇒ solo el nombre (sin token de
        /// color). Lado null ⇒ "?" (nunca lanza NRE).
        /// </summary>
        public static string CardToken(CardDefinition card)
        {
            if (card == null) return "?";
            string prefix = ElementTypeColors.TypePrefix(card.ElementType);
            return string.IsNullOrEmpty(prefix) ? card.CardName : $"{prefix} {card.CardName}";
        }

        /// <summary>Tipo elemental de un lado, null-safe (null ⇒ None).</summary>
        public static ElementType SideType(CardDefinition card) =>
            card != null ? card.ElementType : ElementType.None;

        /// <summary>
        /// Token combinado de una entry sin coste ni marcadores. Single ⇒ su token;
        /// dual ⇒ "[TipoA] NombreA / [TipoB] NombreB", que colapsa a un solo token
        /// SOLO si nombre Y tipo coinciden en ambos lados no-null (mismo criterio que
        /// ShopNodeController.BuildCardSelectLabel). Lados null se renderizan "?" y
        /// nunca colapsan. Nunca lanza NRE.
        /// </summary>
        public static string EntryToken(CardDeckEntry entry)
        {
            if (entry == null || !entry.IsValid) return "";
            if (entry.DualCard != null)
            {
                CardDefinition a = entry.DualCard.SideA;
                CardDefinition b = entry.DualCard.SideB;
                string tokenA = CardToken(a);
                string tokenB = CardToken(b);
                bool sameName = NameOf(a) == NameOf(b);
                bool sameType = SideType(a) == SideType(b);
                bool bothNonNull = a != null && b != null;
                return (sameName && sameType && bothNonNull) ? tokenA : $"{tokenA} / {tokenB}";
            }
            return entry.SingleCard != null ? CardToken(entry.SingleCard) : "";
        }

        // Nombre de un lado, null-safe (null ⇒ "?"), usado por el criterio de colapso.
        private static string NameOf(CardDefinition card) =>
            card != null ? card.CardName : "?";
    }
}

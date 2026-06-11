using UnityEngine;

namespace RoguelikeCardBattler.Gameplay.Combat
{
    /// <summary>
    /// Fuente única de verdad del color de cada tipo elemental (slot C8 de la
    /// auditoría de arte). Los 6 tipos son placeholders POR color
    /// (Rojo/Amarillo/Azul/Morado/Negro/Blanco), así que la UI puede pintar
    /// botones y labels de tipo sin necesidad de arte/IA. Cualquier sitio que
    /// muestre un <see cref="ElementType"/> debe tomar su color de acá para que el
    /// día que se renombren a tipos finales haya un solo lugar que tocar.
    /// </summary>
    public static class ElementTypeColors
    {
        // Paleta crayón: saturada pero no neón, coherente con la estética handmade.
        // Negro/Blanco son los extremos reales del tipo (se corrigen por contraste
        // en ReadableOnDark / ReadableTextOn según dónde se usen).
        private static readonly Color Rojo = new Color(0.82f, 0.22f, 0.18f, 1f);
        private static readonly Color Amarillo = new Color(0.95f, 0.80f, 0.25f, 1f);
        private static readonly Color Azul = new Color(0.27f, 0.50f, 0.85f, 1f);
        private static readonly Color Morado = new Color(0.60f, 0.35f, 0.72f, 1f);
        private static readonly Color Negro = new Color(0.18f, 0.18f, 0.20f, 1f);
        private static readonly Color Blanco = new Color(0.92f, 0.92f, 0.88f, 1f);
        private static readonly Color Neutro = new Color(0.55f, 0.55f, 0.55f, 1f);

        // Tintas de texto para máximo contraste sobre un fondo coloreado.
        private static readonly Color DarkInk = new Color(0.12f, 0.10f, 0.10f, 1f);
        private static readonly Color LightInk = Color.white;

        // Piso de luminancia para texto coloreado sobre fondos oscuros (HUD/cartas):
        // por debajo, el color se levanta hacia blanco para no perderse (ej. Negro).
        private const float MinLuminanceOnDark = 0.45f;
        // Umbral para decidir tinta oscura vs clara sobre un fondo dado.
        private const float DarkTextThreshold = 0.55f;

        /// <summary>Color canónico del tipo. None devuelve un gris neutro.</summary>
        public static Color For(ElementType type) => type switch
        {
            ElementType.Rojo => Rojo,
            ElementType.Amarillo => Amarillo,
            ElementType.Azul => Azul,
            ElementType.Morado => Morado,
            ElementType.Negro => Negro,
            ElementType.Blanco => Blanco,
            _ => Neutro
        };

        /// <summary>
        /// Variante del color del tipo pensada para TEXTO coloreado sobre fondos
        /// oscuros (labels del HUD, prefijo de tipo en las cartas). Levanta los
        /// colores oscuros hacia blanco hasta superar un piso de luminancia para
        /// que Negro (y rojos profundos) sigan siendo legibles.
        /// </summary>
        public static Color ReadableOnDark(ElementType type)
        {
            Color c = For(type);
            float lum = Luminance(c);
            if (lum < MinLuminanceOnDark)
            {
                // t crece cuanto más oscuro es el color; clamp evita división por ~0.
                float t = (MinLuminanceOnDark - lum) / Mathf.Max(0.01f, 1f - lum);
                c = Color.Lerp(c, Color.white, Mathf.Clamp01(t));
            }
            return c;
        }

        /// <summary>
        /// Tinta de texto (oscura o blanca) que mejor contrasta sobre el fondo dado.
        /// Para labels dentro de botones tintados con <see cref="For"/>.
        /// </summary>
        public static Color ReadableTextOn(Color background) =>
            Luminance(background) > DarkTextThreshold ? DarkInk : LightInk;

        /// <summary>
        /// Prefijo rich-text "[Tipo]" coloreado con el color legible sobre fondo oscuro
        /// (ReadableOnDark). Fuente única de verdad del patrón que antes vivía inline en
        /// CardHandView.BuildCardLabel. Devuelve string.Empty para ElementType.None (sin
        /// prefijo). NO incluye espacio final ni separador — el caller decide el formato.
        /// </summary>
        public static string TypePrefix(ElementType type)
        {
            if (type == ElementType.None) return string.Empty;
            string hex = ColorUtility.ToHtmlStringRGB(ReadableOnDark(type));
            return $"<color=#{hex}>[{type}]</color>";
        }

        /// <summary>
        /// Atenúa un color preservando su alpha (para estados deshabilitados que
        /// igual deben dejar leer de qué tipo se trata).
        /// </summary>
        public static Color Dim(Color color, float factor)
        {
            factor = Mathf.Clamp01(factor);
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
        }

        // Luminancia perceptual (Rec. 601) — suficiente para decisiones de contraste.
        private static float Luminance(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
    }
}

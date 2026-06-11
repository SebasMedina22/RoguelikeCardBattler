using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Combat;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// Blinda el helper de tinte por tipo (C8): fuente única de verdad
    /// ElementType→Color. Verifica que cada tipo tiene color distinto, que None es
    /// neutro, que el texto contrasta sobre el fondo, y que ReadableOnDark levanta
    /// los colores oscuros (Negro) para que sigan siendo legibles.
    /// </summary>
    public class ElementTypeColorsTests
    {
        private static readonly ElementType[] ColoredTypes =
        {
            ElementType.Rojo, ElementType.Amarillo, ElementType.Azul,
            ElementType.Morado, ElementType.Negro, ElementType.Blanco
        };

        private static float Luminance(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        [Test]
        public void For_EveryEnumValue_ReturnsOpaqueColor()
        {
            foreach (ElementType type in Enum.GetValues(typeof(ElementType)))
            {
                Color c = ElementTypeColors.For(type);
                Assert.AreEqual(1f, c.a, 1e-4f, $"{type} debe ser opaco");
            }
        }

        [Test]
        public void For_SixTypes_AreAllDistinct()
        {
            var seen = new HashSet<Color>();
            foreach (ElementType type in ColoredTypes)
            {
                Assert.IsTrue(seen.Add(ElementTypeColors.For(type)),
                    $"El color de {type} está duplicado con otro tipo");
            }
        }

        [Test]
        public void ReadableTextOn_LightBackground_IsDark_And_DarkBackground_IsLight()
        {
            // Amarillo y Blanco son fondos claros → texto oscuro.
            Assert.Less(Luminance(ElementTypeColors.ReadableTextOn(ElementTypeColors.For(ElementType.Amarillo))), 0.5f);
            Assert.Less(Luminance(ElementTypeColors.ReadableTextOn(ElementTypeColors.For(ElementType.Blanco))), 0.5f);
            // Azul y Negro son fondos oscuros → texto claro.
            Assert.Greater(Luminance(ElementTypeColors.ReadableTextOn(ElementTypeColors.For(ElementType.Azul))), 0.5f);
            Assert.Greater(Luminance(ElementTypeColors.ReadableTextOn(ElementTypeColors.For(ElementType.Negro))), 0.5f);
        }

        [Test]
        public void ReadableOnDark_LiftsDarkTypes_AboveVisibilityFloor()
        {
            // Negro de fondo se pierde sobre un HUD oscuro; ReadableOnDark lo levanta.
            float negroRaw = Luminance(ElementTypeColors.For(ElementType.Negro));
            float negroOnDark = Luminance(ElementTypeColors.ReadableOnDark(ElementType.Negro));
            Assert.Greater(negroOnDark, negroRaw, "Negro debería levantarse para ser legible sobre oscuro");
            Assert.GreaterOrEqual(negroOnDark, 0.44f);
        }

        [Test]
        public void ReadableOnDark_KeepsBrightTypesEssentiallyUnchanged()
        {
            // Amarillo ya es brillante: no debe alterarse (sigue por encima del piso).
            Color raw = ElementTypeColors.For(ElementType.Amarillo);
            Color onDark = ElementTypeColors.ReadableOnDark(ElementType.Amarillo);
            Assert.AreEqual(raw.r, onDark.r, 1e-4f);
            Assert.AreEqual(raw.g, onDark.g, 1e-4f);
            Assert.AreEqual(raw.b, onDark.b, 1e-4f);
        }

        [Test]
        public void Dim_PreservesAlpha_AndDarkens()
        {
            Color dimmed = ElementTypeColors.Dim(ElementTypeColors.For(ElementType.Rojo), 0.4f);
            Color raw = ElementTypeColors.For(ElementType.Rojo);
            Assert.AreEqual(raw.a, dimmed.a, 1e-4f, "Dim debe preservar el alpha");
            Assert.Less(Luminance(dimmed), Luminance(raw), "Dim debe oscurecer");
        }

        // ── TypePrefix (#104) ─────────────────────────────────────────────────

        [Test]
        public void TypePrefix_None_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, ElementTypeColors.TypePrefix(ElementType.None),
                "None no debe generar prefijo");
        }

        [Test]
        public void TypePrefix_ColoredType_ContainsNameInBrackets()
        {
            string prefix = ElementTypeColors.TypePrefix(ElementType.Rojo);
            StringAssert.Contains("[Rojo]", prefix, "El prefijo debe contener el nombre del tipo entre corchetes");
            StringAssert.StartsWith("<color=#", prefix, "El prefijo debe abrirse con rich-text de color");
            StringAssert.EndsWith("</color>", prefix, "El prefijo debe cerrarse con </color>");
        }

        [Test]
        public void TypePrefix_HexMatchesReadableOnDark_ForRojoAndNegro()
        {
            // Verifica que el hex embebido es el de ReadableOnDark (no el de For).
            foreach (ElementType type in new[] { ElementType.Rojo, ElementType.Negro })
            {
                string expected = ColorUtility.ToHtmlStringRGB(ElementTypeColors.ReadableOnDark(type));
                string prefix = ElementTypeColors.TypePrefix(type);
                StringAssert.Contains(expected, prefix,
                    $"El hex de {type} debe coincidir con ReadableOnDark");
            }
        }

        [Test]
        public void TypePrefix_AllNonNoneTypes_ProduceNonEmptyPrefixWithName()
        {
            foreach (ElementType type in ColoredTypes)
            {
                string prefix = ElementTypeColors.TypePrefix(type);
                Assert.IsNotEmpty(prefix, $"{type} debe producir prefijo no vacío");
                StringAssert.Contains($"[{type}]", prefix,
                    $"{type} debe incluir su nombre entre corchetes");
            }
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using RoguelikeCardBattler.Gameplay.Combat;

namespace RoguelikeCardBattler.Tests.EditMode
{
    public class ElementEffectivenessTests
    {
        private static IEnumerable<TestCaseData> MatrixCases()
        {
            // Diagonals
            yield return new TestCaseData(ElementType.Rojo, ElementType.Rojo, Effectiveness.Neutro).SetName("Rojo_vs_Rojo");
            yield return new TestCaseData(ElementType.Amarillo, ElementType.Amarillo, Effectiveness.Neutro).SetName("Amarillo_vs_Amarillo");
            yield return new TestCaseData(ElementType.Azul, ElementType.Azul, Effectiveness.Neutro).SetName("Azul_vs_Azul");
            yield return new TestCaseData(ElementType.Morado, ElementType.Morado, Effectiveness.Neutro).SetName("Morado_vs_Morado");
            yield return new TestCaseData(ElementType.Negro, ElementType.Negro, Effectiveness.Neutro).SetName("Negro_vs_Negro");
            yield return new TestCaseData(ElementType.Blanco, ElementType.Blanco, Effectiveness.Neutro).SetName("Blanco_vs_Blanco");

            // Rojo row
            yield return new TestCaseData(ElementType.Rojo, ElementType.Amarillo, Effectiveness.PocoEficaz);
            yield return new TestCaseData(ElementType.Rojo, ElementType.Azul, Effectiveness.SuperEficaz);
            yield return new TestCaseData(ElementType.Rojo, ElementType.Morado, Effectiveness.Neutro);
            yield return new TestCaseData(ElementType.Rojo, ElementType.Negro, Effectiveness.PocoEficaz);
            yield return new TestCaseData(ElementType.Rojo, ElementType.Blanco, Effectiveness.SuperEficaz);

            // Amarillo row
            yield return new TestCaseData(ElementType.Amarillo, ElementType.Rojo, Effectiveness.SuperEficaz);
            yield return new TestCaseData(ElementType.Amarillo, ElementType.Azul, Effectiveness.PocoEficaz);
            yield return new TestCaseData(ElementType.Amarillo, ElementType.Morado, Effectiveness.SuperEficaz);
            yield return new TestCaseData(ElementType.Amarillo, ElementType.Negro, Effectiveness.Neutro);
            yield return new TestCaseData(ElementType.Amarillo, ElementType.Blanco, Effectiveness.PocoEficaz);

            // Azul row
            yield return new TestCaseData(ElementType.Azul, ElementType.Rojo, Effectiveness.PocoEficaz);
            yield return new TestCaseData(ElementType.Azul, ElementType.Amarillo, Effectiveness.SuperEficaz);
            yield return new TestCaseData(ElementType.Azul, ElementType.Morado, Effectiveness.PocoEficaz);
            yield return new TestCaseData(ElementType.Azul, ElementType.Negro, Effectiveness.SuperEficaz);
            yield return new TestCaseData(ElementType.Azul, ElementType.Blanco, Effectiveness.Neutro);

            // Morado row
            yield return new TestCaseData(ElementType.Morado, ElementType.Rojo, Effectiveness.Neutro);
            yield return new TestCaseData(ElementType.Morado, ElementType.Amarillo, Effectiveness.SuperEficaz);
            yield return new TestCaseData(ElementType.Morado, ElementType.Azul, Effectiveness.PocoEficaz);
            yield return new TestCaseData(ElementType.Morado, ElementType.Negro, Effectiveness.SuperEficaz);
            yield return new TestCaseData(ElementType.Morado, ElementType.Blanco, Effectiveness.PocoEficaz);

            // Negro row
            yield return new TestCaseData(ElementType.Negro, ElementType.Rojo, Effectiveness.PocoEficaz);
            yield return new TestCaseData(ElementType.Negro, ElementType.Amarillo, Effectiveness.Neutro);
            yield return new TestCaseData(ElementType.Negro, ElementType.Azul, Effectiveness.SuperEficaz);
            yield return new TestCaseData(ElementType.Negro, ElementType.Morado, Effectiveness.PocoEficaz);
            yield return new TestCaseData(ElementType.Negro, ElementType.Blanco, Effectiveness.SuperEficaz);

            // Blanco row
            yield return new TestCaseData(ElementType.Blanco, ElementType.Rojo, Effectiveness.SuperEficaz);
            yield return new TestCaseData(ElementType.Blanco, ElementType.Amarillo, Effectiveness.PocoEficaz);
            yield return new TestCaseData(ElementType.Blanco, ElementType.Azul, Effectiveness.Neutro);
            yield return new TestCaseData(ElementType.Blanco, ElementType.Morado, Effectiveness.SuperEficaz);
            yield return new TestCaseData(ElementType.Blanco, ElementType.Negro, Effectiveness.PocoEficaz);

            // Optional compatibility for None (default enum value)
            yield return new TestCaseData(ElementType.None, ElementType.Rojo, Effectiveness.Neutro).SetName("None_vs_Rojo");
            yield return new TestCaseData(ElementType.Rojo, ElementType.None, Effectiveness.Neutro).SetName("Rojo_vs_None");
            yield return new TestCaseData(ElementType.None, ElementType.None, Effectiveness.Neutro).SetName("None_vs_None");
        }

        [TestCaseSource(nameof(MatrixCases))]
        public void GetEffectiveness_MatchesDefinedMatrix(ElementType attacker, ElementType defender, Effectiveness expected)
        {
            Effectiveness result = ElementEffectiveness.GetEffectiveness(attacker, defender);
            Assert.AreEqual(expected, result);
        }
    }
}


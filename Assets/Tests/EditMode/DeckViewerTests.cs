using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Cards.UI;
using RoguelikeCardBattler.Gameplay.Combat;

/// <summary>
/// Tests EditMode para los helpers static puros de DeckViewerView.
/// No instancia UI. Sigue el patrón de fixtures de NewRunTests/ShopTests/CampfireTests:
/// CardDefinition vía ScriptableObject.CreateInstance + SetDebugData;
/// CardUpgradeDef.SetTestData está bajo UNITY_INCLUDE_TESTS (disponible en EditMode).
/// </summary>
[TestFixture]
public class DeckViewerTests
{
    // ────────────────────────────────────────
    // Helpers de fixture
    // ────────────────────────────────────────

    private static CardDefinition MakeCard(string id, string name, int cost, CardType type, ElementType elem = ElementType.None)
    {
        var c = ScriptableObject.CreateInstance<CardDefinition>();
        c.SetDebugData(id, name, $"Desc {name}", cost, type, CardRarity.Common, CardTarget.SingleEnemy,
                       new List<string>(), new List<EffectRef>(), elem);
        return c;
    }

    private static CardDeckEntry SingleEntry(CardDefinition c)
    {
        var e = new CardDeckEntry();
        e.SetSingleCard(c);
        return e;
    }

    private static CardDeckEntry DualEntry(CardDefinition sideA, CardDefinition sideB)
    {
        var dual = ScriptableObject.CreateInstance<DualCardDefinition>();
        dual.InitRuntimeSides("dual_id", "Dual", sideA, sideB);
        var e = new CardDeckEntry();
        e.SetDualCard(dual);
        return e;
    }

    // Cuenta ocurrencias (no solapadas) de una subcadena. Usado para verificar que
    // un nombre colapsado aparece exactamente una vez en el label.
    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, System.StringComparison.Ordinal)) != -1)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }

    // ────────────────────────────────────────
    // TEST 1 — Orden tipo → coste → nombre
    // ────────────────────────────────────────

    [Test]
    public void SortForDisplay_OrdersByTypeCostName()
    {
        var cA = MakeCard("a", "Zephyr", 2, CardType.Attack);
        var cB = MakeCard("b", "Alpha",  1, CardType.Skill);
        var cC = MakeCard("c", "Mend",   2, CardType.Attack);   // mismo tipo/coste que cA, nombre menor
        var cD = MakeCard("d", "Heal",   1, CardType.Power);
        var cE = MakeCard("e", "Hex",    1, CardType.Curse);    // cubre el resto del enum
        var cF = MakeCard("f", "Burn",   1, CardType.Status);

        var deck = new List<CardDeckEntry>
        {
            // Orden de entrada deliberadamente mezclado para que el sort tenga que trabajar.
            SingleEntry(cF), SingleEntry(cA), SingleEntry(cE),
            SingleEntry(cB), SingleEntry(cC), SingleEntry(cD)
        };

        List<CardDeckEntry> sorted = DeckViewerView.SortForDisplay(deck);

        // Cadena completa del enum: Attack(0) < Skill(1) < Power(2) < Curse(3) < Status(4).
        Assert.AreEqual(CardType.Attack, sorted[0].SingleCard.Type);
        Assert.AreEqual(CardType.Attack, sorted[1].SingleCard.Type);
        Assert.AreEqual(CardType.Skill,  sorted[2].SingleCard.Type);
        Assert.AreEqual(CardType.Power,  sorted[3].SingleCard.Type);
        Assert.AreEqual(CardType.Curse,  sorted[4].SingleCard.Type);
        Assert.AreEqual(CardType.Status, sorted[5].SingleCard.Type);

        // Dentro de Attack (coste 2 ambas): Mend < Zephyr por nombre.
        Assert.AreEqual("Mend",   sorted[0].SingleCard.CardName);
        Assert.AreEqual("Zephyr", sorted[1].SingleCard.CardName);
    }

    // ────────────────────────────────────────
    // TEST 2 — Estabilidad en claves iguales
    // ────────────────────────────────────────

    [Test]
    public void SortForDisplay_IsStableOnEqualKeys()
    {
        // Dos cartas con mismo tipo/coste/nombre — difieren solo en Id.
        var c1 = MakeCard("id_1", "Strike", 1, CardType.Attack);
        var c2 = MakeCard("id_2", "Strike", 1, CardType.Attack);
        var e1 = SingleEntry(c1);
        var e2 = SingleEntry(c2);

        var deck = new List<CardDeckEntry> { e1, e2 };
        List<CardDeckEntry> sorted = DeckViewerView.SortForDisplay(deck);

        // El tie-break por Id garantiza orden determinista.
        Assert.AreEqual("id_1", sorted[0].SingleCard.Id);
        Assert.AreEqual("id_2", sorted[1].SingleCard.Id);
    }

    // ────────────────────────────────────────
    // TEST 3 — Sort no muta la entrada
    // ────────────────────────────────────────

    [Test]
    public void SortForDisplay_DoesNotMutateInput()
    {
        var cA = MakeCard("a", "Zorro", 3, CardType.Skill);
        var cB = MakeCard("b", "Alfa",  1, CardType.Attack);
        var original = new List<CardDeckEntry> { SingleEntry(cA), SingleEntry(cB) };

        List<CardDeckEntry> sorted = DeckViewerView.SortForDisplay(original);

        // La lista original conserva su orden y conteo.
        Assert.AreEqual(2, original.Count);
        Assert.AreEqual("Zorro", original[0].SingleCard.CardName);
        Assert.AreEqual("Alfa",  original[1].SingleCard.CardName);
        // La sortida es una lista nueva.
        Assert.AreNotSame(original, sorted);
        Assert.AreEqual(2, sorted.Count);
    }

    // ────────────────────────────────────────
    // TEST 4 — Null / vacío → lista vacía, sin excepción
    // ────────────────────────────────────────

    [Test]
    public void SortForDisplay_NullOrEmpty_ReturnsEmptyList()
    {
        List<CardDeckEntry> fromNull  = DeckViewerView.SortForDisplay(null);
        List<CardDeckEntry> fromEmpty = DeckViewerView.SortForDisplay(new List<CardDeckEntry>());

        Assert.IsNotNull(fromNull);
        Assert.AreEqual(0, fromNull.Count);
        Assert.IsNotNull(fromEmpty);
        Assert.AreEqual(0, fromEmpty.Count);
    }

    // ────────────────────────────────────────
    // TEST 5 — Dual con SideA null no crashea
    // ────────────────────────────────────────

    [Test]
    public void SortForDisplay_DualWithNullSideA_DoesNotThrow()
    {
        // SideA null, SideB válido → IsValid = true (DualCard != null), pero SideA es null.
        var sideB = MakeCard("b", "SideB", 2, CardType.Skill);
        var entry = DualEntry(null, sideB);

        Assert.IsTrue(entry.IsValid, "Entrada dual con SideA null debe ser IsValid");

        // SortForDisplay ni BuildRowLabel deben lanzar NRE.
        List<CardDeckEntry> sorted = null;
        Assert.DoesNotThrow(() => sorted = DeckViewerView.SortForDisplay(new List<CardDeckEntry> { entry }));

        string label = null;
        Assert.DoesNotThrow(() => label = DeckViewerView.BuildRowLabel(entry));

        // El fallback usa SideB → el entry aparece en la lista.
        Assert.AreEqual(1, sorted.Count);
        // El label del lado null se renderiza como "?" sin crashear.
        StringAssert.Contains("?",      label);
        StringAssert.Contains("SideB",  label);
    }

    // ────────────────────────────────────────
    // TEST 6 — Label simple incluye prefijo tipo + nombre + coste
    // ────────────────────────────────────────

    [Test]
    public void BuildRowLabel_Single_HasTypePrefixNameCost()
    {
        var c = MakeCard("x", "Strike", 1, CardType.Attack, ElementType.Rojo);
        var entry = SingleEntry(c);

        string label = DeckViewerView.BuildRowLabel(entry);

        // El prefijo de tipo Rojo produce "[Rojo]" coloreado.
        StringAssert.Contains("[Rojo]", label);
        StringAssert.Contains("Strike", label);
        StringAssert.Contains("1",      label);
        StringAssert.Contains("⚡",     label);
    }

    // ────────────────────────────────────────
    // TEST 7 — Label dual muestra AMBOS lados; colapsa cuando coinciden
    // ────────────────────────────────────────

    [Test]
    public void BuildRowLabel_Dual_ShowsBothSides_CollapsesWhenSameNameAndType()
    {
        var sideA = MakeCard("a", "BladeA", 2, CardType.Attack, ElementType.Azul);
        var sideB = MakeCard("b", "BladeB", 2, CardType.Attack, ElementType.Morado);
        var entry = DualEntry(sideA, sideB);

        string label = DeckViewerView.BuildRowLabel(entry);

        // Nombre y tipo distintos → NO colapsa: ambos lados presentes + separador " / ".
        StringAssert.Contains("BladeA", label);
        StringAssert.Contains("BladeB", label);
        StringAssert.Contains(" / ",    label);

        // Caso colapso: mismo nombre Y mismo tipo en ambos lados no-null.
        var sameA = MakeCard("sa", "Twin", 1, CardType.Skill, ElementType.Azul);
        var sameB = MakeCard("sb", "Twin", 1, CardType.Skill, ElementType.Azul);
        var sameEntry = DualEntry(sameA, sameB);

        string collapsed = DeckViewerView.BuildRowLabel(sameEntry);
        // Al colapsar, el separador " / " desaparece y el nombre aparece UNA sola vez.
        // (Si una regresión rompiera el colapso, el resultado sería "<tokenA> / <tokenB>"
        // — con separador y "Twin" dos veces — y ambos asserts fallarían. La aserción
        // anterior `!Contains("Twin / Twin")` era tautológica: los tags de color entre
        // medio hacían que esa subcadena literal nunca apareciera ni sin colapso.)
        Assert.IsFalse(collapsed.Contains(" / "), "No debe quedar el separador cuando colapsa");
        Assert.AreEqual(1, CountOccurrences(collapsed, "Twin"), "El nombre colapsado debe aparecer una sola vez");
    }

    // ────────────────────────────────────────
    // TEST 8 — Tipo None no inserta token de color
    // ────────────────────────────────────────

    [Test]
    public void BuildRowLabel_TypeNone_NoColorToken()
    {
        var c = MakeCard("n", "Neutral", 0, CardType.Status, ElementType.None);
        var entry = SingleEntry(c);

        string label = DeckViewerView.BuildRowLabel(entry);

        // TypePrefix(None) = ""; la fila no empieza con "[".
        StringAssert.Contains("Neutral", label);
        Assert.IsFalse(label.TrimStart().StartsWith("["), "Carta None no debe tener token de color al inicio");
    }

    // ────────────────────────────────────────
    // TEST 9 — Preview de mejora disponible (single)
    // ────────────────────────────────────────

    [Test]
    public void BuildUpgradePreview_Available_Single_ShowsUpgradedValues()
    {
        var c = MakeCard("s", "Strike", 1, CardType.Attack);
        c.Upgrade.SetTestData(
            overrideCost:         true,
            upgradedCost:         0,
            upgradedEffects:      null,
            upgradedName:         "Strike+",
            upgradedDescription:  "Inflige más daño.");
        var entry = SingleEntry(c);

        string preview = DeckViewerView.BuildUpgradePreview(entry);

        Assert.IsFalse(string.IsNullOrEmpty(preview), "Preview no debe estar vacío para carta con upgrade");
        StringAssert.Contains("Mejora",   preview);
        StringAssert.Contains("Strike+",  preview);
        StringAssert.Contains("0",        preview);   // coste mejorado (OverrideCost = true)
        StringAssert.Contains("Inflige",  preview);

        // El label de fila tiene el marcador "+".
        string label = DeckViewerView.BuildRowLabel(entry);
        StringAssert.EndsWith("+", label.TrimEnd());
    }

    // ────────────────────────────────────────
    // TEST 10 — Preview de mejora dual PER-LADO
    // ────────────────────────────────────────

    [Test]
    public void BuildUpgradePreview_Dual_PerSide_SideAUpgradedSideBUnchanged()
    {
        var sideA = MakeCard("a", "BladeSA", 2, CardType.Attack);
        sideA.Upgrade.SetTestData(
            overrideCost:         false,
            upgradedCost:         0,
            upgradedEffects:      null,
            upgradedName:         "BladeSA+",
            upgradedDescription:  "Más filosa.");

        var sideB = MakeCard("b", "BladeSB", 1, CardType.Skill);
        // SideB NO tiene upgrade configurado.

        var entry = DualEntry(sideA, sideB);

        // CanUpgrade() debe ser true (aHas=true aunque bHas=false).
        Assert.IsTrue(entry.CanUpgrade());

        // El label termina en "+".
        string label = DeckViewerView.BuildRowLabel(entry);
        StringAssert.EndsWith("+", label.TrimEnd());

        // El preview contiene los valores nuevos de A Y los valores base de B (sin cambios).
        string preview = DeckViewerView.BuildUpgradePreview(entry);
        StringAssert.Contains("BladeSA+", preview, "Debe mostrar nombre mejorado de A");
        StringAssert.Contains("Más filosa", preview, "Debe mostrar descripción mejorada de A");
        StringAssert.Contains("BladeSB",  preview, "Debe mostrar nombre base de B (sin cambios)");
    }

    // ────────────────────────────────────────
    // TEST 11 — Sin mejora → preview vacío, sin marcador
    // ────────────────────────────────────────

    [Test]
    public void BuildUpgradePreview_NoUpgrade_EmptyAndNoMarker()
    {
        var c = MakeCard("u", "Plain", 1, CardType.Attack);
        // No se configura ningún upgrade → HasUpgrade == false.
        var entry = SingleEntry(c);

        string preview = DeckViewerView.BuildUpgradePreview(entry);
        string label   = DeckViewerView.BuildRowLabel(entry);

        Assert.IsEmpty(preview, "Sin upgrade definido el preview debe ser vacío");
        Assert.IsFalse(label.Contains("★") || label.Contains("+"),
            "Sin upgrade no debe haber marcador en el label");
    }

    // ────────────────────────────────────────
    // TEST 12 — Ya mejorada → marcador ★, tooltip "Mejorada"
    // ────────────────────────────────────────

    [Test]
    public void BuildUpgradePreview_AlreadyUpgraded_StarMarkerAndMejoradaText()
    {
        var c = MakeCard("m", "Enhanced", 1, CardType.Attack);
        c.Upgrade.SetTestData(
            overrideCost:        false,
            upgradedCost:        0,
            upgradedEffects:     null,
            upgradedName:        "Enhanced+",
            upgradedDescription: "Mucho mejor.");

        var entry = SingleEntry(c);
        entry.ApplyUpgrade(); // Aplica la mejora → IsUpgraded = true

        string preview = DeckViewerView.BuildUpgradePreview(entry);
        string label   = DeckViewerView.BuildRowLabel(entry);

        StringAssert.Contains("★",       preview);
        StringAssert.Contains("Mejorada", preview);
        // El label lleva el marcador ★ (no "+").
        StringAssert.Contains("★", label);
        Assert.IsFalse(label.Contains(" +"), "Una carta ya mejorada no debe mostrar el marcador '+'");
    }

    // ────────────────────────────────────────
    // TEST 13 — BuildTooltip dual: ambos lados con nombre/descripción + preview anexado
    // ────────────────────────────────────────

    [Test]
    public void BuildTooltip_Dual_ContainsBothSidesAndAppendedPreview()
    {
        var sideA = MakeCard("a", "BladeA", 2, CardType.Attack, ElementType.Azul);
        sideA.Upgrade.SetTestData(false, 0, null, "BladeA+", "A más filosa.");
        var sideB = MakeCard("b", "BladeB", 1, CardType.Skill, ElementType.Morado);
        var entry = DualEntry(sideA, sideB);

        string tooltip = DeckViewerView.BuildTooltip(entry);

        // Ambos lados presentes con sus nombres y descripciones base.
        StringAssert.Contains("BladeA", tooltip);
        StringAssert.Contains("BladeB", tooltip);
        StringAssert.Contains("Desc BladeA", tooltip);
        StringAssert.Contains("Desc BladeB", tooltip);
        // Etiquetas de lado A/B.
        StringAssert.Contains("[A]", tooltip);
        StringAssert.Contains("[B]", tooltip);
        // El bloque de preview de mejora queda anexado al final (per-lado: A mejora, B no).
        StringAssert.Contains("Mejora", tooltip);
        StringAssert.Contains("BladeA+", tooltip);
    }

    // ────────────────────────────────────────
    // TEST 14 — BuildTooltip con un lado null no crashea y rinde "[A] ?"
    // ────────────────────────────────────────

    [Test]
    public void BuildTooltip_NullSide_DoesNotThrowAndRendersPlaceholder()
    {
        var sideB = MakeCard("b", "SoloB", 1, CardType.Skill);
        var entry = DualEntry(null, sideB);

        string tooltip = null;
        Assert.DoesNotThrow(() => tooltip = DeckViewerView.BuildTooltip(entry));

        // El lado A null se renderiza como "[A] ?"; el B válido muestra su nombre.
        StringAssert.Contains("[A] ?", tooltip);
        StringAssert.Contains("SoloB", tooltip);
    }

    // ────────────────────────────────────────
    // TEST 15 — BuildTooltip de carta ya mejorada contiene "Mejorada"
    // ────────────────────────────────────────

    [Test]
    public void BuildTooltip_AlreadyUpgraded_ContainsMejorada()
    {
        var c = MakeCard("m", "Enhanced", 1, CardType.Attack);
        c.Upgrade.SetTestData(false, 0, null, "Enhanced+", "Mucho mejor.");
        var entry = SingleEntry(c);
        entry.ApplyUpgrade();

        string tooltip = DeckViewerView.BuildTooltip(entry);

        // No muestra el "se convertiría en…", solo la marca de ya-mejorada.
        StringAssert.Contains("Mejorada", tooltip);
        StringAssert.Contains("Enhanced", tooltip);
    }

    // ────────────────────────────────────────
    // TEST 16-18 — CardDisplay.RewardToken (previsualización de afinidad)
    // ────────────────────────────────────────

    private static CardDefinition MakeAffineCard(string id, string name)
    {
        var c = ScriptableObject.CreateInstance<CardDefinition>();
        c.SetDebugData(id, name, $"Desc {name}", 1, CardType.Attack, CardRarity.Common, CardTarget.SingleEnemy,
                       new List<string>(), new List<EffectRef>(), ElementType.None, null, true); // newAffinity = true
        return c;
    }

    [Test]
    public void RewardToken_AffineSingle_PreviewsBothWorldTypes()
    {
        // Una afín (None hasta resolverse) debe mostrar los DOS tipos del jugador,
        // para distinguirse de una neutra del mismo nombre.
        var entry = SingleEntry(MakeAffineCard("s", "Golpe"));

        string token = CardDisplay.RewardToken(entry, ElementType.Amarillo, ElementType.Morado);

        StringAssert.Contains("Golpe", token);
        StringAssert.Contains(" / ", token); // ambos lados
        Assert.AreNotEqual("Golpe", token, "Una afín no debe verse igual que una neutra del mismo nombre.");
    }

    [Test]
    public void RewardToken_NeutralSingle_FallsBackToPlainName()
    {
        // Neutra None (affinity = false) → solo el nombre, sin token de tipo.
        var entry = SingleEntry(MakeCard("n", "Golpe", 1, CardType.Attack, ElementType.None));

        string token = CardDisplay.RewardToken(entry, ElementType.Amarillo, ElementType.Morado);

        Assert.AreEqual("Golpe", token);
    }

    [Test]
    public void RewardToken_AffineSingle_SameWorldTypes_Collapses()
    {
        // Si ambos mundos comparten tipo, no se duplica el token.
        var entry = SingleEntry(MakeAffineCard("s", "Golpe"));

        string token = CardDisplay.RewardToken(entry, ElementType.Rojo, ElementType.Rojo);

        StringAssert.DoesNotContain(" / ", token);
        StringAssert.Contains("Golpe", token);
    }
}

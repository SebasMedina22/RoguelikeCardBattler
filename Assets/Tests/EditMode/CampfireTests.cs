using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;
using RoguelikeCardBattler.Run;
using RoguelikeCardBattler.Run.Campfire;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// Tests EditMode del nodo Hoguera (Sub-PR 3C). Validamos lógica de heal,
    /// upgrade de cartas (single + dual), idempotencia del flag IsUpgraded y
    /// la inyección de opciones extra vía OnCampfireOptionsBuilt.
    /// Patrón: RunState directo sin TurnManager (todos los hooks de la Hoguera
    /// corren fuera de combate, ver CampfireOptionsBuiltHookData).
    /// </summary>
    public class CampfireTests
    {
        // ---------- Heal ----------

        [Test]
        public void Heal_AppliesThirtyPercent()
        {
            RunState rs = new RunState { PlayerMaxHP = 100, PlayerCurrentHP = 50 };
            CampfireNodeController.ApplyRest(rs, 30);
            Assert.AreEqual(80, rs.PlayerCurrentHP);
        }

        [Test]
        public void Heal_CapsAtMaxHp()
        {
            RunState rs = new RunState { PlayerMaxHP = 100, PlayerCurrentHP = 90 };
            CampfireNodeController.ApplyRest(rs, 30);
            Assert.AreEqual(100, rs.PlayerCurrentHP);
        }

        [Test]
        public void Heal_FullHp_NoChange()
        {
            RunState rs = new RunState { PlayerMaxHP = 100, PlayerCurrentHP = 100 };
            CampfireNodeController.ApplyRest(rs, 30);
            Assert.AreEqual(100, rs.PlayerCurrentHP);
        }

        // ---------- Upgrade single ----------

        [Test]
        public void SingleCard_Upgrade_BakesValues()
        {
            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.SetDebugData(
                "strike",
                "Strike",
                "Deal 6 damage.",
                1,
                CardType.Attack,
                CardRarity.Common,
                CardTarget.SingleEnemy,
                new List<string>(),
                new List<EffectRef> { new EffectRef { effectType = EffectType.Damage, value = 6, target = EffectTarget.SingleEnemy } });
            card.Upgrade.SetTestData(
                overrideCost: true,
                upgradedCost: 0,
                upgradedEffects: null,
                upgradedName: null,
                upgradedDescription: null);

            CardDeckEntry entry = CardDeckEntry.CreateSingle(card);
            Assert.IsTrue(entry.CanUpgrade());
            entry.ApplyUpgrade();

            CardDefinition active = entry.GetActiveCard(TurnManager.WorldSide.A);
            Assert.AreEqual(0, active.Cost);
            Assert.IsTrue(entry.IsUpgraded);
            Assert.AreEqual("strike_upgraded", active.Id);
        }

        // ---------- Upgrade dual ----------

        [Test]
        public void DualCard_Upgrade_AppliesToBothSides()
        {
            CardDefinition sideA = ScriptableObject.CreateInstance<CardDefinition>();
            sideA.SetDebugData("dualA", "Side A", "", 1, CardType.Attack, CardRarity.Common,
                CardTarget.SingleEnemy, new List<string>(),
                new List<EffectRef> { new EffectRef { effectType = EffectType.Damage, value = 5, target = EffectTarget.SingleEnemy } });
            List<EffectRef> upgradedA = new List<EffectRef>
            {
                new EffectRef { effectType = EffectType.Damage, value = 9, target = EffectTarget.SingleEnemy }
            };
            sideA.Upgrade.SetTestData(false, 0, upgradedA, null, null);

            CardDefinition sideB = ScriptableObject.CreateInstance<CardDefinition>();
            sideB.SetDebugData("dualB", "Side B", "", 1, CardType.Skill, CardRarity.Common,
                CardTarget.Self, new List<string>(),
                new List<EffectRef> { new EffectRef { effectType = EffectType.Block, value = 5, target = EffectTarget.Self } });
            List<EffectRef> upgradedB = new List<EffectRef>
            {
                new EffectRef { effectType = EffectType.Block, value = 9, target = EffectTarget.Self }
            };
            sideB.Upgrade.SetTestData(false, 0, upgradedB, null, null);

            DualCardDefinition dual = ScriptableObject.CreateInstance<DualCardDefinition>();
            dual.InitRuntimeSides("dual1", "Dual", sideA, sideB);

            CardDeckEntry entry = CardDeckEntry.CreateDual(dual);
            Assert.IsTrue(entry.CanUpgrade());
            entry.ApplyUpgrade();

            CardDefinition newA = entry.GetActiveCard(TurnManager.WorldSide.A);
            CardDefinition newB = entry.GetActiveCard(TurnManager.WorldSide.B);
            Assert.AreEqual(9, newA.Effects[0].value, "side A upgraded effect");
            Assert.AreEqual(9, newB.Effects[0].value, "side B upgraded effect");
            Assert.IsTrue(entry.IsUpgraded);
        }

        // ---------- Idempotencia ----------

        [Test]
        public void CanUpgrade_AlreadyUpgraded_ReturnsFalse()
        {
            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.SetDebugData("c1", "C1", "", 2, CardType.Attack, CardRarity.Common,
                CardTarget.SingleEnemy, new List<string>(),
                new List<EffectRef>());
            card.Upgrade.SetTestData(true, 1, null, null, null);

            CardDeckEntry entry = CardDeckEntry.CreateSingle(card);
            entry.ApplyUpgrade();
            CardDefinition firstClone = entry.GetActiveCard(TurnManager.WorldSide.A);
            Assert.AreEqual(1, firstClone.Cost);

            // Segundo ApplyUpgrade es no-op: CanUpgrade==false e IsUpgraded persiste.
            Assert.IsFalse(entry.CanUpgrade());
            entry.ApplyUpgrade();
            Assert.IsTrue(entry.IsUpgraded);
            Assert.AreSame(firstClone, entry.GetActiveCard(TurnManager.WorldSide.A),
                "Aplicar de nuevo no debe re-clonar la carta.");
        }

        // ---------- Sin upgrade definido ----------

        [Test]
        public void CanUpgrade_NoUpgradeDefined_ReturnsFalse()
        {
            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.SetDebugData("plain", "Plain", "", 1, CardType.Attack, CardRarity.Common,
                CardTarget.SingleEnemy, new List<string>(), new List<EffectRef>());
            // Upgrade default: HasUpgrade == false.
            CardDeckEntry entry = CardDeckEntry.CreateSingle(card);
            Assert.IsFalse(entry.CanUpgrade());
        }

        // ---------- Hook OnCampfireOptionsBuilt ----------

        private class AddOptionEffect : IRelicEffect
        {
            public void OnHook(RelicHook hook, RelicHookContext ctx)
            {
                if (hook != RelicHook.OnCampfireOptionsBuilt) return;
                if (ctx is CampfireOptionsBuiltHookData data)
                {
                    data.Options.Add(new CampfireOption(
                        "Excavar",
                        "Encuentra un Retazo común",
                        true,
                        () => { /* test no-op */ }));
                }
            }
        }

        [Test]
        public void CampfireHook_RelicCanAddOption()
        {
            RunState rs = new RunState();
            RelicHookDispatcher disp = new RelicHookDispatcher(rs);

            RelicDefinition def = ScriptableObject.CreateInstance<RelicDefinition>();
            def.DisplayName = "Pala";
            def.Effect = new AddOptionEffect();
            def.Hooks = new[] { RelicHook.OnCampfireOptionsBuilt };
            rs.Relics.Add(new RelicInstance(def, 0));

            List<CampfireOption> options = new List<CampfireOption>
            {
                new CampfireOption("Descansar", "", true, () => { }),
                new CampfireOption("Mejorar carta", "", true, () => { })
            };
            disp.Dispatch(
                RelicHook.OnCampfireOptionsBuilt,
                new CampfireOptionsBuiltHookData(rs, disp, options));

            Assert.AreEqual(3, options.Count);
            Assert.AreEqual("Excavar", options[2].Title);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// M4 bloque 4a — Afinidad de cartas (DD-022 opción A). Verifica que
    /// <see cref="AffinityResolver"/> convierte una carta afín en una dual runtime
    /// tipada por mundo, preserva cuerpo y upgrade, deja pasar neutras/duales, y que
    /// el resultado conmuta tipo por mundo sobre un <see cref="TurnManager"/> REAL sin
    /// que el protegido cambie (el daño/Estilo salen del path orgánico existente).
    /// </summary>
    public class AffinityTests : CombatTestBase
    {
        // SOs runtime creados por estos tests (cuerpos a mano + duales/variantes que
        // produce el resolver y los clones de upgrade). CombatTestBase no expone un
        // helper de afinidad ni rastrea estos SOs runtime, así que los registramos acá
        // y los destruimos en TearDown — sin esto cada Resolve/upgrade dejaría SOs
        // huérfanos en memoria (higiene de test).
        private readonly List<UnityEngine.Object> _runtimeSos = new List<UnityEngine.Object>();

        [TearDown]
        public void CleanupRuntimeSos()
        {
            foreach (UnityEngine.Object so in _runtimeSos)
            {
                if (so != null) UnityEngine.Object.DestroyImmediate(so);
            }
            _runtimeSos.Clear();
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        // Registra un SO runtime para limpieza. Devuelve el mismo objeto (fluido).
        private T TrackSO<T>(T so) where T : UnityEngine.Object
        {
            if (so != null) _runtimeSos.Add(so);
            return so;
        }

        // Registra los SOs que cuelgan de una entry dual (la dual + sus 2 lados).
        // Las entries single comparten el SO del cuerpo (ya rastreado), no crean SOs.
        private CardDeckEntry TrackEntry(CardDeckEntry entry)
        {
            if (entry?.DualCard != null)
            {
                TrackSO(entry.DualCard);
                TrackSO(entry.DualCard.SideA);
                TrackSO(entry.DualCard.SideB);
            }
            return entry;
        }

        // Resolve + registro de los SOs runtime que produce.
        private CardDeckEntry ResolveTracked(CardDeckEntry authored, ElementType typeA, ElementType typeB)
            => TrackEntry(AffinityResolver.Resolve(authored, typeA, typeB));

        private CardDefinition MakeAffineStrike(string id = "aff_strike", int damage = 6)
        {
            var card = ScriptableObject.CreateInstance<CardDefinition>();
            card.SetDebugData(
                id, "Strike", $"Deal {damage} damage to an enemy.", 1, CardType.Attack,
                CardRarity.Common, CardTarget.SingleEnemy, new List<string>(),
                new List<EffectRef> { CreateEffect(EffectType.Damage, damage, EffectTarget.SingleEnemy) },
                ElementType.None, null, /*affinity*/ true);
            return TrackSO(card);
        }

        private CardDefinition MakeAffineDefend(string id = "aff_defend", int block = 5)
        {
            var card = ScriptableObject.CreateInstance<CardDefinition>();
            card.SetDebugData(
                id, "Defend", $"Gain {block} Block.", 1, CardType.Skill,
                CardRarity.Common, CardTarget.Self, new List<string>(),
                new List<EffectRef> { CreateEffect(EffectType.Block, block, EffectTarget.Self) },
                ElementType.None, null, /*affinity*/ true);
            return TrackSO(card);
        }

        // Neutra: mismo cuerpo que la afín pero affinity=false (sigue None → 90%).
        // Se construye a mano (no vía CreateCardWithElement) para fijar cardName
        // "Strike"/"Defend" — ese helper de la base fuerza cardName = id.
        private CardDefinition MakeNeutralStrike(string id, int damage = 6)
        {
            var card = ScriptableObject.CreateInstance<CardDefinition>();
            card.SetDebugData(
                id, "Strike", $"Deal {damage} damage to an enemy.", 1, CardType.Attack,
                CardRarity.Common, CardTarget.SingleEnemy, new List<string>(),
                new List<EffectRef> { CreateEffect(EffectType.Damage, damage, EffectTarget.SingleEnemy) },
                ElementType.None, null, /*affinity*/ false);
            return TrackSO(card);
        }

        private CardDefinition MakeNeutralDefend(string id, int block = 5)
        {
            var card = ScriptableObject.CreateInstance<CardDefinition>();
            card.SetDebugData(
                id, "Defend", $"Gain {block} Block.", 1, CardType.Skill,
                CardRarity.Common, CardTarget.Self, new List<string>(),
                new List<EffectRef> { CreateEffect(EffectType.Block, block, EffectTarget.Self) },
                ElementType.None, null, /*affinity*/ false);
            return TrackSO(card);
        }

        private void SetStrikeUpgrade(CardDefinition card, int upgradedDamage)
        {
            card.Upgrade.SetTestData(false, 0,
                new List<EffectRef> { CreateEffect(EffectType.Damage, upgradedDamage, EffectTarget.SingleEnemy) },
                null, $"Deal {upgradedDamage} damage to an enemy.");
        }

        private static int FirstDamage(CardDefinition c)
        {
            foreach (EffectRef e in c.Effects) if (e.effectType == EffectType.Damage) return e.value;
            return -1;
        }

        private TurnManager CreateManager(CardDeckEntry entry, EnemyDefinition enemy)
        {
            var deck = new List<CardDeckEntry> { entry };
            var go = CreateGameObject("TurnManager");
            var manager = AddComponent<TurnManager>(go);
            manager.SetTestConfig(maxHp: 30, energy: 3, startingHand: 1, cardsPerTurnCount: 1);
            manager.SetTestData(deck, enemy);
            manager.InitializeCombat();
            return manager;
        }

        private EnemyDefinition Enemy(string id, ElementType type, int hp = 50) =>
            CreateEnemyDefinition(id, "Enemy", hp, EnemyAIPattern.Sequence, new List<EnemyMove>(), type);

        // ── Casos ──────────────────────────────────────────────────────────────────

        [Test] // 1 — Resolución afín → dual tipada por mundo
        public void Resolve_AffineSingle_BecomesDualTypedPerWorld()
        {
            CardDeckEntry resolved = ResolveTracked(
                CardDeckEntry.CreateSingle(MakeAffineStrike()), ElementType.Rojo, ElementType.Azul);

            Assert.IsNotNull(resolved.DualCard, "Una carta afín se resuelve a una entrada dual.");
            Assert.IsNull(resolved.SingleCard);
            Assert.AreEqual(ElementType.Rojo, resolved.DualCard.SideA.ElementType, "Lado A toma el tipo del Mundo A.");
            Assert.AreEqual(ElementType.Azul, resolved.DualCard.SideB.ElementType, "Lado B toma el tipo del Mundo B.");
        }

        [Test] // 2 — La variante afín preserva el cuerpo; solo cambia el tipo (y Affinity)
        public void CreateAffinityVariant_PreservesBody_OnlyTypeDiffers()
        {
            CardDefinition body = MakeAffineStrike("body_test", damage: 7);
            CardDefinition variant = TrackSO(body.CreateAffinityVariant(ElementType.Morado));

            Assert.AreEqual(body.CardName, variant.CardName);
            Assert.AreEqual(body.Cost, variant.Cost);
            Assert.AreEqual(body.Type, variant.Type);
            Assert.AreEqual(body.Target, variant.Target);
            Assert.AreEqual(body.Art, variant.Art);
            Assert.AreEqual(body.Effects.Count, variant.Effects.Count);
            Assert.AreEqual(FirstDamage(body), FirstDamage(variant), "El payload de efectos se conserva.");
            Assert.AreEqual(ElementType.Morado, variant.ElementType, "Solo difiere el tipo elemental.");
            Assert.IsFalse(variant.Affinity, "La variante queda resuelta (Affinity=false).");
        }

        [Test] // 3 — La dual afín resuelta sigue siendo mejorable; mejora ambos lados
        public void Resolve_PreservesUpgrade_BothSidesUpgrade()
        {
            CardDefinition affine = MakeAffineStrike("upg_strike", damage: 6);
            SetStrikeUpgrade(affine, 9);

            CardDeckEntry resolved = ResolveTracked(
                CardDeckEntry.CreateSingle(affine), ElementType.Rojo, ElementType.Azul);

            Assert.IsTrue(resolved.CanUpgrade(), "La dual afín conserva el payload de upgrade.");
            resolved.ApplyUpgrade();
            TrackEntry(resolved);   // ApplyUpgrade reemplaza la dual por un clon nuevo
            Assert.AreEqual(9, FirstDamage(resolved.DualCard.SideA), "Lado A mejorado.");
            Assert.AreEqual(9, FirstDamage(resolved.DualCard.SideB), "Lado B mejorado.");
        }

        [Test] // 4 — Una neutra (sin afinidad, None) pasa sin tocar (sigue single None)
        public void Resolve_NeutralSingle_StaysSingleNone()
        {
            CardDeckEntry resolved = ResolveTracked(
                CreateSingleCardEntry(MakeNeutralStrike("neutral_strike")), ElementType.Rojo, ElementType.Azul);

            Assert.IsNotNull(resolved.SingleCard, "Una neutra no se convierte en dual.");
            Assert.IsNull(resolved.DualCard);
            Assert.AreEqual(ElementType.None, resolved.SingleCard.ElementType);
        }

        [Test] // 5 — Integración con CardDeckEntry: GetActiveCard conmuta tipo por mundo
        public void Resolve_GetActiveCard_SwitchesTypePerWorld()
        {
            CardDeckEntry resolved = ResolveTracked(
                CardDeckEntry.CreateSingle(MakeAffineStrike()), ElementType.Negro, ElementType.Blanco);

            Assert.AreEqual(ElementType.Negro, resolved.GetActiveCard(TurnManager.WorldSide.A).ElementType);
            Assert.AreEqual(ElementType.Blanco, resolved.GetActiveCard(TurnManager.WorldSide.B).ElementType);
        }

        [Test] // 6 — Composición de mazo completo: 9 entradas, 5 Strike / 4 Defend con duales/singles esperados
        public void ResolveDeck_StarterComposition_ProducesGddShape()
        {
            var authored = new List<CardDeckEntry>();
            for (int i = 0; i < 3; i++) authored.Add(CardDeckEntry.CreateSingle(MakeAffineStrike("sa" + i)));   // 3 Strike afín
            for (int i = 0; i < 2; i++) authored.Add(CreateSingleCardEntry(MakeNeutralStrike("sn" + i)));        // 2 Strike neutra
            for (int i = 0; i < 2; i++) authored.Add(CardDeckEntry.CreateSingle(MakeAffineDefend("da" + i)));    // 2 Defend afín
            for (int i = 0; i < 2; i++) authored.Add(CreateSingleCardEntry(MakeNeutralDefend("dn" + i)));        // 2 Defend neutra

            List<CardDeckEntry> resolved = AffinityResolver.ResolveDeck(authored, ElementType.Rojo, ElementType.Azul);
            foreach (CardDeckEntry e in resolved) TrackEntry(e);

            Assert.AreEqual(9, resolved.Count, "9 entradas single autoradas (la 10ª es la dual drafteada).");
            Assert.AreEqual(5, resolved.Count(e => RepName(e) == "Strike"), "5 Strike.");
            Assert.AreEqual(4, resolved.Count(e => RepName(e) == "Defend"), "4 Defend.");
            Assert.AreEqual(3, resolved.Count(e => RepName(e) == "Strike" && e.DualCard != null), "3 Strike afín → dual.");
            Assert.AreEqual(2, resolved.Count(e => RepName(e) == "Strike" && e.SingleCard != null), "2 Strike neutra → single.");
            Assert.AreEqual(2, resolved.Count(e => RepName(e) == "Defend" && e.DualCard != null), "2 Defend afín → dual.");
            Assert.AreEqual(2, resolved.Count(e => RepName(e) == "Defend" && e.SingleCard != null), "2 Defend neutra → single.");
        }

        [Test] // 7 — Sobre un TurnManager real: afín en Mundo A = efectividad; neutra = 90% sin Estilo
        public void Resolve_OnRealTurnManager_AffineSuperEffective_NeutralFlat()
        {
            // Afín Strike (base 6) en Mundo A con tipo Rojo, enemigo Azul → Rojo es
            // SuperEficaz vs Azul → 6 × 1.5 = 9 daño y +1 carga de Estilo.
            CardDeckEntry affineEntry = ResolveTracked(
                CardDeckEntry.CreateSingle(MakeAffineStrike("tm_affine", 6)), ElementType.Rojo, ElementType.Azul);
            TurnManager mAff = CreateManager(affineEntry, Enemy("azul_a", ElementType.Azul));
            mAff.SetCurrentWorldForTest(TurnManager.WorldSide.A);
            int hpBeforeAff = mAff.EnemyHP;
            mAff.PlayCard(mAff.PlayerHand[0]);
            Assert.AreEqual(hpBeforeAff - 9, mAff.EnemyHP, "Afín en Mundo A vs SuperEficaz = base×1.5.");
            Assert.AreEqual(1, mAff.StyleCharges, "Un SuperEficaz otorga 1 carga de Estilo.");

            // Neutra Strike (base 6) → None → round(6 × 0.9) = 5 daño, Estilo sin cambios.
            CardDeckEntry neutralEntry = ResolveTracked(
                CreateSingleCardEntry(MakeNeutralStrike("tm_neutral", 6)), ElementType.Rojo, ElementType.Azul);
            TurnManager mNeu = CreateManager(neutralEntry, Enemy("azul_b", ElementType.Azul));
            mNeu.SetCurrentWorldForTest(TurnManager.WorldSide.A);
            int hpBeforeNeu = mNeu.EnemyHP;
            mNeu.PlayCard(mNeu.PlayerHand[0]);
            Assert.AreEqual(hpBeforeNeu - 5, mNeu.EnemyHP, "Neutra = 90% del daño base (DD-002).");
            Assert.AreEqual(0, mNeu.StyleCharges, "La neutra no pasa por la tabla → no mueve Estilo.");
        }

        [Test] // 8 — Round-trip del flag: el clon de upgrade lo preserva; la variante lo resuelve a false
        public void AffinityFlag_RoundTrip()
        {
            CardDefinition affine = MakeAffineStrike("rt_strike", 6);
            SetStrikeUpgrade(affine, 9);

            CardDefinition upgraded = TrackSO(affine.CreateUpgradedClone());
            Assert.IsTrue(upgraded.Affinity, "CreateUpgradedClone preserva el flag de afinidad.");

            CardDefinition variant = TrackSO(affine.CreateAffinityVariant(ElementType.Rojo));
            Assert.IsFalse(variant.Affinity, "CreateAffinityVariant resuelve el flag a false.");
        }

        [Test] // 9 — La MISMA carta afín conmuta su tipo por mundo a través del TurnManager real
        public void Resolve_SameAffineCard_SwitchesTypeByWorld_OnRealTurnManager()
        {
            // Afín base 8, A=Rojo / B=Azul. Una sola dual resuelta, clonada en dos managers.
            CardDeckEntry resolved = ResolveTracked(
                CardDeckEntry.CreateSingle(MakeAffineStrike("wb", 8)), ElementType.Rojo, ElementType.Azul);

            // Mundo A → tipo Rojo, enemigo Azul: Rojo→Azul SuperEficaz → 8×1.5 = 12, +1 Estilo.
            TurnManager mA = CreateManager(resolved.Clone(), Enemy("e_a", ElementType.Azul));
            mA.SetCurrentWorldForTest(TurnManager.WorldSide.A);
            int hpA = mA.EnemyHP;
            mA.PlayCard(mA.PlayerHand[0]);
            Assert.AreEqual(hpA - 12, mA.EnemyHP, "En Mundo A usa el tipo A (Rojo, SuperEficaz vs Azul).");
            Assert.AreEqual(1, mA.StyleCharges);

            // Mundo B → tipo Azul, enemigo Azul: Azul→Azul mismo tipo = Neutro → 8×1.0 = 8, sin Estilo.
            TurnManager mB = CreateManager(resolved.Clone(), Enemy("e_b", ElementType.Azul));
            mB.SetCurrentWorldForTest(TurnManager.WorldSide.B);
            int hpB = mB.EnemyHP;
            mB.PlayCard(mB.PlayerHand[0]);
            Assert.AreEqual(hpB - 8, mB.EnemyHP, "En Mundo B usa el tipo B (Azul, Neutro vs Azul) — conmutó en vivo.");
            Assert.AreEqual(0, mB.StyleCharges, "Neutro no otorga Estilo → confirma que el tipo cambió con el mundo.");
        }

        [Test] // 10 — Una afín YA MEJORADA juega su daño mejorado en combate
        public void Resolve_UpgradedAffine_PlaysUpgradedDamage_InCombat()
        {
            CardDefinition affine = MakeAffineStrike("upg_combat", damage: 6);
            SetStrikeUpgrade(affine, 9);
            CardDeckEntry resolved = ResolveTracked(
                CardDeckEntry.CreateSingle(affine), ElementType.Rojo, ElementType.Azul);
            resolved.ApplyUpgrade();
            TrackEntry(resolved);

            // Mundo A → tipo Rojo, enemigo Morado: Rojo→Morado Neutro → daño mejorado 9 × 1.0 = 9.
            TurnManager m = CreateManager(resolved, Enemy("morado", ElementType.Morado));
            m.SetCurrentWorldForTest(TurnManager.WorldSide.A);
            int hp = m.EnemyHP;
            m.PlayCard(m.PlayerHand[0]);
            Assert.AreEqual(hp - 9, m.EnemyHP, "La afín mejorada inflige su daño mejorado (9) en combate.");
        }

        // Nombre de la carta representante de una entry (single → su carta; dual → SideA).
        private static string RepName(CardDeckEntry entry) =>
            CardDisplay.RepresentativeCard(entry)?.CardName;
    }
}

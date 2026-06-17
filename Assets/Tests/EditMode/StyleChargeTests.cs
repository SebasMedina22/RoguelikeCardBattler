using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Enemies;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Gameplay.Relics.Effects;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// Contrato del Contador de Estilo (golden rule §4) que la cirugía pre-M5 NO debe
    /// romper (auditoría 2026-06, SUB-PR 2):
    /// - T3: semántica PRE-block (D-A). Un golpe SuperEficaz otorga carga aunque el
    ///   enemigo lo bloquee al 100% — el cargo se otorga en
    ///   ApplyPlayerToEnemyEffectiveness cuando finalAmount &gt; 0, ANTES de que
    ///   DamageAction aplique el bloqueo. Hoy PASA (congela comportamiento).
    /// - T4: InitializeCombat resetea el estado de Estilo combat-local; los Counters
    ///   de Retazos (RunState-level) NO se resetean → sangran (R-6, diferido al backlog).
    /// - T7: invariante de cap a 5 (D-B). El fix vive en TurnManager.IncrementStyleCharges;
    ///   T7-B FALLA sin el fix (las cargas crecen &gt;5 con un bonus pendiente).
    /// </summary>
    public class StyleChargeTests : CombatTestBase
    {
        // Assets SO creados localmente (los de la base se limpian en su propio TearDown).
        private readonly List<UnityEngine.Object> _localAssets = new List<UnityEngine.Object>();

        [TearDown]
        public void CleanupLocalAssets()
        {
            foreach (UnityEngine.Object o in _localAssets)
            {
                if (o != null) UnityEngine.Object.DestroyImmediate(o);
            }
            _localAssets.Clear();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private TurnManager CreateManager(List<CardDeckEntry> deck, EnemyDefinition enemy,
            int playerHp = 30, int energy = 3, int startingHand = 1, int cardsPerTurn = 1)
        {
            GameObject go = CreateGameObject("TurnManager");
            TurnManager manager = AddComponent<TurnManager>(go);
            manager.SetTestConfig(playerHp, energy, startingHand, cardsPerTurn);
            manager.SetTestData(deck, enemy);
            return manager;
        }

        private TurnManager CreateBareManager()
        {
            CardDefinition card = CreateCardWithElement("filler", CardType.Attack, CardTarget.SingleEnemy,
                cost: 0, ElementType.None, CreateEffect(EffectType.Damage, 0, EffectTarget.SingleEnemy));
            var deck = new List<CardDeckEntry> { CreateSingleCardEntry(card) };
            EnemyDefinition enemy = CreateEnemyDefinition("dummy", "Dummy", 50,
                EnemyAIPattern.Sequence, new List<EnemyMove>(), ElementType.None);
            return CreateManager(deck, enemy);
        }

        // Enemigo con bloqueo inicial (BaseBlock) — necesario para el "bloqueo total" de T3.
        // CombatTestBase.CreateEnemyDefinition hardcodea baseBlock=0, por eso el helper local.
        private EnemyDefinition CreateBlockerEnemy(string id, int maxHp, int baseBlock, ElementType type)
        {
            EnemyDefinition enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            enemy.SetDebugData(id, id, maxHp, baseBlock, EnemyAIPattern.Sequence,
                null, null, 1f, null, type);
            _localAssets.Add(enemy);
            return enemy;
        }

        // Inscribe un efecto en un RunState dedicado con su dispatcher. El dispatcher
        // setea CurrentRelic (internal set) → los hooks se despachan por él, no por
        // OnHook directo. Espejo de RelicEffectsTests.Setup.
        private static (RunState rs, RelicHookDispatcher disp) SetupRelic(IRelicEffect effect, params RelicHook[] hooks)
        {
            RelicDefinition def = ScriptableObject.CreateInstance<RelicDefinition>();
            def.DisplayName = effect.GetType().Name;
            def.Effect = effect;
            def.Hooks = hooks;
            RunState rs = new RunState();
            RelicHookDispatcher disp = new RelicHookDispatcher(rs);
            rs.Relics.Add(new RelicInstance(def, 0));
            return (rs, disp);
        }

        // ── T3: PRE-block — un golpe SuperEficaz bloqueado al 100% igual da carga ──
        [Test]
        public void StyleCharge_SuperEffectiveHitFullyBlocked_StillGrantsCharge()
        {
            // Carta Rojo vs enemigo Azul = SuperEficaz. El enemigo arranca con 100 de
            // bloqueo (BaseBlock) → absorbe el golpe entero (round(5*1.5)=8 ≤ 100).
            // Bajo D-A (pre-block) el cargo se otorga igual: se aplica en
            // ApplyPlayerToEnemyEffectiveness antes de que DamageAction aplique el bloqueo.
            CardDefinition card = CreateCardWithElement("strike_rojo", CardType.Attack, CardTarget.SingleEnemy,
                cost: 0, ElementType.Rojo, CreateEffect(EffectType.Damage, 5, EffectTarget.SingleEnemy));
            var deck = new List<CardDeckEntry> { CreateSingleCardEntry(card) };
            EnemyDefinition enemy = CreateBlockerEnemy("blocker_azul", maxHp: 20, baseBlock: 100, type: ElementType.Azul);

            TurnManager manager = CreateManager(deck, enemy);
            manager.InitializeCombat();
            Assert.AreEqual(0, manager.StyleCharges);
            int enemyHpBefore = manager.EnemyHP;

            manager.PlayCard(manager.PlayerHand[0]);

            Assert.AreEqual(1, manager.StyleCharges,
                "Pre-block (D-A): el golpe SuperEficaz otorga carga aunque se bloquee al 100%.");
            Assert.AreEqual(enemyHpBefore, manager.EnemyHP,
                "El bloqueo de 100 absorbe el golpe entero (HP intacto) → confirma que fue bloqueo total.");
        }

        // ── T4: re-init resetea estado de Estilo; counters de Retazos sangran (R-6) ──
        [Test]
        public void InitializeCombat_SecondCall_ResetsStyleState_RelicCountersBleed()
        {
            TurnManager manager = CreateBareManager();
            manager.InitializeCombat();
            manager.SetStyleChargesForTest(4);
            manager.SetBonusWorldSwitchesForTest(1);

            // Segunda inicialización (nuevo combate de la misma run).
            manager.InitializeCombat();

            // El estado de Estilo (combat-local) SÍ se resetea (TurnManager.InitializeCombat).
            Assert.AreEqual(0, manager.StyleCharges, "InitializeCombat resetea las cargas de Estilo.");
            Assert.AreEqual(manager.MaxWorldSwitchesPerCombat, manager.TotalAvailableWorldSwitches,
                "InitializeCombat resetea el switch bonus (no quedan switches extra del combate previo).");

            // Los Counters de Retazos viven en RunState.Relics (run-level) y NADA en
            // InitializeCombat los toca → sangran entre combates. R-6: conocido y
            // diferido al backlog (plan pre-M4 §6). Este assert congela ese contrato.
            var fx = new RelicTurnEnergyEveryNEffect { Period = 3, Amount = 1 };
            var (rs, disp) = SetupRelic(fx, RelicHook.OnPlayerTurnStart);
            RelicInstance inst = rs.Relics[0];
            disp.Dispatch(RelicHook.OnPlayerTurnStart, new PlayerTurnStartHookData(rs, manager, disp, TurnManager.WorldSide.A));
            disp.Dispatch(RelicHook.OnPlayerTurnStart, new PlayerTurnStartHookData(rs, manager, disp, TurnManager.WorldSide.A));
            int counterBefore = inst.Counters["r-6:turns"]; // 2 (Period=3 aún no resetea)

            manager.InitializeCombat();

            Assert.AreEqual(counterBefore, inst.Counters["r-6:turns"],
                "R-6: InitializeCombat NO resetea los Counters de Retazos (RunState-level) → sangran entre combates.");
        }

        // ── T7-A: overshoot desde estado limpio → 1 bonus + reset (sin leftover) ──
        [Test]
        public void GrantStyleCharge_OvershootFromCleanState_GrantsSingleBonusAndResets()
        {
            TurnManager manager = CreateBareManager();
            manager.InitializeCombat();
            manager.SetStyleChargesForTest(4);

            // Un Retazo otorga +3 de una → 7, que supera 5 SIN bonus pendiente: se
            // otorga 1 switch bonus y las cargas resetean a 0 (golden rule §4).
            var (rs, disp) = SetupRelic(new RelicSwitchStyleChargeEffect { Amount = 3 }, RelicHook.OnWorldSwitch);
            disp.Dispatch(RelicHook.OnWorldSwitch,
                new WorldSwitchHookData(rs, manager, disp, TurnManager.WorldSide.A, TurnManager.WorldSide.B));

            Assert.AreEqual(0, manager.StyleCharges, "Tras superar 5 sin bonus pendiente, las cargas resetean a 0.");
            Assert.AreEqual(2, manager.TotalAvailableWorldSwitches, "Se otorga exactamente 1 switch bonus (base 1 + 1).");
        }

        // ── T7-B: overflow con bonus ya pendiente → cap a 5 (D-B). FALLA sin el fix ──
        [Test]
        public void GrantStyleCharge_OverflowWhileBonusPending_CapsAtFive()
        {
            TurnManager manager = CreateBareManager();
            manager.InitializeCombat();
            // Estado realista: el jugador ya llegó a 5 y tiene 1 switch bonus sin usar.
            manager.SetBonusWorldSwitchesForTest(1);
            manager.SetStyleChargesForTest(0);

            // Con un bonus pendiente, la rama de reset de IncrementStyleCharges no dispara
            // (_bonusWorldSwitches != 0). Sin el cap (D-B) las cargas crecerían sin techo
            // vía Retazos. Otorgamos 6 → debe quedar topado en 5, nunca 6.
            var (rs, disp) = SetupRelic(new RelicSwitchStyleChargeEffect { Amount = 1 }, RelicHook.OnWorldSwitch);
            for (int i = 0; i < 6; i++)
            {
                disp.Dispatch(RelicHook.OnWorldSwitch,
                    new WorldSwitchHookData(rs, manager, disp, TurnManager.WorldSide.A, TurnManager.WorldSide.B));
            }

            Assert.LessOrEqual(manager.StyleCharges, 5, "El Contador de Estilo nunca debe exceder 5 (D-B).");
            Assert.AreEqual(5, manager.StyleCharges, "6 cargas con bonus pendiente quedan topadas en 5.");
            Assert.AreEqual(2, manager.TotalAvailableWorldSwitches, "El bonus no se acumula (sigue 1, total 2).");
        }
    }
}

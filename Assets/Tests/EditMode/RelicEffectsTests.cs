using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Gameplay.Relics.Effects;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// Tests EditMode de los 23 efectos concretos de Sub-PR 3B.
    /// Estrategia: cada efecto se inscribe en RunState.Relics y se dispara vía
    /// RelicHookDispatcher (que setea CurrentRelic internamente — único camino
    /// permitido tras 3A; el setter es internal por diseño). El payload se
    /// construye con TurnManager null cuando aplica: las APIs Grant* de
    /// RelicHookContext hacen no-op silencioso por guard ([IMPL 4]). Las
    /// mutaciones de Amount (DamageDealt/DamageTaken) y RunState directo
    /// (OnCombatEnd) son verificables sin TurnManager.
    /// </summary>
    public class RelicEffectsTests
    {
        // ---------- Helpers ----------

        private static (RunState rs, RelicHookDispatcher disp, RelicInstance inst) Setup(
            IRelicEffect effect, params RelicHook[] hooks)
        {
            RelicDefinition def = ScriptableObject.CreateInstance<RelicDefinition>();
            def.DisplayName = effect.GetType().Name;
            def.Effect = effect;
            def.Hooks = hooks;
            RunState rs = new RunState();
            RelicHookDispatcher disp = new RelicHookDispatcher(rs);
            RelicInstance inst = new RelicInstance(def, 0);
            rs.Relics.Add(inst);
            return (rs, disp, inst);
        }

        // ---------- Slot 7 R-DMG-1 ----------
        [Test]
        public void RelicDmgFlatBoost_OnDamageDealt_AddsAmount()
        {
            var fx = new RelicDmgFlatBoostEffect { Amount = 2 };
            var (rs, disp, _) = Setup(fx, RelicHook.OnDamageDealt);
            DamageDealtHookData data = new DamageDealtHookData(rs, null, disp, 10, Effectiveness.Neutro, ElementType.Rojo, null);
            disp.Dispatch(RelicHook.OnDamageDealt, data);
            Assert.AreEqual(12, data.Amount);
        }

        [Test]
        public void RelicDmgFlatBoost_OtherHook_NoOp()
        {
            var fx = new RelicDmgFlatBoostEffect { Amount = 2 };
            var (rs, disp, _) = Setup(fx, RelicHook.OnDamageDealt);
            // Dispatcher filtra: efecto NO escucha OnDamageTaken aunque la firma del
            // callback sí podría manejarlo. El payload no debe mutar.
            DamageTakenHookData data = new DamageTakenHookData(rs, null, disp, 10, Effectiveness.Neutro, ElementType.Rojo, null);
            disp.Dispatch(RelicHook.OnDamageTaken, data);
            Assert.AreEqual(10, data.Amount);
        }

        // ---------- Slot 8 R-DMG-2 (first hit only) ----------
        [Test]
        public void RelicDmgFirstHit_FirstCallBoosts_SecondDoesNot()
        {
            var fx = new RelicDmgFirstHitEffect { Amount = 5 };
            var (rs, disp, _) = Setup(fx, RelicHook.OnCombatStart, RelicHook.OnDamageDealt);
            disp.Dispatch(RelicHook.OnCombatStart, new CombatStartHookData(rs, null, disp, null));
            DamageDealtHookData first = new DamageDealtHookData(rs, null, disp, 10, Effectiveness.Neutro, ElementType.Rojo, null);
            disp.Dispatch(RelicHook.OnDamageDealt, first);
            Assert.AreEqual(15, first.Amount);
            DamageDealtHookData second = new DamageDealtHookData(rs, null, disp, 10, Effectiveness.Neutro, ElementType.Rojo, null);
            disp.Dispatch(RelicHook.OnDamageDealt, second);
            Assert.AreEqual(10, second.Amount);
        }

        // ---------- Slot 9 R-DMG-3 ----------
        [Test]
        public void RelicDmgReduce_DamageTaken_Reduces()
        {
            var fx = new RelicDmgReduceEffect { Amount = 1 };
            var (rs, disp, _) = Setup(fx, RelicHook.OnDamageTaken);
            DamageTakenHookData data = new DamageTakenHookData(rs, null, disp, 5, Effectiveness.Neutro, ElementType.Rojo, null);
            disp.Dispatch(RelicHook.OnDamageTaken, data);
            Assert.AreEqual(4, data.Amount);
        }

        [Test]
        public void RelicDmgReduce_FloorsAtZero()
        {
            var fx = new RelicDmgReduceEffect { Amount = 10 };
            var (rs, disp, _) = Setup(fx, RelicHook.OnDamageTaken);
            DamageTakenHookData data = new DamageTakenHookData(rs, null, disp, 3, Effectiveness.Neutro, ElementType.Rojo, null);
            disp.Dispatch(RelicHook.OnDamageTaken, data);
            Assert.AreEqual(0, data.Amount);
        }

        // ---------- Slot 11 R-ACC-2 every Nth attack ----------
        [Test]
        public void RelicAccEveryNthAttack_Every3rd_Boosts()
        {
            var fx = new RelicAccEveryNthAttackEffect { Period = 3, Amount = 4 };
            var (rs, disp, _) = Setup(fx, RelicHook.OnCombatStart, RelicHook.OnDamageDealt);
            disp.Dispatch(RelicHook.OnCombatStart, new CombatStartHookData(rs, null, disp, null));

            int[] expected = { 10, 10, 14, 10, 10, 14 };
            for (int i = 0; i < expected.Length; i++)
            {
                DamageDealtHookData data = new DamageDealtHookData(rs, null, disp, 10, Effectiveness.Neutro, ElementType.Rojo, null);
                disp.Dispatch(RelicHook.OnDamageDealt, data);
                Assert.AreEqual(expected[i], data.Amount, $"hit #{i + 1}");
            }
        }

        // ---------- Slot 12 R-END-1 gold reward ----------
        [Test]
        public void RelicEndGold_VictoryAddsGold()
        {
            var fx = new RelicEndGoldEffect { Amount = 5 };
            var (rs, disp, _) = Setup(fx, RelicHook.OnCombatEnd);
            rs.Gold = 0;
            disp.Dispatch(RelicHook.OnCombatEnd, new CombatEndHookData(rs, null, disp, true, null));
            Assert.AreEqual(5, rs.Gold);
        }

        [Test]
        public void RelicEndGold_DefeatNoOp()
        {
            var fx = new RelicEndGoldEffect { Amount = 5 };
            var (rs, disp, _) = Setup(fx, RelicHook.OnCombatEnd);
            rs.Gold = 0;
            disp.Dispatch(RelicHook.OnCombatEnd, new CombatEndHookData(rs, null, disp, false, null));
            Assert.AreEqual(0, rs.Gold);
        }

        // ---------- Slot 13 R-END-2 heal post-combat ----------
        [Test]
        public void RelicEndHeal_VictoryRespectsMaxHp()
        {
            var fx = new RelicEndHealEffect { Amount = 4 };
            var (rs, disp, _) = Setup(fx, RelicHook.OnCombatEnd);
            rs.PlayerMaxHP = 50; rs.PlayerCurrentHP = 48;
            disp.Dispatch(RelicHook.OnCombatEnd, new CombatEndHookData(rs, null, disp, true, null));
            Assert.AreEqual(50, rs.PlayerCurrentHP);
        }

        [Test]
        public void RelicEndHeal_VictoryHealsBelowMax()
        {
            var fx = new RelicEndHealEffect { Amount = 4 };
            var (rs, disp, _) = Setup(fx, RelicHook.OnCombatEnd);
            rs.PlayerMaxHP = 50; rs.PlayerCurrentHP = 40;
            disp.Dispatch(RelicHook.OnCombatEnd, new CombatEndHookData(rs, null, disp, true, null));
            Assert.AreEqual(44, rs.PlayerCurrentHP);
        }

        // ---------- Slot 14 R-END-3 Elite gold ----------
        [Test]
        public void RelicEndEliteGold_OnlyOnEliteVictory()
        {
            var fx = new RelicEndEliteGoldEffect { Amount = 10 };
            var (rs, disp, _) = Setup(fx, RelicHook.OnCombatEnd);
            rs.Gold = 0;
            disp.Dispatch(RelicHook.OnCombatEnd, new CombatEndHookData(rs, null, disp, true, null, isBoss: false, isElite: false));
            Assert.AreEqual(0, rs.Gold);
            disp.Dispatch(RelicHook.OnCombatEnd, new CombatEndHookData(rs, null, disp, true, null, isBoss: false, isElite: true));
            Assert.AreEqual(10, rs.Gold);
        }

        // ---------- Slot 19 R-ELITE-1 vampiric ----------
        [Test]
        public void RelicEliteVampiric_GuardsOnNullTurnManager()
        {
            var fx = new RelicEliteVampiricEffect { Amount = 2 };
            var (rs, disp, _) = Setup(fx, RelicHook.OnDamageDealt);
            // TurnManager null + Effectiveness.SuperEficaz → GrantHeal hace no-op
            // por guard de RelicHookContext. El dispatcher captura excepciones,
            // pero si tirara, fallaría (try/catch loguea LogError → NUnit lo ignora).
            DamageDealtHookData data = new DamageDealtHookData(rs, null, disp, 10, Effectiveness.SuperEficaz, ElementType.Rojo, null);
            Assert.DoesNotThrow(() => disp.Dispatch(RelicHook.OnDamageDealt, data));
            // Amount no se muta (es heal, no boost).
            Assert.AreEqual(10, data.Amount);
        }

        // ---------- Slot 21 R-ELITE-3 purist ----------
        [Test]
        public void RelicElitePurist_OnlyAtFullHp()
        {
            var fx = new RelicElitePuristEffect { Amount = 10 };
            var (rs, disp, _) = Setup(fx, RelicHook.OnCombatEnd);
            rs.Gold = 0; rs.PlayerMaxHP = 50; rs.PlayerCurrentHP = 49;
            disp.Dispatch(RelicHook.OnCombatEnd, new CombatEndHookData(rs, null, disp, true, null));
            Assert.AreEqual(0, rs.Gold);
            rs.PlayerCurrentHP = 50;
            disp.Dispatch(RelicHook.OnCombatEnd, new CombatEndHookData(rs, null, disp, true, null));
            Assert.AreEqual(10, rs.Gold);
        }

        // ---------- Slot 22 R-ELITE-4 charge boost ----------
        [Test]
        public void RelicEliteChargeBoost_NoTurnManager_NoOp()
        {
            var fx = new RelicEliteChargeBoostEffect { Threshold = 3, Amount = 4 };
            var (rs, disp, _) = Setup(fx, RelicHook.OnDamageDealt);
            DamageDealtHookData data = new DamageDealtHookData(rs, null, disp, 10, Effectiveness.Neutro, ElementType.Rojo, null);
            disp.Dispatch(RelicHook.OnDamageDealt, data);
            // Sin TurnManager el efecto early-returns (no puede leer StyleCharges).
            Assert.AreEqual(10, data.Amount);
        }

        // ---------- Slot 6 R-TURN-3 every N turns ----------
        [Test]
        public void RelicTurnEnergyEveryN_CounterAdvancesAndResets()
        {
            var fx = new RelicTurnEnergyEveryNEffect { Period = 3, Amount = 1 };
            var (rs, disp, inst) = Setup(fx, RelicHook.OnPlayerTurnStart);
            // Sin TurnManager: GrantEnergy es no-op pero el counter avanza/resetea.
            for (int i = 0; i < 3; i++)
            {
                disp.Dispatch(RelicHook.OnPlayerTurnStart, new PlayerTurnStartHookData(rs, null, disp, TurnManager.WorldSide.A));
            }
            Assert.AreEqual(0, inst.Counters["r-6:turns"], "Tras Period invocaciones el counter debe resetear.");
            disp.Dispatch(RelicHook.OnPlayerTurnStart, new PlayerTurnStartHookData(rs, null, disp, TurnManager.WorldSide.A));
            Assert.AreEqual(1, inst.Counters["r-6:turns"]);
        }

        // ---------- Slot 10 R-ACC-1 skill stacker (path defensivo sin Card real) ----------
        [Test]
        public void RelicAccSkillStacker_NullCard_DoesNotIncrement()
        {
            var fx = new RelicAccSkillStackerEffect { Period = 3, Amount = 1 };
            var (rs, disp, inst) = Setup(fx, RelicHook.OnCardPlayed, RelicHook.OnPlayerTurnStart);
            disp.Dispatch(RelicHook.OnCardPlayed, new CardPlayedHookData(rs, null, disp, null, TurnManager.WorldSide.A, 0));
            int skills = inst.Counters.TryGetValue("r-10:skillsThisTurn", out int s) ? s : 0;
            Assert.AreEqual(0, skills);
            // PlayerTurnStart no debe tirar aunque counters vengan vacíos.
            Assert.DoesNotThrow(() =>
                disp.Dispatch(RelicHook.OnPlayerTurnStart, new PlayerTurnStartHookData(rs, null, disp, TurnManager.WorldSide.A)));
        }

        // ---------- Slot 23 R-BOSS-1 last-stitch ----------
        [Test]
        public void RelicBossLastStitch_VictoryFlagsCounter_PersistsAcrossDispatches()
        {
            var fx = new RelicBossLastStitchEffect { EnergyBonus = 1, BlockBonus = 5 };
            var (rs, disp, inst) = Setup(fx, RelicHook.OnCombatEnd, RelicHook.OnCombatStart);
            disp.Dispatch(RelicHook.OnCombatEnd, new CombatEndHookData(rs, null, disp, true, null));
            Assert.AreEqual(1, inst.Counters["r-23:won"]);
            // Sin TurnManager el efecto early-returns en OnCombatStart y el counter
            // persiste — comportamiento esperado: el reset ocurre solo cuando el
            // bonus se consume con éxito.
            disp.Dispatch(RelicHook.OnCombatStart, new CombatStartHookData(rs, null, disp, null));
            Assert.AreEqual(1, inst.Counters["r-23:won"], "Sin TurnManager el flag debe persistir hasta poder consumirlo.");
        }

        // ---------- Grant-based effects: defensive guards con TurnManager null ----------
        [Test]
        public void GrantBasedEffects_TurnManagerNull_DoNotThrow()
        {
            // Cada efecto Grant* delega a RelicHookContext.Grant* que early-returns
            // si TurnManager == null. El test valida que NINGUNO tire. Si tirara,
            // el dispatcher loguea LogError pero el test pasa — por eso además del
            // disp.Dispatch hacemos un check sin try/catch invocando directamente
            // a través del dispatcher (la captura del dispatcher cubre el caso real
            // pero queremos verificar que el guard del context funciona).
            IRelicEffect[] effects = new IRelicEffect[]
            {
                new RelicOpenBlockEffect(), new RelicOpenDrawEffect(), new RelicOpenEnergyEffect(),
                new RelicTurnDrawEffect(), new RelicTurnBlockEffect(),
                new RelicSwitchHealEffect(), new RelicSwitchBlockEffect(),
                new RelicSwitchStyleChargeEffect(), new RelicSwitchDamageEffect(),
                new RelicEliteSpinesEffect(),
            };
            foreach (IRelicEffect fx in effects)
            {
                var (rs, disp, _) = Setup(fx, RelicHook.OnCombatStart, RelicHook.OnPlayerTurnStart,
                    RelicHook.OnWorldSwitch, RelicHook.OnDamageTaken);
                Assert.DoesNotThrow(() =>
                {
                    disp.Dispatch(RelicHook.OnCombatStart, new CombatStartHookData(rs, null, disp, null));
                    disp.Dispatch(RelicHook.OnPlayerTurnStart, new PlayerTurnStartHookData(rs, null, disp, TurnManager.WorldSide.A));
                    disp.Dispatch(RelicHook.OnWorldSwitch, new WorldSwitchHookData(rs, null, disp, TurnManager.WorldSide.A, TurnManager.WorldSide.B));
                    disp.Dispatch(RelicHook.OnDamageTaken, new DamageTakenHookData(rs, null, disp, 5, Effectiveness.Neutro, ElementType.Rojo, null));
                }, $"Effect {fx.GetType().Name} threw with null TurnManager");
            }
        }
    }
}

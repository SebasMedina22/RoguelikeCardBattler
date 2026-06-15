using NUnit.Framework;
using UnityEngine;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// Tests del fix de sincronización de HP al cerrar combate + retry
    /// (spec fix_combat_end_hp_sync, SUB-PR 1). Cobertura:
    /// - T1 documenta H1: el paso de outcome (ApplyCombatResult) NO pisa el HP que
    ///   los Retazos OnCombatEnd dejaron en RunState. La otra mitad — TurnManager
    ///   sincronizando RunState = actor antes del dispatch — cruza el borde de
    ///   RunSession y se valida en Play (§Validación manual del spec).
    /// - T2 documenta H3: PrepareForRetry devuelve HP jugable (full) tras derrota.
    /// </summary>
    public class CombatEndSyncTests : CombatTestBase
    {
        // ---------- T1: H1 — el outcome no pisa la curación de fin de combate ----------
        [Test]
        public void ReportOutcome_AfterCombatEndHook_PreservesRelicHeal()
        {
            // Arrange: simula la base post-combate ya sincronizada por TurnManager
            // (30 de HP al cerrar + 4 de heal aplicado en el dispatch = 34). Sin
            // CombatConfig asignada, TryDropRelics es no-op.
            GameObject sessionGo = CreateGameObject("RunSession");
            RunSession session = AddComponent<RunSession>(sessionGo);
            session.State.PlayerMaxHP = 60;
            session.State.PlayerCurrentHP = 34;

            GameObject flowGo = CreateGameObject("BattleFlow");
            BattleFlowController flow = AddComponent<BattleFlowController>(flowGo);

            // Act: camino de outcome de victoria (sin sync de HP, sin carga de escena).
            flow.ApplyCombatResult(session, victory: true);

            // Assert: la curación sobrevive — el outcome no pisa el HP.
            Assert.AreEqual(34, session.State.PlayerCurrentHP,
                "ApplyCombatResult no debe re-sincronizar/pisar el HP que dejó el hook OnCombatEnd.");
            Assert.AreEqual(RunState.NodeOutcome.Victory, session.State.LastNodeOutcome);
            Assert.IsTrue(session.State.PendingReturnFromBattle);
            Assert.IsFalse(session.State.RunFailed);
        }

        // ---------- T2: H3 — retry restaura HP jugable ----------
        [Test]
        public void RunState_AfterDefeatWithZeroHp_RetryPathRestoresPlayableHp()
        {
            // Arrange: estado tras derrota (HP a 0, run marcada como fallida).
            RunState state = new RunState
            {
                PlayerMaxHP = 60,
                PlayerCurrentHP = 0,
                HasPlayerHPInitialized = true,
                RunFailed = true,
                PendingReturnFromBattle = true
            };
            state.LastNodeOutcome = RunState.NodeOutcome.Defeat;

            // Act
            state.PrepareForRetry();

            // Assert: HP full y jugable (no 1), flags de outcome limpios.
            Assert.AreEqual(60, state.PlayerCurrentHP);
            Assert.Greater(state.PlayerCurrentHP, 1);
            Assert.IsFalse(state.RunFailed);
            Assert.IsFalse(state.PendingReturnFromBattle);
            Assert.AreEqual(RunState.NodeOutcome.None, state.LastNodeOutcome);
        }
    }
}

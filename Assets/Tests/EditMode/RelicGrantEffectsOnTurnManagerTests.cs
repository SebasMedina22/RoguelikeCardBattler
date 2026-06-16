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
    /// T6 (auditoría 2026-06, SUB-PR 2): 10 casos de Retazos que usan la API Grant*
    /// (GrantBlock / GrantStyleCharge / GrantHeal / GrantEnergy / GrantDrawCards)
    /// disparados sobre un TurnManager REAL (no mock) y verificando que el estado de
    /// combate muta como se espera. Cada hook se despacha a mano (no por el flujo de
    /// combate completo): se construye el *HookData con el TurnManager real como
    /// destinatario, de modo que ctx.GrantX delega en TurnManager.RelicGrantX y la
    /// mutación real ocurre sobre el actor. Foco en la mutación de estado, no en la
    /// animación.
    ///
    /// El dispatcher es uno dedicado por test (RunState propio): no se usa RunSession
    /// porque su dispatcher se cablea en Awake, que no corre en EditMode. Lo único que
    /// importa para Grant* es que el TurnManager del payload sea el real.
    /// </summary>
    public class RelicGrantEffectsOnTurnManagerTests : CombatTestBase
    {
        // ── Infra ──────────────────────────────────────────────────────────────

        private EnemyDefinition DummyEnemy(int hp = 50)
        {
            return CreateEnemyDefinition("dummy", "Dummy", hp,
                EnemyAIPattern.Sequence, new List<EnemyMove>(), ElementType.None);
        }

        private CardDefinition NoneCard(string id, int damage = 0, int cost = 0)
        {
            return CreateCardWithElement(id, CardType.Attack, CardTarget.SingleEnemy, cost,
                ElementType.None, CreateEffect(EffectType.Damage, damage, EffectTarget.SingleEnemy));
        }

        private List<CardDeckEntry> Deck(int count)
        {
            var deck = new List<CardDeckEntry>();
            for (int i = 0; i < count; i++)
            {
                deck.Add(CreateSingleCardEntry(NoneCard("card_" + i)));
            }
            return deck;
        }

        // TurnManager real e inicializado. ConfigureCombat permite override de HP
        // (necesario para los casos de curación: arrancar por debajo del máximo).
        private TurnManager CreateManager(List<CardDeckEntry> deck, EnemyDefinition enemy,
            int maxHp = 30, int energy = 3, int startingHand = 1, int cardsPerTurn = 1, int? currentHp = null)
        {
            GameObject go = CreateGameObject("TurnManager");
            TurnManager manager = AddComponent<TurnManager>(go);
            manager.SetTestConfig(maxHp, energy, startingHand, cardsPerTurn);
            manager.ConfigureCombat(deck, enemy,
                playerCurrentHpOverride: currentHp,
                playerMaxHpOverride: maxHp,
                initializeImmediately: true);
            return manager;
        }

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

        // ── GrantBlock (×4) ──────────────────────────────────────────────────────

        [Test] // R-OPEN-1
        public void GrantBlock_OpenBlockOnCombatStart_GivesPlayerBlock()
        {
            TurnManager m = CreateManager(Deck(1), DummyEnemy());
            Assert.AreEqual(0, m.PlayerBlock);
            var (rs, disp) = SetupRelic(new RelicOpenBlockEffect { Amount = 4 }, RelicHook.OnCombatStart);
            disp.Dispatch(RelicHook.OnCombatStart, new CombatStartHookData(rs, m, disp, null));
            Assert.AreEqual(4, m.PlayerBlock);
        }

        [Test] // R-TURN-2
        public void GrantBlock_TurnBlockOnPlayerTurnStart_GivesPlayerBlock()
        {
            TurnManager m = CreateManager(Deck(1), DummyEnemy());
            var (rs, disp) = SetupRelic(new RelicTurnBlockEffect { Amount = 3 }, RelicHook.OnPlayerTurnStart);
            disp.Dispatch(RelicHook.OnPlayerTurnStart, new PlayerTurnStartHookData(rs, m, disp, TurnManager.WorldSide.A));
            Assert.AreEqual(3, m.PlayerBlock);
        }

        [Test] // R-SW-1
        public void GrantBlock_SwitchBlockOnWorldSwitch_GivesPlayerBlock()
        {
            TurnManager m = CreateManager(Deck(1), DummyEnemy());
            var (rs, disp) = SetupRelic(new RelicSwitchBlockEffect { Amount = 5 }, RelicHook.OnWorldSwitch);
            disp.Dispatch(RelicHook.OnWorldSwitch, new WorldSwitchHookData(rs, m, disp, TurnManager.WorldSide.A, TurnManager.WorldSide.B));
            Assert.AreEqual(5, m.PlayerBlock);
        }

        [Test] // R-BOSS-1
        public void GrantBlock_BossLastStitchAfterVictory_GivesBlockOnNextCombatStart()
        {
            TurnManager m = CreateManager(Deck(1), DummyEnemy());
            var (rs, disp) = SetupRelic(new RelicBossLastStitchEffect { EnergyBonus = 1, BlockBonus = 5 },
                RelicHook.OnCombatEnd, RelicHook.OnCombatStart);
            // Combate previo ganado → marca el flag interno (counter persiste en la instancia).
            disp.Dispatch(RelicHook.OnCombatEnd, new CombatEndHookData(rs, m, disp, victory: true, null));
            // Siguiente combate → consume el flag y otorga el bloqueo de remate.
            disp.Dispatch(RelicHook.OnCombatStart, new CombatStartHookData(rs, m, disp, null));
            Assert.AreEqual(5, m.PlayerBlock,
                "Last-stitch otorga bloqueo al iniciar el combate siguiente tras una victoria.");
        }

        // ── GrantStyleCharge (×1) ─────────────────────────────────────────────────

        [Test] // R-SW-2
        public void GrantStyleCharge_SwitchStyleOnWorldSwitch_AddsCharge()
        {
            TurnManager m = CreateManager(Deck(1), DummyEnemy());
            Assert.AreEqual(0, m.StyleCharges);
            var (rs, disp) = SetupRelic(new RelicSwitchStyleChargeEffect { Amount = 1 }, RelicHook.OnWorldSwitch);
            disp.Dispatch(RelicHook.OnWorldSwitch, new WorldSwitchHookData(rs, m, disp, TurnManager.WorldSide.A, TurnManager.WorldSide.B));
            Assert.AreEqual(1, m.StyleCharges);
        }

        // ── GrantHeal (×2) ────────────────────────────────────────────────────────

        [Test] // R-SW-3
        public void GrantHeal_SwitchHealOnWorldSwitch_HealsPlayer()
        {
            TurnManager m = CreateManager(Deck(1), DummyEnemy(), maxHp: 30, currentHp: 10);
            Assert.AreEqual(10, m.PlayerHP);
            var (rs, disp) = SetupRelic(new RelicSwitchHealEffect { Amount = 2 }, RelicHook.OnWorldSwitch);
            disp.Dispatch(RelicHook.OnWorldSwitch, new WorldSwitchHookData(rs, m, disp, TurnManager.WorldSide.A, TurnManager.WorldSide.B));
            Assert.AreEqual(12, m.PlayerHP);
        }

        [Test] // R-ELITE-1
        public void GrantHeal_VampiricOnSuperEffective_HealsPlayer()
        {
            TurnManager m = CreateManager(Deck(1), DummyEnemy(), maxHp: 30, currentHp: 10);
            var (rs, disp) = SetupRelic(new RelicEliteVampiricEffect { Amount = 2 }, RelicHook.OnDamageDealt);
            disp.Dispatch(RelicHook.OnDamageDealt,
                new DamageDealtHookData(rs, m, disp, 10, Effectiveness.SuperEficaz, ElementType.Rojo, m.Enemy));
            Assert.AreEqual(12, m.PlayerHP, "Vampírico cura al pegar un golpe SuperEficaz.");
        }

        // ── GrantEnergy (×1) ──────────────────────────────────────────────────────

        [Test] // R-OPEN-3
        public void GrantEnergy_OpenEnergyOnCombatStart_RestoresSpentEnergy()
        {
            // La energía arranca al máximo; se gasta 1 jugando una carta de costo 1
            // (Update no corre en EditMode → no hay auto-end-turn que resetee energía).
            var deck = new List<CardDeckEntry> { CreateSingleCardEntry(NoneCard("c", damage: 0, cost: 1)) };
            TurnManager m = CreateManager(deck, DummyEnemy(), energy: 3, startingHand: 1);
            m.PlayCard(m.PlayerHand[0]);
            int energyBefore = m.PlayerEnergy; // 2

            var (rs, disp) = SetupRelic(new RelicOpenEnergyEffect { Amount = 1 }, RelicHook.OnCombatStart);
            disp.Dispatch(RelicHook.OnCombatStart, new CombatStartHookData(rs, m, disp, null));

            Assert.AreEqual(energyBefore + 1, m.PlayerEnergy, "GrantEnergy restaura energía gastada.");
        }

        // ── GrantDrawCards (×2) ────────────────────────────────────────────────────

        [Test] // R-OPEN-2
        public void GrantDrawCards_OpenDrawOnCombatStart_AddsCardToHand()
        {
            // 3 cartas, mano inicial 1 → quedan 2 en el mazo para robar (mano < maxHandSize=7).
            TurnManager m = CreateManager(Deck(3), DummyEnemy(), startingHand: 1, cardsPerTurn: 1);
            int handBefore = m.PlayerHandCount; // 1
            var (rs, disp) = SetupRelic(new RelicOpenDrawEffect { Amount = 1 }, RelicHook.OnCombatStart);
            disp.Dispatch(RelicHook.OnCombatStart, new CombatStartHookData(rs, m, disp, null));
            Assert.AreEqual(handBefore + 1, m.PlayerHandCount);
        }

        [Test] // R-TURN-1
        public void GrantDrawCards_TurnDrawOnPlayerTurnStart_AddsCardToHand()
        {
            TurnManager m = CreateManager(Deck(3), DummyEnemy(), startingHand: 1, cardsPerTurn: 1);
            int handBefore = m.PlayerHandCount; // 1
            var (rs, disp) = SetupRelic(new RelicTurnDrawEffect { Amount = 1 }, RelicHook.OnPlayerTurnStart);
            disp.Dispatch(RelicHook.OnPlayerTurnStart, new PlayerTurnStartHookData(rs, m, disp, TurnManager.WorldSide.A));
            Assert.AreEqual(handBefore + 1, m.PlayerHandCount);
        }
    }
}

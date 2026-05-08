using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Gameplay.Relics.Hooks;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Tests.EditMode
{
    /// <summary>
    /// Tests de plomería del dispatcher de Retazos (Sub-PR 3A). Cubren los 8
    /// casos del spec en Docs/dev/specs/m3_hooks_spec.md §"Casos de prueba".
    /// Tests de integración con TurnManager se cubren en 3B con Retazos reales.
    /// </summary>
    public class RelicHookDispatcherTests
    {
        // Stub que graba qué hook recibió y deja al test inyectar lógica extra
        // (mutación de payload, escritura en counters) vía Callback.
        private class RecordingEffect : IRelicEffect
        {
            public readonly List<RelicHook> Calls = new List<RelicHook>();
            public Action<RelicHook, RelicHookContext> Callback;

            public void OnHook(RelicHook hook, RelicHookContext ctx)
            {
                Calls.Add(hook);
                Callback?.Invoke(hook, ctx);
            }
        }

        private static RelicDefinition MakeDef(string name, IRelicEffect effect, params RelicHook[] hooks)
        {
            RelicDefinition def = ScriptableObject.CreateInstance<RelicDefinition>();
            def.DisplayName = name;
            def.Effect = effect;
            def.Hooks = hooks;
            return def;
        }

        private static PlayerTurnStartHookData MakeTurnStartData(RunState rs, RelicHookDispatcher disp)
        {
            // TurnManager null: válido — los tests no exigen API de mutación.
            return new PlayerTurnStartHookData(rs, null, disp, TurnManager.WorldSide.A);
        }

        [Test]
        public void Dispatch_WithNoRelics_DoesNotThrow()
        {
            RunState rs = new RunState();
            RelicHookDispatcher disp = new RelicHookDispatcher(rs);

            Assert.DoesNotThrow(() => disp.Dispatch(RelicHook.OnPlayerTurnStart, MakeTurnStartData(rs, disp)));
            Assert.AreEqual(0, disp.GetSubscribers(RelicHook.OnPlayerTurnStart).Count);
        }

        [Test]
        public void Dispatch_SingleSubscriber_InvokesOnceWithCorrectHook()
        {
            RunState rs = new RunState();
            RelicHookDispatcher disp = new RelicHookDispatcher(rs);
            RecordingEffect effect = new RecordingEffect();
            rs.Relics.Add(new RelicInstance(MakeDef("Opening", effect, RelicHook.OnCombatStart), 0));

            disp.Dispatch(RelicHook.OnCombatStart, new CombatStartHookData(rs, null, disp, null));

            Assert.AreEqual(1, effect.Calls.Count);
            Assert.AreEqual(RelicHook.OnCombatStart, effect.Calls[0]);
        }

        [Test]
        public void Dispatch_MultipleSubscribers_RunInAcquisitionOrder()
        {
            RunState rs = new RunState();
            RelicHookDispatcher disp = new RelicHookDispatcher(rs);
            List<string> order = new List<string>();
            RecordingEffect a = new RecordingEffect { Callback = (_, __) => order.Add("A") };
            RecordingEffect b = new RecordingEffect { Callback = (_, __) => order.Add("B") };
            RecordingEffect c = new RecordingEffect { Callback = (_, __) => order.Add("C") };

            // Adquiridos en orden A → B → C.
            rs.Relics.Add(new RelicInstance(MakeDef("A", a, RelicHook.OnPlayerTurnStart), 0));
            rs.Relics.Add(new RelicInstance(MakeDef("B", b, RelicHook.OnPlayerTurnStart), 1));
            rs.Relics.Add(new RelicInstance(MakeDef("C", c, RelicHook.OnPlayerTurnStart), 2));

            disp.Dispatch(RelicHook.OnPlayerTurnStart, MakeTurnStartData(rs, disp));

            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, order);
        }

        [Test]
        public void Dispatch_IrrelevantHook_DoesNotInvoke()
        {
            RunState rs = new RunState();
            RelicHookDispatcher disp = new RelicHookDispatcher(rs);
            RecordingEffect effect = new RecordingEffect();
            rs.Relics.Add(new RelicInstance(MakeDef("DamageOnly", effect, RelicHook.OnDamageDealt), 0));

            disp.Dispatch(RelicHook.OnPlayerTurnStart, MakeTurnStartData(rs, disp));

            Assert.AreEqual(0, effect.Calls.Count);
        }

        [Test]
        public void Dispatch_MutablePayload_ChainsAcrossSubscribers()
        {
            RunState rs = new RunState();
            RelicHookDispatcher disp = new RelicHookDispatcher(rs);
            RecordingEffect a = new RecordingEffect { Callback = (_, ctx) => ((DamageDealtHookData)ctx).Amount += 1 };
            RecordingEffect b = new RecordingEffect { Callback = (_, ctx) => ((DamageDealtHookData)ctx).Amount += 1 };
            rs.Relics.Add(new RelicInstance(MakeDef("A", a, RelicHook.OnDamageDealt), 0));
            rs.Relics.Add(new RelicInstance(MakeDef("B", b, RelicHook.OnDamageDealt), 1));

            DamageDealtHookData data = new DamageDealtHookData(
                rs, null, disp, 10, Effectiveness.Neutro, ElementType.Rojo, null);
            disp.Dispatch(RelicHook.OnDamageDealt, data);

            Assert.AreEqual(12, data.Amount);
        }

        [Test]
        public void Dispatch_RelicWithMultipleHooks_FiresOnEach()
        {
            RunState rs = new RunState();
            RelicHookDispatcher disp = new RelicHookDispatcher(rs);
            RecordingEffect effect = new RecordingEffect();
            rs.Relics.Add(new RelicInstance(
                MakeDef("Bracket", effect, RelicHook.OnCombatStart, RelicHook.OnCombatEnd), 0));

            disp.Dispatch(RelicHook.OnCombatStart, new CombatStartHookData(rs, null, disp, null));
            disp.Dispatch(RelicHook.OnCombatEnd, new CombatEndHookData(rs, null, disp, true, null));

            Assert.AreEqual(2, effect.Calls.Count);
            Assert.AreEqual(RelicHook.OnCombatStart, effect.Calls[0]);
            Assert.AreEqual(RelicHook.OnCombatEnd, effect.Calls[1]);
        }

        [Test]
        public void Counters_PersistAcrossDispatches_DispatcherDoesNotReset()
        {
            RunState rs = new RunState();
            RelicHookDispatcher disp = new RelicHookDispatcher(rs);
            RecordingEffect effect = new RecordingEffect
            {
                Callback = (_, ctx) =>
                {
                    Dictionary<string, int> counters = ctx.CurrentRelic.Counters;
                    int prev = counters.TryGetValue("x", out int v) ? v : 0;
                    counters["x"] = prev + 1;
                }
            };
            RelicInstance instance = new RelicInstance(
                MakeDef("Counter", effect, RelicHook.OnPlayerTurnStart), 0);
            rs.Relics.Add(instance);

            disp.Dispatch(RelicHook.OnPlayerTurnStart, MakeTurnStartData(rs, disp));
            disp.Dispatch(RelicHook.OnPlayerTurnStart, MakeTurnStartData(rs, disp));
            disp.Dispatch(RelicHook.OnPlayerTurnStart, MakeTurnStartData(rs, disp));

            Assert.AreEqual(3, instance.Counters["x"]);
        }

        [Test]
        public void GetSubscribers_FiltersByHookAndOrdersByAcquisition()
        {
            RunState rs = new RunState();
            RelicHookDispatcher disp = new RelicHookDispatcher(rs);

            // 5 Retazos mixtos: 3 escuchan OnPlayerTurnStart (R2, R3, R5).
            rs.Relics.Add(new RelicInstance(MakeDef("R1", new RecordingEffect(), RelicHook.OnDamageDealt), 0));
            rs.Relics.Add(new RelicInstance(MakeDef("R2", new RecordingEffect(), RelicHook.OnPlayerTurnStart), 1));
            rs.Relics.Add(new RelicInstance(MakeDef("R3", new RecordingEffect(),
                RelicHook.OnDamageDealt, RelicHook.OnPlayerTurnStart), 2));
            rs.Relics.Add(new RelicInstance(MakeDef("R4", new RecordingEffect(), RelicHook.OnCombatEnd), 3));
            rs.Relics.Add(new RelicInstance(MakeDef("R5", new RecordingEffect(), RelicHook.OnPlayerTurnStart), 4));

            IReadOnlyList<RelicInstance> subs = disp.GetSubscribers(RelicHook.OnPlayerTurnStart);
            Assert.AreEqual(3, subs.Count);
            Assert.AreEqual("R2", subs[0].Definition.DisplayName);
            Assert.AreEqual("R3", subs[1].Definition.DisplayName);
            Assert.AreEqual("R5", subs[2].Definition.DisplayName);
        }
    }
}
